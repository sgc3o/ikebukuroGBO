using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IncentiveReturnConfirmPopupController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button returnButton; // もどる
    [SerializeField] private Button stayButton;   // もどらない

    [Header("Countdown UI")]
    [SerializeField] private CircularCountdownUI countdownUI;

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float clickHoldSeconds = 1.0f;
    [SerializeField] private float fadeOutSeconds = 1.0f;

    private IReturnConfirmOwner owner;
    private bool transitioning;
    private Coroutine countdownCo;

    // --- default visuals cache (押下見た目を毎回リセットするため) ---
    private Image returnImg;
    private Image stayImg;
    private Sprite returnDefaultSprite;
    private Sprite stayDefaultSprite;
    private Color returnDefaultColor;
    private Color stayDefaultColor;

    private void Awake()
    {
        // 初期見た目をキャッシュ（pressedSprite等で上書きされても戻せるように）
        returnImg = returnButton != null ? returnButton.targetGraphic as Image : null;
        stayImg = stayButton != null ? stayButton.targetGraphic as Image : null;

        if (returnImg != null)
        {
            returnDefaultSprite = returnImg.sprite;
            returnDefaultColor = returnImg.color;
        }

        if (stayImg != null)
        {
            stayDefaultSprite = stayImg.sprite;
            stayDefaultColor = stayImg.color;
        }

        // 置きっぱなし運用：初期は非表示
        HideImmediate();
    }

    public void Show(IReturnConfirmOwner ownerRef, float timeoutSeconds)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // ★ここが今回の肝：毎回、押下後の見た目を初期状態に戻す
        ResetButtonVisuals();

        owner = ownerRef;
        transitioning = false;

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturnPressed);
            returnButton.interactable = true;
        }

        if (stayButton != null)
        {
            stayButton.onClick.RemoveAllListeners();
            stayButton.onClick.AddListener(OnStayPressed);
            stayButton.interactable = true;
        }

        // 表示開始
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }

        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());

        // タイムアウト開始（unscaled）
        if (countdownCo != null) StopCoroutine(countdownCo);
        countdownCo = StartCoroutine(TimeoutRoutine(timeoutSeconds));
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (countdownCo != null) { StopCoroutine(countdownCo); countdownCo = null; }

        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    public void HideImmediate()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (countdownCo != null) { StopCoroutine(countdownCo); countdownCo = null; }

        transitioning = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private IEnumerator FadeInRoutine()
    {
        yield return FadeRoutine(0f, 1f, fadeInSeconds);
        if (canvasGroup != null) canvasGroup.interactable = true;
    }

    private IEnumerator FadeOutRoutine()
    {
        if (canvasGroup != null) canvasGroup.interactable = false;
        yield return FadeRoutine(1f, 0f, fadeOutSeconds);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void OnReturnPressed()
    {
        if (transitioning) return;
        transitioning = true;

        ApplyPressedVisual(returnButton);

        StartCoroutine(ExecuteThenClose(() =>
        {
            owner?.ReturnToHub();
        }));
    }

    private void OnStayPressed()
    {
        if (transitioning) return;
        transitioning = true;

        ApplyPressedVisual(stayButton);

        StartCoroutine(ExecuteThenClose(() =>
        {
            owner?.ClosePopupAndResume();
        }));
    }

    private IEnumerator ExecuteThenClose(System.Action action)
    {
        // 押した感を見せる
        yield return new WaitForSecondsRealtime(clickHoldSeconds);

        // カウント停止して閉じる
        if (countdownCo != null) { StopCoroutine(countdownCo); countdownCo = null; }

        yield return FadeOutRoutine();
        action?.Invoke();
    }

    private IEnumerator FadeRoutine(float from, float to, float seconds)
    {
        if (canvasGroup == null) yield break;

        seconds = Mathf.Max(0.01f, seconds);

        float t = 0f;
        canvasGroup.alpha = from;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / seconds);
            canvasGroup.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private IEnumerator TimeoutRoutine(float timeoutSeconds)
    {
        float total = Mathf.Max(0.01f, timeoutSeconds);
        float remaining = total;

        // 初期表示
        if (countdownUI != null) countdownUI.StartCountdownFromRemaining(total, remaining);

        while (remaining > 0f && !transitioning)
        {
            remaining -= Time.unscaledDeltaTime;
            if (remaining < 0f) remaining = 0f;

            // 円を更新（既存APIに合わせる）
            if (countdownUI != null) countdownUI.StartCountdownFromRemaining(total, remaining);

            yield return null;
        }

        if (!transitioning)
        {
            transitioning = true;
            yield return FadeOutRoutine();
            owner?.ReturnToHub();
        }
    }

    private void ResetButtonVisuals()
    {
        // 再インタラクト可能に（Show内でもtrueにするが安全のため）
        if (returnButton != null) returnButton.interactable = true;
        if (stayButton != null) stayButton.interactable = true;

        // SpriteSwapで pressedSprite を直接 sprite に上書きしているので、必ず戻す
        if (returnImg != null)
        {
            if (returnDefaultSprite != null) returnImg.sprite = returnDefaultSprite;
            returnImg.color = returnDefaultColor;
        }

        if (stayImg != null)
        {
            if (stayDefaultSprite != null) stayImg.sprite = stayDefaultSprite;
            stayImg.color = stayDefaultColor;
        }
    }

    private void ApplyPressedVisual(Button btn)
    {
        if (btn == null) return;

        // 連打防止
        if (returnButton != null) returnButton.interactable = false;
        if (stayButton != null) stayButton.interactable = false;

        var img = btn.targetGraphic as Image;
        if (img == null) return;

        if (btn.transition == Selectable.Transition.SpriteSwap)
        {
            var ps = btn.spriteState.pressedSprite;
            if (ps != null) img.sprite = ps;
        }
        else if (btn.transition == Selectable.Transition.ColorTint)
        {
            img.color = btn.colors.pressedColor;
        }
    }
}
