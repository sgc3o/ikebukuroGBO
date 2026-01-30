using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PV02CommonController : MonoBehaviour
{
    [Header("Panels (CanvasGroup)")]
    [SerializeField] private CanvasGroup openingPanel;
    [SerializeField] private CanvasGroup howToPlayPanel;

    [Header("Fade Settings")]
    [SerializeField, Range(0.05f, 2f)] private float fadeDuration = 0.25f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Next Scene")]
    [SerializeField] private string nextGameSceneName = "PV03_Virusweets_Game";

    private bool transitioning;

    private void Awake()
    {
        // 念のため null 防止（無ければ追加）
        if (openingPanel != null) EnsureCanvasGroup(openingPanel.gameObject);
        if (howToPlayPanel != null) EnsureCanvasGroup(howToPlayPanel.gameObject);
    }

    private void Start()
    {
        // 起動時：Openingのみ表示（即時）
        SetVisibleInstant(openingPanel, true);
        SetVisibleInstant(howToPlayPanel, false);
    }

    // OpeningのStartボタン
    public void OnStartPressed()
    {
        if (transitioning) return;
        StartCoroutine(CrossFade(openingPanel, howToPlayPanel));
    }

    // HowToPlayのOKボタン
    public void OnOkPressed()
    {
        if (transitioning) return;
        StartCoroutine(FadeOutAndLoad(howToPlayPanel, nextGameSceneName));
    }

    // HowToPlay → Opening に戻すボタンがある場合
    public void OnBackToOpening()
    {
        if (transitioning) return;
        StartCoroutine(CrossFade(howToPlayPanel, openingPanel));
    }

    // --------------------
    // Cross Fade
    // --------------------
    private IEnumerator CrossFade(CanvasGroup from, CanvasGroup to)
    {
        if (from == null || to == null) yield break;

        transitioning = true;

        // 切替中は両方入力停止
        SetInteractable(from, false);
        SetInteractable(to, false);

        // to を先に表示＆透明スタート（ここが “背景見えない” の肝）
        to.gameObject.SetActive(true);
        to.alpha = 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeDuration);
            float eased = (fadeCurve != null) ? fadeCurve.Evaluate(u) : u;

            // 同時にやる＝クロスフェード
            from.alpha = Mathf.Lerp(1f, 0f, eased);
            to.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        from.alpha = 0f;
        to.alpha = 1f;

        // from は完全に消してOK
        from.gameObject.SetActive(false);

        // to の入力だけ復帰
        SetInteractable(to, true);

        transitioning = false;
    }

    private IEnumerator FadeOutAndLoad(CanvasGroup from, string sceneName)
    {
        if (from == null) yield break;

        transitioning = true;
        SetInteractable(from, false);

        // フェードアウト（単独）
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeDuration);
            float eased = (fadeCurve != null) ? fadeCurve.Evaluate(u) : u;

            from.alpha = Mathf.Lerp(1f, 0f, eased);
            yield return null;
        }

        from.alpha = 0f;
        from.gameObject.SetActive(false);

        SceneManager.LoadScene(sceneName);
    }

    // --------------------
    // Helpers
    // --------------------
    private void SetVisibleInstant(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(visible);
        cg.alpha = visible ? 1f : 0f;
        SetInteractable(cg, visible);
    }

    private void SetInteractable(CanvasGroup cg, bool on)
    {
        if (cg == null) return;
        cg.interactable = on;
        cg.blocksRaycasts = on;
    }

    private void EnsureCanvasGroup(GameObject go)
    {
        if (go == null) return;
        if (go.GetComponent<CanvasGroup>() == null)
            go.AddComponent<CanvasGroup>();
    }
}
