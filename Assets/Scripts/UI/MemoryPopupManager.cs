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

    [Header("Rule (Common)")]
    [Tooltip("表示秒（この秒数で自動的に閉じる）")]
    [SerializeField] private float showSec = 1.0f;

    [Tooltip("閉じるフェード秒（0なら即消し）")]
    [SerializeField] private float fadeOutSec = 0.2f;

    [Tooltip("タップで閉じる時のフェード秒（0なら即消し）")]
    [SerializeField] private float tapFadeOutSec = 0.1f;

    [Tooltip("表示中に1タップで即閉じする")]
    [SerializeField] private bool tapToClose = true;

    [Tooltip("表示直後、この秒数だけタップを無視（直前のタップ貫通事故防止）")]
    [SerializeField] private float tapIgnoreSec = 0.1f;

    private Coroutine routine;
    private float shownAtUnscaledTime;

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
        HideImmediate();
    }

    private void Update()
    {
        if (!tapToClose) return;
        if (!IsShowing) return;

        // 直前のタップで即閉じないようガード
        if (Time.unscaledTime - shownAtUnscaledTime < tapIgnoreSec) return;

        // タッチパネルでも Mouse として入ってくる想定
        if (Input.GetMouseButtonDown(0))
        {
            Close();
        }
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
        shownAtUnscaledTime = Time.unscaledTime;

        popupImage.sprite = sprite;
        popupImage.enabled = true;

        canvasGroup.alpha = 1f;

        // 下を触れないようにブロック（タップで閉じるのは Update で拾う）
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        // timeScale=0 でも進むように RealTime
        if (showSec > 0f)
            yield return new WaitForSecondsRealtime(showSec);

        // 自動クローズ
        yield return FadeOutRealtime(fadeOutSec);

        HideImmediate();
        routine = null;
    }

    private IEnumerator FadeOutRealtime(float sec)
    {
        sec = Mathf.Max(0f, sec);
        if (sec <= 0f)
        {
            canvasGroup.alpha = 0f;
            yield break;
        }

        float t = 0f;
        float startA = canvasGroup.alpha;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / sec);
            canvasGroup.alpha = Mathf.Lerp(startA, 0f, k);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    public void Close()
    {
        if (!IsShowing) return;

        // 進行中の自動表示ルーチンを止めて、今からフェードして閉じる
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        yield return FadeOutRealtime(tapFadeOutSec);
        HideImmediate();
    }

    private void HideImmediate()
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

    // ---- 既存互換（StageManagerが owner で回してても壊れないようにする） ----
    public Coroutine ShowAndWait(Sprite sprite, MonoBehaviour owner)
    {
        Show(sprite);
        return routine;
    }

    public Coroutine ShowGameStartAndWait(MonoBehaviour owner) => ShowAndWait(gameStartSprite, owner);
    public Coroutine ShowFinishAndWait(MonoBehaviour owner) => ShowAndWait(finishSprite, owner);
}