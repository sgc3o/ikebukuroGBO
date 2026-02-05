using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq; // 追加（名前検索に便利）


public class MemoryStageManager : MonoBehaviour
{
    [Serializable]
    public class StageConfig
    {
        [Header("IP Pack")]
        public MemoryIpPack ipPack;

        [Header("Board")]
        [Range(4, 6)] public int columns = 5; // 4..6
        [Min(1)] public int correctCount = 3;

        [Header("Timers")]
        [Min(1)] public int missionCountdownSec = 5;
        [Min(1)] public int memorizeCountdownSec = 5;
        [Min(0.1f)] public float fadeToNextSec = 1f;     // 0表示後のクロスフェード時間
        [Min(1f)] public float playTimeLimitSec = 10f;

        [Header("Capsule Anim")]
        public float retractSec = 0.5f;
        public float closeSec = 0.5f;
    }

    private enum State
    {
        None,
        MissionIntro,
        Memorize,
        GameStartPopup,
        Playing,
        FinishPopup,
        Done
    }

    [Header("Stages")]
    [SerializeField] private StageConfig[] stages; // 3ステージ想定（増やしてもOK）

    [Header("Panels Root")]
    [SerializeField] private CanvasGroup panelsCanvasGroup; // PanelsにCanvasGroup付けてここへ
    [SerializeField] private GameObject missionIntroPanel;
    [SerializeField] private GameObject memorizePanel;
    [SerializeField] private GameObject gamePlayPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Mission Intro UI")]
    [SerializeField] private Sprite[] stageBadgeSprites; // element0=Stage1, element1=Stage2...
    [SerializeField] private Image targetCharaImage;

    [Header("UI Refs")]
    [SerializeField] private CountdownUI missionCountdownUI;
    [SerializeField] private CountdownUI memorizeCountdownUI;
    [SerializeField] private PopupManager popupManager; // 既存
    [SerializeField] private Image stageBadgeImage;     // 1/2 等（差し替えたいなら）

    [Header("HUD Timer (Circle)")]
    [SerializeField] private Image radialTimerFill; // FillAmount使う想定（CircularCountdownUIでもOK）
    [SerializeField] private bool timerClockwise = true; // 見た目合わせ用

    [Header("Board")]
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private Transform gridRoot;          // grid.transform でもOK
    [SerializeField] private MemoryCapsuleItem capsulePrefab;
    [SerializeField] private Sprite capsuleClosedSprite;  // 赤カプセル
    [SerializeField] private int rowsFixed = 3;
    [SerializeField] private SpriteSequencePlayer correctEffectPlayer;
    [SerializeField] private Canvas canvasForUI; // Screen Space Overlayでも入れると安全

    [Header("Cross Fade Override (optional)")]
    [SerializeField] private bool useCrossFadeOverride = true;
    [SerializeField] private float crossFadeSecOverride = 0.25f; // 好きな初期値

    [Header("SE (Optional)")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip correctSE;
    [SerializeField] private AudioClip wrongSE;

    // runtime
    private int currentStageIndex = 0;
    private State state = State.None;

    private readonly List<MemoryCapsuleItem> items = new();
    private int remainingCorrect;
    private float playRemainSec;
    private bool stageEnding;

    private void Start()
    {
        BeginStage(0);
    }

    public void BeginStage(int stageIndex)
    {
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("[MemoryStageManager] stages is empty");
            return;
        }

        currentStageIndex = Mathf.Clamp(stageIndex, 0, stages.Length - 1);
        StopAllCoroutines();
        StartCoroutine(RunStageFlow());
    }

    private IEnumerator RunStageFlow()
    {
        stageEnding = false;

        var cfg = stages[currentStageIndex];
        ApplyStageBadge(currentStageIndex);

        // 盤面生成
        BuildBoard(cfg);

        // --- MissionIntro 表示準備 ---
        ShowOnly(missionIntroPanel);
        ApplyStageBadge(currentStageIndex);

        if (targetCharaImage != null)
        {
            targetCharaImage.sprite = (cfg.ipPack != null) ? cfg.ipPack.TargetSprite : null;
            targetCharaImage.enabled = (targetCharaImage.sprite != null);
        }

        // MissionIntro 入る直前
        missionCountdownUI.gameObject.SetActive(true);
        missionCountdownUI.SetAlpha(1f);   // ← CountdownUIに関数がなければ canvasGroup.alpha = 1 直書きでもOK

        // --- Mission Intro ---
        state = State.MissionIntro;

        // ★追加：ミッション提示UI（背景・ステージバッジ・ターゲット画像）をセット
        SetupMissionIntro(cfg, currentStageIndex);

        // 既存：カウントダウン（5→0） + 0後にfadeToNextSec待つ想定
        yield return missionCountdownUI.Play(cfg.missionCountdownSec);

        // MissionIntro -> Memorize を「パネル同士のクロスフェード」で

        float fadeSec = useCrossFadeOverride ? crossFadeSecOverride : cfg.fadeToNextSec;
        yield return CrossFadePanels(missionIntroPanel, memorizePanel, fadeSec);


        //yield return CrossFadePanels(missionIntroPanel, memorizePanel, cfg.fadeToNextSec);

        // HideMissionIntro は不要（fromPanelをSetActive(false)するので）
        // HideMissionIntro();

        // --- Memorize ---
        state = State.Memorize;
        yield return RunMemorizeSequence(cfg);

        // --- GameStart Popup ---
        state = State.GameStartPopup;
        if (popupManager != null) popupManager.ShowGameStart();
        // ポップアップ表示中は操作不可
        SetAllInteractable(false);

        // ここは「プレイ画面アイドル」なのでHUDタイマー停止（=開始しない）
        yield return WaitPopupDone();

        // --- Playing ---
        state = State.Playing;
        SetAllInteractable(true);
        yield return RunPlayingSequence(cfg);

        // --- Finish Popup ---
        state = State.FinishPopup;
        SetAllInteractable(false);
        if (popupManager != null) popupManager.ShowFinish();
        yield return WaitPopupDone();

        // 2秒後フェードで次ステージへ（ここは SceneTransition に置き換え可）
        yield return new WaitForSeconds(2f);

        EndStageAndGoNext();
    }

    private void EndStageAndGoNext()
    {
        if (state == State.Done) return;

        int next = currentStageIndex + 1;
        if (next >= stages.Length)
        {
            state = State.Done;
            // TODO: Incentiveへ SceneTransition.Go("S_MemoryIncentive") 等
            Debug.Log("[MemoryStageManager] All stages done. Go Incentive.");
            return;
        }

        BeginStage(next);
    }

    private IEnumerator RunCountdown(CountdownUI ui, int seconds, float fadeToNextSec)
    {
        if (ui == null) yield break;

        for (int s = seconds; s >= 0; s--)
        {
            ui.ShowNumber(s);
            yield return new WaitForSeconds(1f);
        }

        // 0を見せた後のクロスフェード
        yield return ui.FadeOut(fadeToNextSec);
    }

    private IEnumerator RunMemorizeSequence(StageConfig cfg)
    {
        // ①カプセルひっこめ動画（0.5秒想定）
        //    今回は「全カプセルを一斉に“開ける”」がイメージに近いので、
        //    表示だけ先に作っておいて、動画部分は時間待ちで吸収してる。
        //    もし「盤面全体の動画」をUIで流すなら、ここにその再生コルーチンを挿す。

        yield return new WaitForSeconds(cfg.retractSec);

        // ②画像スケールアップ表示（全員Reveal）
        foreach (var it in items)
        {
            // ここでは“正解/不正解問わず”配置を見せる（全部見せ運用）
            yield return it.PlayRetractAndReveal();
        }

        // ③ここからカウントダウン開始（画像は表示されたまま）
        yield return RunCountdown(memorizeCountdownUI, cfg.memorizeCountdownSec, 0f);

        // ④ 0表示済み（RunCountdownの最後が0を1秒見せてるのでOK）

        // ⑤カプセル閉じる用の動画
        foreach (var it in items)
        {
            yield return it.PlayClose();
        }

        // カウントUIを次のために戻す（必要なら）
        if (memorizeCountdownUI != null) yield return memorizeCountdownUI.FadeOut(cfg.fadeToNextSec);
    }

    private IEnumerator RunPlayingSequence(StageConfig cfg)
    {
        remainingCorrect = cfg.correctCount;
        playRemainSec = cfg.playTimeLimitSec;
        stageEnding = false;

        // タイマー開始
        while (!stageEnding)
        {
            playRemainSec -= Time.deltaTime;
            UpdateRadialTimer(playRemainSec, cfg.playTimeLimitSec);

            // 終了条件
            if (remainingCorrect <= 0)
            {
                stageEnding = true;
                break;
            }
            if (playRemainSec <= 0f)
            {
                stageEnding = true;
                break;
            }

            yield return null;
        }
    }

    private void OnCapsuleClicked(MemoryCapsuleItem item)
    {
        if (state != State.Playing) return;
        if (item == null) return;
        if (item.IsOpened) return; // 連打ガード

        StartCoroutine(HandleClick(item));
    }

    private IEnumerator HandleClick(MemoryCapsuleItem item)
    {
        // 連打ガード：一旦全部OFF
        SetAllInteractable(false);

        // ひっこめ→表示（ここはそのまま）
        yield return item.PlayRetractAndReveal();

        // 判定
        if (item.IsCorrect)
        {
            remainingCorrect--;
            PlaySE(correctSE);

            // ★当たり演出：押した位置に出す
            yield return PlayCorrectEffectAtItem(item);
        }
        else
        {
            // ★不正解でも閉じない（固定表示）
            PlaySE(wrongSE);
            // ここで閉じる処理はしない
        }

        // 終了判定
        if (remainingCorrect <= 0 || playRemainSec <= 0f)
        {
            stageEnding = true;
            yield break;
        }

        // 入力再開
        SetAllInteractable(true);
    }

    private IEnumerator PlayCorrectEffectAtItem(MemoryCapsuleItem item)
    {
        if (correctEffectPlayer == null) yield break;

        // 位置合わせに必要なRectTransform
        RectTransform effectRect = correctEffectPlayer.Rect;
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (effectRect == null || itemRect == null)
        {
            // RectTransformが取れない場合は中央再生にフォールバック
            yield return correctEffectPlayer.PlayOnceAndWait();
            yield break;
        }

        // itemRect のワールド座標 → Canvas内のローカル座標に変換して当たり演出をそこへ置く
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, itemRect.position);

        RectTransform canvasRect = canvasForUI != null ? canvasForUI.GetComponent<RectTransform>() : null;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos))
        {
            effectRect.anchoredPosition = localPos;
        }

        yield return correctEffectPlayer.PlayOnceAndWait();
    }


    private void BuildBoard(StageConfig cfg)
    {
        // 既存クリア
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] != null) Destroy(items[i].gameObject);
        }
        items.Clear();

        // Grid設定
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cfg.columns;
        }
        if (gridRoot == null && grid != null) gridRoot = grid.transform;

        int totalSlots = rowsFixed * cfg.columns;

        // データ作成（正解/不正解のスプライト割り当て + ランダム配置）
        List<SlotData> slotData = BuildSlotData(totalSlots, cfg.correctCount, cfg.ipPack);

        // 生成
        for (int i = 0; i < totalSlots; i++)
        {
            var d = slotData[i];

            var it = Instantiate(capsulePrefab, gridRoot);
            // ステージ設定で秒数反映（CapsuleItem側のInspectorを上書きしたい場合）
            // ※今回は最小なので、そのままCapsuleItem側の値でOK。必要なら setter 追加。

            it.Setup(
                capsuleClosedSprite,
                d.revealedSprite,
                d.isCorrect,
                OnCapsuleClicked
            );

            it.SetInteractable(false); // 開始前は触れない（GameStart後にON）
            items.Add(it);
        }
    }

    // ===============================
// Mission Intro 表示セットアップ
// ===============================
private void SetupMissionIntro(StageConfig cfg, int stageIndex)
{
    // パネル表示
    if (missionIntroPanel != null)
        missionIntroPanel.SetActive(true);

    // ステージバッジ（1 / 2 / 3）
    if (stageBadgeImage != null &&
        stageBadgeSprites != null &&
        stageIndex >= 0 &&
        stageIndex < stageBadgeSprites.Length &&
        stageBadgeSprites[stageIndex] != null)
    {
        stageBadgeImage.sprite = stageBadgeSprites[stageIndex];
        stageBadgeImage.enabled = true;
    }
    else if (stageBadgeImage != null)
    {
        stageBadgeImage.enabled = false;
    }

    // ターゲットキャラ（IPの index0 = TARGET）
    Sprite target = null;
    if (cfg.ipPack != null)
        target = cfg.ipPack.TargetSprite;

    if (targetCharaImage != null)
    {
        targetCharaImage.sprite = target;
        targetCharaImage.enabled = (target != null);
    }
}

private void HideMissionIntro()
{
    if (missionIntroPanel != null)
        missionIntroPanel.SetActive(false);
}


    private List<SlotData> BuildSlotData(int totalSlots, int correctCount, MemoryIpPack pack)
    {
        var list = new List<SlotData>(totalSlots);

        if (pack == null || pack.Count == 0)
        {
            Debug.LogError("[MemoryStageManager] IpPack is missing or empty");
            // それでも落ちないように空で埋める
            for (int i = 0; i < totalSlots; i++)
                list.Add(new SlotData { isCorrect = false, revealedSprite = null });
            return list;
        }

        correctCount = Mathf.Clamp(correctCount, 0, totalSlots);

        Sprite target = pack.TargetSprite;

        // 1) 正解を correctCount 分作る
        for (int i = 0; i < correctCount; i++)
            list.Add(new SlotData { isCorrect = true, revealedSprite = target });

        // 2) 残りは不正解。できるだけ偏り少なくする（ラウンドロビン）
        var others = new List<Sprite>();
        for (int i = 0; i < pack.Count; i++)
        {
            var sp = pack.GetSprite(i);
            if (sp == null) continue;
            if (sp == target) continue;
            others.Add(sp);
        }

        int remain = totalSlots - correctCount;

        if (others.Count == 0)
        {
            // ターゲットしかない場合、全部ターゲットで埋める（運用的にはNGなので警告）
            Debug.LogWarning("[MemoryStageManager] IpPack has only target sprite. Fill remain with target.");
            for (int i = 0; i < remain; i++)
                list.Add(new SlotData { isCorrect = false, revealedSprite = target });
        }
        else
        {
            for (int i = 0; i < remain; i++)
            {
                var sp = others[i % others.Count];
                list.Add(new SlotData { isCorrect = false, revealedSprite = sp });
            }
        }

        // 3) 配置ランダム（Fisher-Yates）
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private void SetAllInteractable(bool value)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i] != null) items[i].SetInteractable(value);
    }

    private IEnumerator WaitPopupDone()
    {
        if (popupManager == null) yield break;
        // 表示開始直後は IsShowing が true になるので、終わるまで待つ
        while (popupManager.IsShowing) yield return null;
    }

    private void ApplyStageBadge(int stageIndex)
    {
        if (stageBadgeImage == null) return;
        if (stageBadgeSprites == null) return;
        if (stageIndex < 0 || stageIndex >= stageBadgeSprites.Length) return;

        stageBadgeImage.sprite = stageBadgeSprites[stageIndex];
        stageBadgeImage.enabled = (stageBadgeSprites[stageIndex] != null);
    }

    private void UpdateRadialTimer(float remain, float total)
    {
        if (radialTimerFill == null) return;
        if (total <= 0f) return;

        float t = Mathf.Clamp01(remain / total);
        radialTimerFill.fillAmount = timerClockwise ? t : (1f - t);
    }

    private void PlaySE(AudioClip clip)
    {
        if (seSource == null) return;
        if (clip == null) return;
        seSource.PlayOneShot(clip);
    }

    private struct SlotData
    {
        public bool isCorrect;
        public Sprite revealedSprite;
    }


    private Sprite FindSpriteByName(MemoryIpPack pack, string exactName)
    {
        if (pack == null) return null;

        // MemoryIpPack 側の配列名に合わせる（あなたは characterSprites）
        var list = pack.characterSprites;

        if (list == null || list.Length == 0) return null;

        // 完全一致
        var hit = list.FirstOrDefault(s => s != null && s.name == exactName);
        if (hit != null) return hit;

        // ch__00000 みたいに差がある場合の保険（部分一致）
        hit = list.FirstOrDefault(s => s != null && s.name.Contains(exactName));
        if (hit != null) return hit;

        // 最後の保険：先頭
        return list[0];
    }

    private void ShowOnly(GameObject panel)
    {
        HideAllPanels();
        if (panel) panel.SetActive(true);
    }

    private IEnumerator CrossFadeTo(GameObject nextPanel, float sec)
    {
        // panelsCanvasGroup が無いなら「切替だけ」して終わり
        if (panelsCanvasGroup == null || sec <= 0f)
        {
            ShowOnly(nextPanel);
            yield break;
        }

        // FadeOut
        yield return FadeCanvasGroup(panelsCanvasGroup, 1f, 0f, sec);

        // 切替
        ShowOnly(nextPanel);

        // FadeIn
        yield return FadeCanvasGroup(panelsCanvasGroup, 0f, 1f, sec);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float sec)
    {
        cg.alpha = from;
        cg.blocksRaycasts = true;
        cg.interactable = false;

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / sec);
            cg.alpha = Mathf.Lerp(from, to, a);
            yield return null;
        }
        cg.alpha = to;
    }

    // ここに「表示/非表示」をまとめる（nullでも落ちないようにする）
    private void HideAllPanels()
    {
        // Panel系（あなたのHierarchy名に合わせて必要なだけ追加OK）
        SetActiveSafe(missionIntroPanel, false);
        SetActiveSafe(memorizePanel, false);
        SetActiveSafe(gamePlayPanel, false);

        // UI単体（必要なら）
        if (missionCountdownUI != null) SetActiveSafe(missionCountdownUI.gameObject, false);
        if (memorizeCountdownUI != null) SetActiveSafe(memorizeCountdownUI.gameObject, false);

        // HUDもまとめたいなら（参照がある場合だけ）
        SetActiveSafe(hudPanel, false);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private IEnumerator CrossFadePanels(GameObject fromPanel, GameObject toPanel, float sec)
    {
        var from = GetOrAddCanvasGroup(fromPanel);
        var to = GetOrAddCanvasGroup(toPanel);

        if (toPanel != null) toPanel.SetActive(true);

        // 初期値
        if (to != null)
        {
            to.alpha = 0f;
            to.interactable = false;
            to.blocksRaycasts = false;
        }
        if (from != null)
        {
            from.alpha = 1f;
            from.interactable = false;
            from.blocksRaycasts = false;
        }

        if (sec <= 0f)
        {
            if (fromPanel != null) fromPanel.SetActive(false);
            if (to != null)
            {
                to.alpha = 1f;
                to.interactable = true;
                to.blocksRaycasts = true;
            }
            yield break;
        }

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sec);

            if (from != null) from.alpha = Mathf.Lerp(1f, 0f, k);
            if (to != null) to.alpha = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        // 終了
        if (from != null) from.alpha = 0f;
        if (fromPanel != null) fromPanel.SetActive(false);

        if (to != null)
        {
            to.alpha = 1f;
            to.interactable = true;
            to.blocksRaycasts = true;
        }
    }


}