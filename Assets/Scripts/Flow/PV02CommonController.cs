using System.Collections;
using UnityEngine;

public class PV02CommonController : MonoBehaviour
{
    [Header("Panels (CanvasGroup)")]
    [SerializeField] private CanvasGroup openingPanel;
    [SerializeField] private CanvasGroup howToPlayPanel;

    [Header("Fade Settings")]
    [SerializeField, Range(0.05f, 2f)] private float fadeDuration = 0.25f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Flow (Same Scene)")]
    [SerializeField] private ScreenFlowController flow;

    private bool transitioning;

    private void Awake()
    {
        if (flow == null) flow = ScreenFlowController.I;
    }

    private void Start()
    {
        // 初期：Openingのみ
        SetVisibleInstant(openingPanel, true);
        SetVisibleInstant(howToPlayPanel, false);
    }

    // Opening → HowTo
    public void OnStartPressed()
    {
        //Debug.Log("OnStartPressed fired!");

        if (transitioning) return;
        StartCoroutine(CrossFade(openingPanel, howToPlayPanel));
    }

    // HowTo → Game（同一シーン内切替）
    public void OnOkPressed()
    {
        if (transitioning) return;

        if (flow == null) flow = ScreenFlowController.I;
        if (flow == null)
        {
            //Debug.LogError("[PV02CommonController] ScreenFlowController が見つかりません。");
            return;
        }

        flow.ShowGame();
    }

    // HowTo → Opening（戻るボタンがある場合）
    public void OnBackToOpening()
    {
        if (transitioning) return;
        StartCoroutine(CrossFade(howToPlayPanel, openingPanel));
    }

    private IEnumerator CrossFade(CanvasGroup from, CanvasGroup to)
    {
        if (from == null || to == null) yield break;

        transitioning = true;

        SetInteractable(from, false);
        SetInteractable(to, false);

        to.gameObject.SetActive(true);
        to.alpha = 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / fadeDuration);
            float eased = (fadeCurve != null) ? fadeCurve.Evaluate(u) : u;

            from.alpha = Mathf.Lerp(1f, 0f, eased);
            to.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        from.alpha = 0f;
        to.alpha = 1f;

        from.gameObject.SetActive(false);
        SetInteractable(to, true);

        transitioning = false;
    }

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
}
