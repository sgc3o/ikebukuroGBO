using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IdleRecoveryPopupController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button returnButton; // 戻る
    [SerializeField] private Button stayButton;   // 戻らない

    [Header("Countdown UI")]
    [SerializeField] private CircularCountdownUI countdownUI;

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float clickHoldSeconds = 1.0f; // 押した感を見せる時間
    [SerializeField] private float fadeOutSeconds = 1.0f;

    private IdleRecoveryManager owner;
    private bool transitioning;

    public void Bind(IdleRecoveryManager manager)
    {
        owner = manager;

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturnPressed);
        }

        if (stayButton != null)
        {
            stayButton.onClick.RemoveAllListeners();
            stayButton.onClick.AddListener(OnStayPressed);
        }

        // 初期：フェードイン準備
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;   // 裏操作ブロックは即ON
            canvasGroup.interactable = false;    // フェードイン中は触れない
        }

        StartCoroutine(FadeInRoutine());
    }

    public void StartReturnCountdown(float seconds)
    {
        if (countdownUI != null) countdownUI.StartCountdown(seconds);
    }



    public bool TickCountdown(float dt)
    {
        if (countdownUI == null) return false;
        return countdownUI.Tick(dt);
    }

    private IEnumerator FadeInRoutine()
    {
        yield return FadeRoutine(0f, 1f, fadeInSeconds);

        if (canvasGroup != null) canvasGroup.interactable = true;
    }

    private void OnReturnPressed()
    {
        if (transitioning) return;
        transitioning = true;

        // 押した見た目を固定
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
        // ここで「押した感」を見せる
        yield return new WaitForSecondsRealtime(clickHoldSeconds);

        // フェードアウト
        if (canvasGroup != null) canvasGroup.interactable = false;
        yield return FadeRoutine(1f, 0f, fadeOutSeconds);

        action?.Invoke();
    }

    private IEnumerator FadeRoutine(float from, float to, float seconds)
    {
        if (canvasGroup == null)
            yield break;

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

    private void ApplyPressedVisual(Button btn)
    {
        if (btn == null) return;

        // 連打防止
        if (returnButton != null) returnButton.interactable = false;
        if (stayButton != null) stayButton.interactable = false;

        // “押した感”の固定（SpriteSwap/ColorTint両対応）
        var img = btn.targetGraphic as Image;
        if (img == null) return;

        if (btn.transition == Selectable.Transition.SpriteSwap)
        {
            var ps = btn.spriteState.pressedSprite;
            if (ps != null) img.sprite = ps;
        }
        else if (btn.transition == Selectable.Transition.ColorTint)
        {
            var c = btn.colors.pressedColor;
            img.color = c;
        }
    }
    public void StartReturnCountdownFromRemaining(float total, float remaining)
    {
        if (countdownUI != null) countdownUI.StartCountdownFromRemaining(total, remaining);
    }

    // 毎フレーム残り時間に追従させたい時用
    public void SetRemaining(float total, float remaining)
    {
        if (countdownUI != null) countdownUI.StartCountdownFromRemaining(total, remaining);
    }

}
