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
        //Debug.Log($"CLICK: {name}");


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

    public IEnumerator PlayRetractAndReveal()
    {
        // Open再生するなら Closeは消す（白板防止）
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        if (isOpened) yield break;
        isOpened = true;

        // ★開く連番があるならそれを再生
        if (openSeq != null)
            yield return openSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(retractSec);

        // キャラ表示（スケール0→1）
        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null)
            yield return ScaleIn(revealRect, scaleInSec, scaleCurve);
    }


    public IEnumerator PlayClose()
    {
        // Close再生するなら Openは消す（白板防止）
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        // Close開始でキャラを隠す（重なり対策）
        if (hideRevealedAtCloseStart)
        {
            if (closeHideScaleSec > 0f && revealRect != null)
                yield return ScaleOut(revealRect, closeHideScaleSec);

            if (revealedImage != null) revealedImage.enabled = false;
            if (revealRect != null) revealRect.localScale = Vector3.zero;
        }

        // ★閉じる連番があるならそれを再生
        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        // hideRevealedAtCloseStart=false の場合はここで消す
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

    public IEnumerator ForceRevealFade(float sec)
    {
        // すでに開いてたら何もしない（押せてない＝未オープンだけ見せたい）
        if (isOpened) yield break;

        isOpened = true; // 以後クリックさせないため（StageManager側でもinteractableは切るけど保険）

        // キャラを見せる
        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;

        // CanvasGroupでフェード（無ければ付ける）
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
        // Close再生するならOpenは消す
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        // CloseSeq中はキャラを見せない（カプセル演出を優先）
        if (revealedImage != null) revealedImage.enabled = false;
        if (revealRect != null) revealRect.localScale = Vector3.zero;

        // CloseSeq 再生（=カプセルが消える動画）
        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        // ✅カプセル見た目は消す（もう戻ってこない）
        if (capsuleImage != null) capsuleImage.enabled = false;
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        // ✅正解画像を出しっぱなしにする
        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;

        // 以後クリックさせないならここで閉じてもOK（StageManager側でも可）
        if (button != null) button.interactable = false;

        // isOpened は「開いたまま」でOK（trueのままにする）
        isOpened = true;
    }

    public IEnumerator PlayCloseOnly()
    {
        // Close中はキャラ出さない
        SetRevealedVisible(false);

        // CloseSeq再生
        if (closeSeq != null)
        {
            closeSeq.gameObject.SetActive(true);
            yield return closeSeq.PlayOnceAndWait(); // あなたの待機方法に合わせて
            closeSeq.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // カプセル見た目を消して固定（消える動画の後の状態）
        if (capsuleImage != null) capsuleImage.enabled = false;

        // 必要ならクリック無効
        if (button != null) button.interactable = false;
    }


    public IEnumerator PlayCloseOnlyThenDisappear()
    {
        // Open再生するならCloseは消す
        if (openSeq != null) openSeq.gameObject.SetActive(false);

        // Close中はキャラを見せない
        if (revealedImage != null) revealedImage.enabled = false;
        if (revealRect != null) revealRect.localScale = Vector3.zero;

        // CloseSeq再生
        if (closeSeq != null)
            yield return closeSeq.PlayOnceAndWait();
        else
            yield return new WaitForSeconds(closeSec);

        // カプセル見た目を消して固定（「消える」完成形）
        if (capsuleImage != null) capsuleImage.enabled = false;
        if (closeSeq != null) closeSeq.gameObject.SetActive(false);

        // 以後クリックさせない
        if (button != null) button.interactable = false;

        // 「もう開いた扱い」にして連打防止
        isOpened = true;
    }

    // Close後に“最終表示”としてキャラを出す（スケールも戻す）
    public void ShowRevealedFinal()
    {
        if (revealedImage != null) revealedImage.enabled = true;
        if (revealRect != null) revealRect.localScale = Vector3.one;
    }



}
