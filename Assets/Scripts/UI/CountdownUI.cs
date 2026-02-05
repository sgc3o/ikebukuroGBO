using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image numberImage;
    [SerializeField] private Sprite[] numberSprites;

    [Header("Timing")]
    [SerializeField] private float holdLastNumberSec = 0.35f;


    Coroutine playing;

    void Awake()
    {
        HideImmediate();
    }

    public void ShowImmediate()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void HideImmediate()
    {
        if (playing != null)
        {
            StopCoroutine(playing);
            playing = null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public IEnumerator Play(int seconds)
    {
        gameObject.SetActive(true);
        SetAlpha(1f);

        for (int s = seconds; s >= 0; s--)
        {
            ShowNumber(s);

            if (s > 0)
            {
                yield return new WaitForSeconds(1f);
            }
            else
            {
                // ★ 0だけ少し見せる
                yield return new WaitForSeconds(holdLastNumberSec);
            }
        }
    }


    // ===== Compatibility methods for older MemoryStageManager calls =====

    // StageManager から alpha を直接いじりたい時用（nullでも落ちない）
    public void SetAlpha(float a)
    {
        if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(a);
    }

    // 指定した数字を表示（秒数カウントの途中で呼ぶ用）
    public void ShowNumber(int number)
    {
        if (numberImage == null || numberSprites == null) return;

        // numberSprites は [0..N] を想定
        if (number < 0 || number >= numberSprites.Length) return;

        numberImage.sprite = numberSprites[number];
        if (!numberImage.enabled) numberImage.enabled = true;

        // 表示されてない時は表示にする
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (canvasGroup != null && canvasGroup.alpha <= 0f) canvasGroup.alpha = 1f;
    }

    // フェードアウト（必要なければ duration=0 で即消す運用もOK）
    public IEnumerator FadeOut(float duration)
    {
        if (duration <= 0f)
        {
            // 即消し
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            yield break;
        }

        if (canvasGroup == null)
        {
            // CanvasGroup が無いなら即消し
            gameObject.SetActive(false);
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(start, 0f, k);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

}
