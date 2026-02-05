using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountdownUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image numberImage;

    [Header("Sprites index = number")]
    [Tooltip("sprites[0]Ç™0ÅAsprites[5]Ç™5 ÇÃÇÊÇ§Ç…ì¸ÇÍÇÈ")]
    [SerializeField] private Sprite[] numberSprites; // 0..5

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void ShowNumber(int number)
    {
        if (numberImage == null) return;
        if (numberSprites == null) return;
        if (number < 0 || number >= numberSprites.Length) return;

        numberImage.sprite = numberSprites[number];
        numberImage.enabled = (numberSprites[number] != null);

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeOut(float sec)
    {
        if (canvasGroup == null || sec <= 0f)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            yield break;
        }

        float t = 0f;
        float startA = canvasGroup.alpha;
        while (t < sec)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startA, 0f, t / sec);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
