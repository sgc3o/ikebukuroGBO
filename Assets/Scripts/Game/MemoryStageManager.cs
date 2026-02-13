using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


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

    [Header("Scene Transition")]
    [SerializeField] private string incentiveSceneName = "S_MemoryIncentive";

    [Header("Panels Root")]
    [SerializeField] private CanvasGroup panelsCanvasGroup; // PanelsにCanvasGroup付けてここへ
    [SerializeField] private GameObject missionIntroPanel;
    [SerializeField] private GameObject memorizePanel;
    [SerializeField] private GameObject gamePlayPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Mission Intro UI")]
    [SerializeField] private Sprite[] stageBadgeSprites; // element0=Stage1, element1=Stage2...
    [SerializeField] private Image targetCharaImage;
    [SerializeField] private Image missionLogoImage;   // MissionIntroPanel側
    [SerializeField] private Image memorizeLogoImage;  // MemorizePanel側
    [SerializeField] private Image playLogoImage;      // GamePlayPanel側


    [Header("UI Refs")]
    [SerializeField] private CountdownUI missionCountdownUI;
    [SerializeField] private CountdownUI memorizeCountdownUI;
    [SerializeField] private CountdownUI gamePlayCountdownUI; // Gameplay用（画像カウントダウン）
    [SerializeField] private float showZeroHoldSec = 0.35f;   // 0を見せる時間
    [SerializeField] private MemoryPopupManager popupManager; // 既存
    [SerializeField] private Image stageBadgeImage;     // 1/2 等（差し替えたいなら）

    [Header("HUD / HowTo Images")]
[Tooltip("旧仕様: HUD側で共有して使うHowTo Image（未設定でもOK）")]
[SerializeField] private Image howToImage;

[Tooltip("新仕様: MemorizePanel側のHowTo Image（任意）。設定すると Memorize中はこちらが表示され、Play側は非表示になります。")]
[SerializeField] private Image memorizeHowToImage;

[Tooltip("新仕様: GamePlayPanel（HUD）側のHowTo Image（任意）。設定すると Play中はこちらが表示され、Memorize側は非表示になります。")]
[SerializeField] private Image playHowToImage;

[SerializeField] private Sprite howToMemorizeSprite; // M03_02_Memorize_Howto
[SerializeField] private Sprite howToPlaySprite;     // M03_03_Play_Howto

    [SerializeField] private Image hudStageBadgeImage;   // HUD側のステージ数（StageBadgeImageをコピペしたやつ）
    [SerializeField] private GameObject circleTimerRoot; // 旧：HUD/CircleTimer（未使用なら未設定でOK）
    [SerializeField] private Image hudTargetCharaImage; // HUDのターゲット表示Image（作ったやつ）
    private Sprite currentTargetSprite;                 // 今ステージのターゲットを保持

    [Header("HUD Timer (Circle) - legacy")]
    [SerializeField] private Image radialTimerFill; // 旧：FillAmount用（使わないなら未設定でOK）
    [SerializeField] private bool timerClockwise = true; // 旧：見た目合わせ用
    [SerializeField] private CanvasGroup circleTimerCanvasGroup; // 旧：HUD/CircleTimer の CanvasGroup
    [SerializeField] private float countdownToTimerFadeSec = 0.25f; // 旧：クロスフェード秒


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
        StopAllCoroutines();

        // ★追加：フェード途中で止まっても復帰させる
        if (panelsCanvasGroup != null)
        {
            panelsCanvasGroup.alpha = 1f;
            panelsCanvasGroup.gameObject.SetActive(true);
        }

        //（あれば）各パネル側も念のため復帰
        ForceCanvasGroupAlpha(missionIntroPanel, 1f);
        ForceCanvasGroupAlpha(memorizePanel, 1f);
        ForceCanvasGroupAlpha(gamePlayPanel, 1f);

        currentStageIndex = stageIndex;
        StartCoroutine(RunStageFlow());
    }

    private void ForceCanvasGroupAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = a;
    }


    private IEnumerator RunStageFlow()
    {
        stageEnding = false;

        var cfg = stages[currentStageIndex];
        ApplyStageBadge(currentStageIndex);

        // 盤面生成
        BuildBoard(cfg);
        // ★ 追加：このステージで「あと何体 正解が残ってるか」
        remainingCorrect = Mathf.Max(1, cfg.correctCount);
        // 任意：デバッグ（あとで消してOK）
        Debug.Log($"[MemoryStageManager] BeginStage idx={currentStageIndex} correctCount={cfg.correctCount} remainingCorrect={remainingCorrect}");


        // --- MissionIntro 表示準備 ---
        ShowOnly(missionIntroPanel);
        ApplyStageBadge(currentStageIndex);

        if (targetCharaImage != null)
        {
            targetCharaImage.sprite = (cfg.ipPack != null) ? cfg.ipPack.TargetSprite : null;
            targetCharaImage.enabled = (targetCharaImage.sprite != null);
        }
        currentTargetSprite = (cfg != null && cfg.ipPack != null) ? cfg.ipPack.TargetSprite : null;
        ApplyIpLogo(cfg.ipPack);


        // MissionIntro 入る直前
        missionCountdownUI.gameObject.SetActive(true);
        missionCountdownUI.SetAlpha(1f);   // ← CountdownUIに関数がなければ canvasGroup.alpha = 1 直書きでもOK

        // --- Mission Intro ---
        state = State.MissionIntro;

        //ミッション提示UI（背景・ステージバッジ・ターゲット画像）をセット
        SetupMissionIntro(cfg, currentStageIndex);

        //カウントダウン（5→0） + 0後にfadeToNextSec待つ想定
        yield return missionCountdownUI.Play(cfg.missionCountdownSec);

        //ミッション側カウント表示を消してから遷移
        if (missionCountdownUI != null)
            yield return missionCountdownUI.FadeOut(0f);

        // Gameplayタイマーは Playing 直前に出すので、ここでは出さない
        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.gameObject.SetActive(false);
            gamePlayCountdownUI.SetAlpha(0f);
        }

        // 旧Circleタイマーを使っていた名残（残っててもOKだが、Playでは使わない）
        if (circleTimerRoot != null) circleTimerRoot.SetActive(false);


        // MissionIntro -> Memorize を「パネル同士のクロスフェード」で
        float fadeSec = useCrossFadeOverride ? crossFadeSecOverride : cfg.fadeToNextSec;
        yield return CrossFadePanels(missionIntroPanel, memorizePanel, fadeSec);

        //Memorizeでは盤面(HUD+Grid)も表示する（重ね表示）
        SetActiveSafe(gamePlayPanel, true);
        SetActiveSafe(hudPanel, true);

        // Memorizeカウントも表示
        if (memorizeCountdownUI != null) memorizeCountdownUI.ShowImmediate();

        // --- Memorize ---
        state = State.Memorize;
        SetHudModeMemorize();
        memorizeCountdownUI.ShowImmediate();
        memorizeCountdownUI.ShowNumber(cfg.memorizeCountdownSec); // 最初から5を表示
        yield return RunMemorizeSequence(cfg);

        // --- GameStart Popup ---
        state = State.GameStartPopup;
        if (popupManager != null) popupManager.ShowGameStart();
        // ポップアップ表示中は操作不可
        SetAllInteractable(false);

        // ここは「プレイ画面アイドル」なのでHUDタイマー停止（=開始しない）
        yield return WaitPopupDone();

        // ★GameStartポップアップが閉じたら、Playing開始と同時にGameplayカウントダウンを使う
        if (memorizeCountdownUI != null)
        {
            memorizeCountdownUI.SetAlpha(0f);
            memorizeCountdownUI.gameObject.SetActive(false);
        }
        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.gameObject.SetActive(true);
            gamePlayCountdownUI.ShowImmediate();
        }

        // --- Playing ---
        state = State.Playing;
        SetHudModePlay();
        SetAllInteractable(true);
        yield return RunPlayingSequence(cfg);

        // --- Finish Popup ---
        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.ShowImmediate();   // 念のため表示状態に
            gamePlayCountdownUI.ShowNumber(0);     // 0を強制描画
            yield return new WaitForSeconds(showZeroHoldSec);
        }
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
            SceneTransition.Go(incentiveSceneName);
            return;
        }

        BeginStage(next);
    }

    private IEnumerator RunCountdown(CountdownUI ui, int seconds)
    {
        if (ui == null) yield break;

        // 先に表示＆最初の数字を出す（白対策にもなる）
        ui.ShowImmediate();
        ui.ShowNumber(seconds);

        // CountdownUI.Play() は「0で holdLastNumberSec だけ待つ」仕様
        yield return ui.Play(seconds);

        // ★ここでは消さない（0を残すのが目的）
    }


    private IEnumerator RunMemorizeSequence(StageConfig cfg)
    {
        // Memorize中は触れない
        SetAllInteractable(false);

        // retract待ち（今の速度感を維持）
        yield return new WaitForSeconds(cfg.retractSec);

        // ★全員同時にReveal
        var reveal = new List<Coroutine>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            reveal.Add(StartCoroutine(it.PlayRetractAndReveal()));
        }
        foreach (var c in reveal) yield return c;

        // ★カウント（CountdownUI側が 0 を短く見せる仕様）
        if (memorizeCountdownUI != null)
            yield return memorizeCountdownUI.Play(cfg.memorizeCountdownSec);
        else
            yield return new WaitForSeconds(cfg.memorizeCountdownSec);

        // ★全員同時にClose
        var close = new List<Coroutine>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            close.Add(StartCoroutine(it.PlayClose()));
        }

        foreach (var c in close) yield return c;
        // ★ここではまだ円時計を出さない（GameStartポップアップ後に出す）
        // 次は GameStartPopup → Playing なので、ここではまだ操作不可のままでOK
    }

    private IEnumerator RunPlayingSequence(StageConfig cfg)
    {
        // Playing中は基本押せる
        SetAllInteractable(true);

        // ★Gameplayタイマー（画像カウントダウン）
        int limitSec = Mathf.Max(1, Mathf.CeilToInt(cfg.playTimeLimitSec)); // 10秒想定
        playRemainSec = limitSec;

        Coroutine timerCo = null;
        bool timeUp = false;

        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.gameObject.SetActive(true);
            gamePlayCountdownUI.ShowImmediate();
            gamePlayCountdownUI.ShowNumber(limitSec); // 最初に10を確実に出す

            timerCo = StartCoroutine(PlayGameplayCountdown(limitSec, () =>
            {
                timeUp = true;
                playRemainSec = 0f;
            }));
        }

        // ループ：時間切れ もしくは 全正解 まで待つ
        float remain = limitSec;
        while (!stageEnding && remainingCorrect > 0 && !timeUp)
        {
            remain -= Time.deltaTime;
            playRemainSec = remain;
            if (remain <= 0f)
            {
                timeUp = true;
                playRemainSec = 0f;
                break;
            }
            yield return null;
        }

        // 全正解で先に抜けたら、タイマー止める
        if (!timeUp && remainingCorrect <= 0)
        {
            stageEnding = true;
        }

        if (timerCo != null) StopCoroutine(timerCo);

        // ここから「終了処理」(※ループ外！！)
        bool success = (remainingCorrect <= 0);

        // 失敗なら未開封の正解を見せる（任意）
        if (!success)
        {
            // ここで操作不能にして演出に集中
            SetAllInteractable(false);

            // 0.25秒でフェードイン表示（あなたの要望②）
            yield return RevealUnopenedCorrects(0.25f);
        }

        // Playing終了時点ではもう押させない（次のポップアップや遷移があるため）
        SetAllInteractable(false);
    }

    // Gameplay用の画像カウントダウン。
    // - 全正解で stageEnding が立ったら自動停止
    // - 最後に 0 を表示して onTimeUp を呼ぶ
    private IEnumerator PlayGameplayCountdown(int seconds, Action onTimeUp)
    {
        if (gamePlayCountdownUI == null)
            yield break;

        int s = Mathf.Max(1, seconds);
        while (s > 0)
        {
            // 途中でステージが終わったら止める
            if (stageEnding || remainingCorrect <= 0)
                yield break;

            // 次の1秒待つ
            yield return new WaitForSeconds(1f);
            s--;
            gamePlayCountdownUI.ShowNumber(s);
        }

        if (stageEnding || remainingCorrect <= 0)
            yield break;

        // 0を出した状態で通知
        onTimeUp?.Invoke();
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

        // まず開く→キャラ表示（ここは両方共通）
        yield return item.PlayRetractAndReveal();

        if (item.IsCorrect)
        {
            // 正解
            remainingCorrect--;
            PlaySE(correctSE);

            // ✅ 正解はCloseしない
            // ✅ キャラ画像は既に出てるので、そのタイミングでエフェクト開始
            // （視覚的に “タッチ→Open→キャラ＋エフェクト” になる）
            yield return PlayCorrectEffectAtItem(item);

            if (remainingCorrect <= 0)
            {
                stageEnding = true;
                yield break;
            }
        }
        else
        {
            // 不正解
            PlaySE(wrongSE);

            // ✅ 不正解もCloseしない（キャラ出しっぱなし）
            // ここで item.PlayClose(); を呼ばない
        }

        // 終了判定（タイムアップなど）
        if (playRemainSec <= 0f)
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
        Debug.Log($"[MemoryStageManager] SetAllInteractable({value}) items={items?.Count}");

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

    private void ApplyHowToVisibility(bool isMemorize)
    {
        // 新仕様: パネルごとに別Imageを使う
        if (memorizeHowToImage != null || playHowToImage != null)
        {
            if (memorizeHowToImage != null)
            {
                memorizeHowToImage.sprite = howToMemorizeSprite;
                memorizeHowToImage.enabled = isMemorize && (howToMemorizeSprite != null);
            }

            if (playHowToImage != null)
            {
                playHowToImage.sprite = howToPlaySprite;
                playHowToImage.enabled = !isMemorize && (howToPlaySprite != null);
            }

            // 旧共有Imageがある場合は念のためOFF
            if (howToImage != null)
                howToImage.enabled = false;

            return;
        }

        // 旧仕様: HUD側で共有Imageを切り替える
        if (howToImage != null)
        {
            howToImage.sprite = isMemorize ? howToMemorizeSprite : howToPlaySprite;
            howToImage.enabled = (howToImage.sprite != null);
        }
    }

    private void SetHudModeMemorize()
    {
        // HowTo: Memorize用（新仕様優先）
        ApplyHowToVisibility(isMemorize: true);

        if (hudStageBadgeImage != null && stageBadgeSprites != null &&
            currentStageIndex >= 0 && currentStageIndex < stageBadgeSprites.Length)
        {
            hudStageBadgeImage.sprite = stageBadgeSprites[currentStageIndex];
            hudStageBadgeImage.enabled = (hudStageBadgeImage.sprite != null);
        }

        if (memorizeCountdownUI != null) memorizeCountdownUI.gameObject.SetActive(true);
        if (gamePlayCountdownUI != null) gamePlayCountdownUI.gameObject.SetActive(false);
        if (circleTimerRoot != null) circleTimerRoot.SetActive(false); // 旧UIが残っていてもOFF

        // ★Memorize中もターゲットを出す（要望②）
        if (hudTargetCharaImage != null)
        {
            hudTargetCharaImage.sprite = currentTargetSprite;
            hudTargetCharaImage.enabled = (currentTargetSprite != null);
            if (currentTargetSprite != null)
                hudTargetCharaImage.transform.SetAsLastSibling();
        }
    }

    private void SetHudModePlay()
    {
        // HowTo: Play用（新仕様優先）
        ApplyHowToVisibility(isMemorize: false);

        if (hudStageBadgeImage != null && stageBadgeSprites != null &&
            currentStageIndex >= 0 && currentStageIndex < stageBadgeSprites.Length)
        {
            hudStageBadgeImage.sprite = stageBadgeSprites[currentStageIndex];
            hudStageBadgeImage.enabled = (hudStageBadgeImage.sprite != null);
        }

        if (memorizeCountdownUI != null) memorizeCountdownUI.gameObject.SetActive(false);
        if (circleTimerRoot != null) circleTimerRoot.SetActive(false); // ★Circleは使わない
        if (gamePlayCountdownUI != null) gamePlayCountdownUI.gameObject.SetActive(true);

        // ★Play中：HowToの上にターゲット表示
        if (hudTargetCharaImage != null)
        {
            hudTargetCharaImage.sprite = currentTargetSprite;
            hudTargetCharaImage.enabled = (currentTargetSprite != null);

            // 確実に前面へ（HowToより上にしたいなら有効）
            if (currentTargetSprite != null)
                hudTargetCharaImage.transform.SetAsLastSibling();
        }
    }

    private IEnumerator CrossFadeCountdownToCircleTimer(float sec)
    {
        if (memorizeCountdownUI == null) yield break;
        if (circleTimerCanvasGroup == null) yield break;

        // CircleTimer表示準備
        circleTimerCanvasGroup.gameObject.SetActive(true);
        circleTimerCanvasGroup.alpha = 0f;

        var from = memorizeCountdownUI.CG;
        var to = circleTimerCanvasGroup;

        if (sec <= 0f)
        {
            from.alpha = 0f;
            memorizeCountdownUI.gameObject.SetActive(false);
            to.alpha = 1f;
            yield break;
        }

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sec);

            from.alpha = 1f - k;
            to.alpha = k;

            yield return null;
        }

        from.alpha = 0f;
        memorizeCountdownUI.gameObject.SetActive(false);
        to.alpha = 1f;
    }

    private IEnumerator RevealUnopenedCorrects(float fadeSec)
    {
        SetAllInteractable(false);

        foreach (var it in items)
        {
            if (it == null) continue;

            if (it.IsCorrect && !it.IsOpened)
            {
                // ✅ 正解画像だけを表示（カプセル見た目は消す）＋フェード
                yield return it.ForceShowCorrectOnlyFade(fadeSec);
            }
        }
    }




    public void SetRevealedVisible(bool visible)
    {
        if (targetCharaImage != null) targetCharaImage.enabled = visible;
    }

    private void ApplyIpLogo(MemoryIpPack pack)
    {
        Sprite s = (pack != null) ? pack.LogoSprite : null;

        if (missionLogoImage != null)
        {
            missionLogoImage.sprite = s;
            missionLogoImage.enabled = (s != null);
        }
        if (memorizeLogoImage != null)
        {
            memorizeLogoImage.sprite = s;
            memorizeLogoImage.enabled = (s != null);
        }
        if (playLogoImage != null)
        {
            playLogoImage.sprite = s;
            playLogoImage.enabled = (s != null);
        }
    }


}