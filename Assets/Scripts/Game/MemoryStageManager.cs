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
    [SerializeField] private StageConfig[] stages;

    [Header("Scene Transition")]
    [SerializeField] private string incentiveSceneName = "S_MemoryIncentive";

    [Header("Panels Root")]
    [SerializeField] private CanvasGroup panelsCanvasGroup;
    [SerializeField] private GameObject missionIntroPanel;
    [SerializeField] private GameObject memorizePanel;
    [SerializeField] private GameObject gamePlayPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Mission Intro UI")]
    [SerializeField] private Sprite[] stageBadgeSprites;
    [SerializeField] private Image targetCharaImage;
    [SerializeField] private Image missionLogoImage;
    [SerializeField] private Image memorizeLogoImage;
    [SerializeField] private Image playLogoImage;

    [Header("UI Refs")]
    [SerializeField] private CountdownUI missionCountdownUI;
    [SerializeField] private CountdownUI memorizeCountdownUI;
    [SerializeField] private CountdownUI gamePlayCountdownUI; // Gameplay用（画像カウントダウン）
    [SerializeField] private float showZeroHoldSec = 0.35f;
    [SerializeField] private MemoryPopupManager popupManager;
    [SerializeField] private Image stageBadgeImage;

    [Header("HUD / HowTo Images")]
    [Tooltip("旧仕様: HUD側で共有して使うHowTo Image（未設定でもOK）")]
    [SerializeField] private Image howToImage;

    [Tooltip("新仕様: MemorizePanel側のHowTo Image（任意）。設定すると Memorize中はこちらが表示され、Play側は非表示になります。")]
    [SerializeField] private Image memorizeHowToImage;

    [Tooltip("新仕様: GamePlayPanel（HUD）側のHowTo Image（任意）。設定すると Play中はこちらが表示され、Memorize側は非表示になります。")]
    [SerializeField] private Image playHowToImage;

    [SerializeField] private Sprite howToMemorizeSprite;
    [SerializeField] private Sprite howToPlaySprite;

    [SerializeField] private Image hudStageBadgeImage;
    [SerializeField] private GameObject circleTimerRoot; // legacy（未使用なら未設定OK）
    [SerializeField] private Image hudTargetCharaImage;
    [SerializeField] private Image memorizeTargetCharaImage;
    private Sprite currentTargetSprite;

    [Header("Board")]
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private MemoryCapsuleItem capsulePrefab;
    [SerializeField] private Sprite capsuleClosedSprite;
    [SerializeField] private int rowsFixed = 3;
    [SerializeField] private SpriteSequencePlayer correctEffectPlayer;
    [SerializeField] private Canvas canvasForUI;

    [Header("Cross Fade Override (optional)")]
    [SerializeField] private bool useCrossFadeOverride = true;
    [SerializeField] private float crossFadeSecOverride = 0.25f;

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

        if (panelsCanvasGroup != null)
        {
            panelsCanvasGroup.alpha = 1f;
            panelsCanvasGroup.gameObject.SetActive(true);
        }

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

        BuildBoard(cfg);

        remainingCorrect = Mathf.Max(1, cfg.correctCount);
        Debug.Log($"[MemoryStageManager] BeginStage idx={currentStageIndex} correctCount={cfg.correctCount} remainingCorrect={remainingCorrect}");

        ShowOnly(missionIntroPanel);
        ApplyStageBadge(currentStageIndex);

        if (targetCharaImage != null)
        {
            targetCharaImage.sprite = (cfg.ipPack != null) ? cfg.ipPack.TargetSprite : null;
            targetCharaImage.enabled = (targetCharaImage.sprite != null);
        }

        currentTargetSprite = (cfg != null && cfg.ipPack != null) ? cfg.ipPack.TargetSprite : null;
        ApplyIpLogo(cfg.ipPack);

        missionCountdownUI.gameObject.SetActive(true);
        missionCountdownUI.SetAlpha(1f);

        state = State.MissionIntro;

        SetupMissionIntro(cfg, currentStageIndex);

        yield return missionCountdownUI.Play(cfg.missionCountdownSec);

        if (missionCountdownUI != null)
            yield return missionCountdownUI.FadeOut(0f);

        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.gameObject.SetActive(false);
            gamePlayCountdownUI.SetAlpha(0f);
        }

        if (circleTimerRoot != null) circleTimerRoot.SetActive(false);

        float fadeSec = useCrossFadeOverride ? crossFadeSecOverride : cfg.fadeToNextSec;
        yield return CrossFadePanels(missionIntroPanel, memorizePanel, fadeSec);

        SetActiveSafe(gamePlayPanel, true);
        SetActiveSafe(hudPanel, true);

        if (memorizeCountdownUI != null) memorizeCountdownUI.ShowImmediate();

        state = State.Memorize;
        SetHudModeMemorize();
        memorizeCountdownUI.ShowImmediate();
        memorizeCountdownUI.ShowNumber(cfg.memorizeCountdownSec);

        yield return RunMemorizeSequence(cfg);

        state = State.GameStartPopup;
        if (popupManager != null) popupManager.ShowGameStart();
        SetAllInteractable(false);

        yield return WaitPopupDone();

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

        state = State.Playing;
        SetHudModePlay();
        SetAllInteractable(true);

        yield return RunPlayingSequence(cfg);

        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.ShowImmediate();
            gamePlayCountdownUI.ShowNumber(0);
            yield return new WaitForSeconds(showZeroHoldSec);
        }

        state = State.FinishPopup;
        SetAllInteractable(false);
        if (popupManager != null) popupManager.ShowFinish();
        yield return WaitPopupDone();

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
            SceneTransition.Go(incentiveSceneName);
            return;
        }

        BeginStage(next);
    }

    private IEnumerator RunMemorizeSequence(StageConfig cfg)
    {
        SetAllInteractable(false);

        yield return new WaitForSeconds(cfg.retractSec);

        // ✅ Memorize：必ず OpenSeq で一斉リビール（Flashは絶対使わない）
        var reveal = new List<Coroutine>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            reveal.Add(StartCoroutine(it.PlayRetractAndRevealForMemorize()));
        }
        foreach (var c in reveal) yield return c;

        if (memorizeCountdownUI != null)
            yield return memorizeCountdownUI.Play(cfg.memorizeCountdownSec);
        else
            yield return new WaitForSeconds(cfg.memorizeCountdownSec);

        // ✅ Memorize終わり：一斉 Close（キャラは消える）
        var close = new List<Coroutine>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            close.Add(StartCoroutine(it.PlayClose()));
        }
        foreach (var c in close) yield return c;
    }

    private IEnumerator RunPlayingSequence(StageConfig cfg)
    {
        SetAllInteractable(true);

        int limitSec = Mathf.Max(1, Mathf.CeilToInt(cfg.playTimeLimitSec));
        playRemainSec = limitSec;

        Coroutine timerCo = null;
        bool timeUp = false;

        if (gamePlayCountdownUI != null)
        {
            gamePlayCountdownUI.gameObject.SetActive(true);
            gamePlayCountdownUI.ShowImmediate();
            gamePlayCountdownUI.ShowNumber(limitSec);

            timerCo = StartCoroutine(PlayGameplayCountdown(limitSec, () =>
            {
                timeUp = true;
                playRemainSec = 0f;
            }));
        }

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

        if (timerCo != null) StopCoroutine(timerCo);

        bool success = (remainingCorrect <= 0);

        if (!success)
        {
            SetAllInteractable(false);
            yield return RevealUnopenedCorrects(0.25f);
        }

        SetAllInteractable(false);
    }

    private IEnumerator PlayGameplayCountdown(int seconds, Action onTimeUp)
    {
        if (gamePlayCountdownUI == null)
            yield break;

        int s = Mathf.Max(1, seconds);
        while (s > 0)
        {
            if (stageEnding || remainingCorrect <= 0)
                yield break;

            yield return new WaitForSeconds(1f);
            s--;
            gamePlayCountdownUI.ShowNumber(s);
        }

        if (stageEnding || remainingCorrect <= 0)
            yield break;

        onTimeUp?.Invoke();
    }

    private void OnCapsuleClicked(MemoryCapsuleItem item)
    {
        if (state != State.Playing) return;
        if (item == null) return;
        if (item.IsOpened) return;

        StartCoroutine(HandleClick(item));
    }

    private IEnumerator HandleClick(MemoryCapsuleItem item)
    {
        SetAllInteractable(false);

        // ✅ Play中タップ：Gameplayルート（Flash優先、無ければOpenSeq）
        yield return item.PlayRetractAndReveal();

        if (item.IsCorrect)
        {
            remainingCorrect--;
            PlaySE(correctSE);

            yield return PlayCorrectEffectAtItem(item);

            if (remainingCorrect <= 0)
            {
                stageEnding = true;
                yield break;
            }
        }
        else
        {
            PlaySE(wrongSE);
            // 不正解もCloseしない（キャラ出しっぱなし）
        }

        if (playRemainSec <= 0f)
        {
            stageEnding = true;
            yield break;
        }

        SetAllInteractable(true);
    }

    private IEnumerator PlayCorrectEffectAtItem(MemoryCapsuleItem item)
    {
        if (correctEffectPlayer == null) yield break;

        RectTransform effectRect = correctEffectPlayer.Rect;
        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (effectRect == null || itemRect == null)
        {
            yield return correctEffectPlayer.PlayOnceAndWait();
            yield break;
        }

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
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] != null) Destroy(items[i].gameObject);
        }
        items.Clear();

        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cfg.columns;
        }
        if (gridRoot == null && grid != null) gridRoot = grid.transform;

        int totalSlots = rowsFixed * cfg.columns;

        List<SlotData> slotData = BuildSlotData(totalSlots, cfg.correctCount, cfg.ipPack);

        for (int i = 0; i < totalSlots; i++)
        {
            var d = slotData[i];

            var it = Instantiate(capsulePrefab, gridRoot);

            it.Setup(
                capsuleClosedSprite,
                d.revealedSprite,
                d.isCorrect,
                OnCapsuleClicked
            );

            it.SetInteractable(false);
            items.Add(it);
        }
    }

    private void SetupMissionIntro(StageConfig cfg, int stageIndex)
    {
        if (missionIntroPanel != null)
            missionIntroPanel.SetActive(true);

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

        Sprite target = null;
        if (cfg.ipPack != null)
            target = cfg.ipPack.TargetSprite;

        if (targetCharaImage != null)
        {
            targetCharaImage.sprite = target;
            targetCharaImage.enabled = (target != null);
        }
    }

    private List<SlotData> BuildSlotData(int totalSlots, int correctCount, MemoryIpPack pack)
    {
        var list = new List<SlotData>(totalSlots);

        if (pack == null || pack.Count == 0)
        {
            Debug.LogError("[MemoryStageManager] IpPack is missing or empty");
            for (int i = 0; i < totalSlots; i++)
                list.Add(new SlotData { isCorrect = false, revealedSprite = null });
            return list;
        }

        correctCount = Mathf.Clamp(correctCount, 0, totalSlots);

        Sprite target = pack.TargetSprite;

        for (int i = 0; i < correctCount; i++)
            list.Add(new SlotData { isCorrect = true, revealedSprite = target });

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

        //Debug.Log($"[MemoryStageManager] SetAllInteractable({value}) items={items?.Count}");
    }

    private IEnumerator WaitPopupDone()
    {
        if (popupManager == null) yield break;
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

    private void ShowOnly(GameObject panel)
    {
        HideAllPanels();
        if (panel) panel.SetActive(true);
    }

    private void HideAllPanels()
    {
        SetActiveSafe(missionIntroPanel, false);
        SetActiveSafe(memorizePanel, false);
        SetActiveSafe(gamePlayPanel, false);

        if (missionCountdownUI != null) SetActiveSafe(missionCountdownUI.gameObject, false);
        if (memorizeCountdownUI != null) SetActiveSafe(memorizeCountdownUI.gameObject, false);

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

    private Image GetLogoImageForPanel(GameObject panel)
    {
        if (panel == null) return null;
        if (panel == missionIntroPanel) return missionLogoImage;
        if (panel == memorizePanel) return memorizeLogoImage;
        if (panel == gamePlayPanel) return playLogoImage;
        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup(Component c)
    {
        if (c == null) return null;
        var go = c.gameObject;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private IEnumerator CrossFadePanels(GameObject fromPanel, GameObject toPanel, float sec)
    {
        var from = GetOrAddCanvasGroup(fromPanel);
        var to = GetOrAddCanvasGroup(toPanel);

        var fromLogoImg = GetLogoImageForPanel(fromPanel);
        var toLogoImg = GetLogoImageForPanel(toPanel);
        var fromLogo = GetOrAddCanvasGroup(fromLogoImg);
        var toLogo = GetOrAddCanvasGroup(toLogoImg);

        if (toPanel != null) toPanel.SetActive(true);
        if (toLogoImg != null) toLogoImg.gameObject.SetActive(true);

        if (toLogo != null)
        {
            toLogo.alpha = 0f;
            toLogo.interactable = false;
            toLogo.blocksRaycasts = false;
        }
        if (fromLogo != null)
        {
            fromLogo.alpha = 1f;
            fromLogo.interactable = false;
            fromLogo.blocksRaycasts = false;
        }

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
            if (fromLogoImg != null) fromLogoImg.gameObject.SetActive(false);

            if (toLogo != null)
            {
                toLogo.alpha = 1f;
                toLogo.interactable = true;
                toLogo.blocksRaycasts = true;
            }
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
            if (fromLogo != null) fromLogo.alpha = Mathf.Lerp(1f, 0f, k);
            if (toLogo != null) toLogo.alpha = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        if (from != null) from.alpha = 0f;
        if (fromPanel != null) fromPanel.SetActive(false);
        if (fromLogo != null) fromLogo.alpha = 0f;
        if (fromLogoImg != null) fromLogoImg.gameObject.SetActive(false);

        if (toLogo != null)
        {
            toLogo.alpha = 1f;
            toLogo.interactable = true;
            toLogo.blocksRaycasts = true;
        }
        if (to != null)
        {
            to.alpha = 1f;
            to.interactable = true;
            to.blocksRaycasts = true;
        }
    }

    private void ApplyHowToVisibility(bool isMemorize)
    {
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

            if (howToImage != null)
                howToImage.enabled = false;

            return;
        }

        if (howToImage != null)
        {
            howToImage.sprite = isMemorize ? howToMemorizeSprite : howToPlaySprite;
            howToImage.enabled = (howToImage.sprite != null);
        }
    }

    private void SetHudModeMemorize()
    {
        ApplyHowToVisibility(isMemorize: true);

        if (hudStageBadgeImage != null && stageBadgeSprites != null &&
            currentStageIndex >= 0 && currentStageIndex < stageBadgeSprites.Length)
        {
            hudStageBadgeImage.sprite = stageBadgeSprites[currentStageIndex];
            hudStageBadgeImage.enabled = (hudStageBadgeImage.sprite != null);
        }

        if (memorizeCountdownUI != null) memorizeCountdownUI.gameObject.SetActive(true);
        if (gamePlayCountdownUI != null) gamePlayCountdownUI.gameObject.SetActive(false);
        if (circleTimerRoot != null) circleTimerRoot.SetActive(false);

        if (hudTargetCharaImage != null)
        {
            hudTargetCharaImage.enabled = false;
        }

        if (memorizeTargetCharaImage != null)
        {
            memorizeTargetCharaImage.sprite = currentTargetSprite;
            memorizeTargetCharaImage.enabled = (currentTargetSprite != null);
            if (currentTargetSprite != null)
                memorizeTargetCharaImage.transform.SetAsLastSibling();
        }
    }

    private void SetHudModePlay()
    {
        ApplyHowToVisibility(isMemorize: false);

        if (hudStageBadgeImage != null && stageBadgeSprites != null &&
            currentStageIndex >= 0 && currentStageIndex < stageBadgeSprites.Length)
        {
            hudStageBadgeImage.sprite = stageBadgeSprites[currentStageIndex];
            hudStageBadgeImage.enabled = (hudStageBadgeImage.sprite != null);
        }

        if (memorizeCountdownUI != null) memorizeCountdownUI.gameObject.SetActive(false);
        if (circleTimerRoot != null) circleTimerRoot.SetActive(false);
        if (gamePlayCountdownUI != null) gamePlayCountdownUI.gameObject.SetActive(true);

        if (memorizeTargetCharaImage != null)
        {
            memorizeTargetCharaImage.enabled = false;
        }

        if (hudTargetCharaImage != null)
        {
            hudTargetCharaImage.sprite = currentTargetSprite;
            hudTargetCharaImage.enabled = (currentTargetSprite != null);
            if (currentTargetSprite != null)
                hudTargetCharaImage.transform.SetAsLastSibling();
        }
    }

    private IEnumerator RevealUnopenedCorrects(float fadeSec)
    {
        SetAllInteractable(false);

        foreach (var it in items)
        {
            if (it == null) continue;

            if (it.IsCorrect && !it.IsOpened)
            {
                yield return it.ForceShowCorrectOnlyFade(fadeSec);
            }
        }
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
