using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSequencePlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image targetImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;

    [Header("Playback")]
    [SerializeField] private float fps = 24f;
    [SerializeField] private bool hideOnComplete = true;

    public RectTransform Rect => targetImage != null ? targetImage.rectTransform : null;

    private Coroutine playing;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public IEnumerator PlayOnceAndWait()
    {
        if (playing != null) StopCoroutine(playing);
        yield return CoPlayOnce();
        playing = null;
    }

    private IEnumerator CoPlayOnce()
    {
        if (targetImage == null || frames == null || frames.Length == 0 || fps <= 0f)
            yield break;

        gameObject.SetActive(true);

        float dt = 1f / fps;

        for (int i = 0; i < frames.Length; i++)
        {
            targetImage.sprite = frames[i];
            targetImage.enabled = (frames[i] != null);
            yield return new WaitForSeconds(dt);
        }

        if (hideOnComplete)
            gameObject.SetActive(false);
    }
}
