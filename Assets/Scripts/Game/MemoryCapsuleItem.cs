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
    [SerializeField] private SpriteSequencePlayer openSeq;
    [SerializeField] private SpriteSequencePlayer closeSeq;
    [SerializeField] private bool hideRevealedAtCloseStart = true; // Close開始でキャラを消す
    [SerializeField] private float closeHideScaleSec = 0.15f;       // 縮小で消す時間（0なら即）

    [Header("Timing")]
    [SerializeField] private float retractSec = 0.5f; // ひっこめ動画の長さ
    [SerializeField] private float closeSec = 0.5f;   // 閉じる動画の長さ

    [Header("Tap Open Flash (Gameplay Only)")]
    [SerializeField] private bool enableOpenFlashInGameplay = true;
    [SerializeField] private Sprite openFlashSprite;
    [SerializeField] private float openFlashSec = 0.1f;

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

        if (openSeq != null) openSeq.gameObject.SetActive(false);
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);
    }

    public void SetInteractable(bool value)
    {
        if (button != null) button.interactable = value;
    }

    public IEnumerator PlayRetractAndRevealForMemorize()
    {
        // Memorize開始時専用：必ずOpen連番（openSeq）を使う。Flashは使わない。
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (isOpened) yield break;
        isOpened = true;

        if (capsuleImage != null) capsuleImage.enabled = false;

        if (openSeq != null)
            yield return openSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(retractSec);

        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null)
            yield return ScaleIn(revealRect, scaleInSec, scaleCurve);
    }

    private IEnumerator PlayOpenFlashIfEnabled()
    {
        if (!enableOpenFlashInGameplay) yield break;
        if (openFlashSprite == null) yield break;
        if (capsuleImage == null) yield break;

        bool wasActive = capsuleImage.gameObject.activeSelf;
        if (!wasActive) capsuleImage.gameObject.SetActive(true);

        var prevSprite = capsuleImage.sprite;
        var prevEnabled = capsuleImage.enabled;

        capsuleImage.sprite = openFlashSprite;
        capsuleImage.enabled = true;

        float sec = Mathf.Max(0f, openFlashSec);
        if (sec > 0f) yield return new WaitForSeconds(sec);
        else yield return null;

        capsuleImage.enabled = prevEnabled;
        capsuleImage.sprite = prevSprite;

        if (!wasActive) capsuleImage.gameObject.SetActive(false);
    }

    public IEnumerator PlayRetractAndReveal()
    {
        // Gameplayタップ想定：Flashを優先（設定されていれば）。無ければ従来通りopenSeq。
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (isOpened) yield break;
        isOpened = true;

        if (capsuleImage != null) capsuleImage.enabled = false;

        if (enableOpenFlashInGameplay && openFlashSprite != null)
        {
            yield return PlayOpenFlashIfEnabled();
        }
        else
        {
            if (openSeq != null)
                yield return openSeq.PlayOnceAndWait();
            else
                yield return new WaitForSeconds(retractSec);
        }

        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null)
            yield return ScaleIn(revealRect, scaleInSec, scaleCurve);
    }

    public IEnumerator PlayClose()
    {
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        if (hideRevealedAtCloseStart)
        {
            if (closeHideScaleSec > 0f && revealRect != null)
                yield return ScaleOut(revealRect, closeHideScaleSec);

            if (revealedImage != null) revealedImage.enabled = false;
            if (revealRect != null) revealRect.localScale = Vector3.zero;
        }

        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        if (!hideRevealedAtCloseStart)
        {
            if (revealedImage != null) revealedImage.enabled = false;
            if (revealRect != null) revealRect.localScale = Vector3.zero;
        }

        isOpened = false;
    }

    private IEnumerator ScaleOut(RectTransform rt, float sec)
    {
        if (rt == null) yield break;
        if (sec <= 0f)
        {
            rt.localScale = Vector3.zero;
            yield break;
        }

        float t = 0f;
        Vector3 start = rt.localScale;
        while (t < sec)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / sec);
            rt.localScale = Vector3.Lerp(start, Vector3.zero, n);
            yield return null;
        }
        rt.localScale = Vector3.zero;
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

    // ===== 以降、StageManager側の失敗演出などで使っている補助 =====

    public IEnumerator ForceRevealFade(float sec)
    {
        if (isOpened) yield break;
        isOpened = true;

        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;

        var cg = revealedImage != null ? revealedImage.GetComponent<CanvasGroup>() : null;
        if (cg == null && revealedImage != null) cg = revealedImage.gameObject.AddComponent<CanvasGroup>();
        if (cg == null) yield break;

        cg.alpha = 0f;

        if (sec <= 0f)
        {
            cg.alpha = 1f;
            yield break;
        }

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / sec);
            yield return null;
        }

        cg.alpha = 1f;
    }

    public void SetRevealedVisible(bool visible)
    {
        if (revealedImage != null) revealedImage.enabled = visible;
    }

    public IEnumerator PlayCloseAndLeaveRevealed()
    {
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        if (revealedImage != null) revealedImage.enabled = false;
        if (revealRect != null) revealRect.localScale = Vector3.zero;

        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        if (capsuleImage != null) capsuleImage.enabled = false;
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;

        if (button != null) button.interactable = false;
        isOpened = true;
    }

    public IEnumerator PlayCloseOnly()
    {
        SetRevealedVisible(false);

        if (closeSeq != null)
        {
            closeSeq.gameObject.SetActive(true);
            yield return closeSeq.PlayOnceAndWait();
            closeSeq.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        if (capsuleImage != null) capsuleImage.enabled = false;
        if (button != null) button.interactable = false;
    }

    public IEnumerator PlayCloseOnlyThenDisappear()
    {
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        if (revealedImage != null) revealedImage.enabled = false;
        if (revealRect != null) revealRect.localScale = Vector3.zero;

        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        if (capsuleImage != null) capsuleImage.enabled = false;
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (button != null) button.interactable = false;
        isOpened = true;
    }

    public void ShowRevealedFinal()
    {
        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;
    }

    public void ForceShowCorrectOnlyImmediate()
    {
        if (capsuleImage != null) capsuleImage.enabled = false;

        if (openSeq != null) openSeq.gameObject.SetActive(false);
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;

        if (button != null) button.interactable = false;
    }

    public IEnumerator ForceShowCorrectOnlyFade(float sec)
    {
        ForceShowCorrectOnlyImmediate();

        CanvasGroup cg = null;
        if (revealedImage != null) cg = revealedImage.GetComponent<CanvasGroup>();
        if (cg == null && revealRect != null) cg = revealRect.GetComponent<CanvasGroup>();

        if (cg == null) yield break;

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / sec);
            yield return null;
        }
        cg.alpha = 1f;
    }
}
