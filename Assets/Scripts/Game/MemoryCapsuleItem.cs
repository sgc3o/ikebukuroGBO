using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MemoryCapsuleItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image capsuleImage;     // 閉じてる見た目（赤カプセル）
    [SerializeField] private Image revealedImage;    // キャラ表示用（普段OFFでもOK）

    [Header("Animation (Optional)")]
    [SerializeField] private Animator animator;      // なくても動く
    [SerializeField] private string retractTrigger = "Retract";
    [SerializeField] private string closeTrigger = "Close";

    [Header("Timing")]
    [SerializeField] private float retractSec = 0.5f; // ひっこめ動画の長さ
    [SerializeField] private float closeSec = 0.5f;   // 閉じる動画の長さ

    [Header("Reveal Scale")]
    [SerializeField] private RectTransform revealRect; // revealedImageのRectTransform
    [SerializeField] private float scaleInSec = 0.25f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isCorrect;
    private bool isOpened;
    private Action<MemoryCapsuleItem> onClick;

    public bool IsCorrect => isCorrect;
    public bool IsOpened => isOpened;

    public void Setup(Sprite hiddenSprite, Sprite revealedSprite, bool correct, Action<MemoryCapsuleItem> onClickCb)
    {
        isCorrect = correct;
        isOpened = false;
        onClick = onClickCb;

        if (capsuleImage != null) capsuleImage.sprite = hiddenSprite;
        if (revealedImage != null)
        {
            revealedImage.sprite = revealedSprite;
            revealedImage.enabled = false;
        }

        if (revealRect != null) revealRect.localScale = Vector3.zero;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
            button.interactable = true;
        }
    }

    public void SetInteractable(bool value)
    {
        if (button != null) button.interactable = value;
    }

    public IEnumerator PlayRetractAndReveal()
    {
        if (isOpened) yield break;
        isOpened = true;

        // ひっこめ動画
        if (animator != null && !string.IsNullOrEmpty(retractTrigger))
            animator.SetTrigger(retractTrigger);

        yield return new WaitForSeconds(retractSec);

        // キャラ表示（スケール0→1）
        if (revealedImage != null) revealedImage.enabled = true;

        if (revealRect != null)
            yield return ScaleIn(revealRect, scaleInSec, scaleCurve);
    }

    public IEnumerator PlayClose()
    {
        // 閉じる動画（必要なら）
        if (animator != null && !string.IsNullOrEmpty(closeTrigger))
            animator.SetTrigger(closeTrigger);

        yield return new WaitForSeconds(closeSec);

        // キャラ非表示
        if (revealedImage != null) revealedImage.enabled = false;
        if (revealRect != null) revealRect.localScale = Vector3.zero;

        isOpened = false;
    }

    private IEnumerator ScaleIn(RectTransform rt, float sec, AnimationCurve curve)
    {
        if (rt == null) yield break;
        if (sec <= 0f)
        {
            rt.localScale = Vector3.one;
            yield break;
        }

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / sec);
            float v = (curve != null) ? curve.Evaluate(n) : n;
            rt.localScale = Vector3.one * v;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
}
