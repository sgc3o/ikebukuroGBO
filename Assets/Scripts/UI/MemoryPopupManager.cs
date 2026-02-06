using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MemoryPopupManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;   // PopupLayerのCanvasGroup
    [SerializeField] private Image popupImage;          // 表示するImage

    [Header("Sprites")]
    [SerializeField] private Sprite gameStartSprite;
    [SerializeField] private Sprite finishSprite;

    [Header("Timing")]
    [SerializeField] private float showSec = 1.5f;
    [SerializeField] private float fadeOutSec = 0.5f;

    private Coroutine routine;

    public bool IsShowing
    {
        get
        {
            if (routine != null) return true;
            if (canvasGroup != null && canvasGroup.alpha > 0.01f) return true;
            if (popupImage != null && popupImage.enabled) return true;
            return false;
        }
    }

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        popupImage = GetComponentInChildren<Image>(true);
    }

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (popupImage != null)
            popupImage.enabled = false;
    }

    public void ShowGameStart() => Show(gameStartSprite);
    public void ShowFinish() => Show(finishSprite);

    public void Show(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[MemoryPopupManager] sprite is null");
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(sprite));
    }

    private IEnumerator ShowRoutine(Sprite sprite)
    {
        popupImage.sprite = sprite;

        // NativeSizeしたい場合はON（画像が576ぴったりなら不要）
        // popupImage.SetNativeSize();

        popupImage.enabled = true;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        yield return new WaitForSeconds(showSec);

        float t = 0f;
        float startA = canvasGroup.alpha;
        while (t < fadeOutSec)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startA, 0f, t / fadeOutSec);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        popupImage.enabled = false;
        routine = null;
    }

    // MemoryStageManagerから「待てる」用（既存PopupManager互換）
    public Coroutine ShowAndWait(Sprite sprite, MonoBehaviour owner)
    {
        if (routine != null) owner.StopCoroutine(routine);
        routine = owner.StartCoroutine(ShowRoutine(sprite));
        return routine;
    }

    // 便利：StageManager側で呼びやすい
    public Coroutine ShowGameStartAndWait(MonoBehaviour owner) => ShowAndWait(gameStartSprite, owner);
    public Coroutine ShowFinishAndWait(MonoBehaviour owner) => ShowAndWait(finishSprite, owner);
}
