using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DebugIncentiveJumpConfirmPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Fade")]
    [SerializeField] private float fadeInSeconds = 0.2f;
    [SerializeField] private float fadeOutSeconds = 0.2f;
    [SerializeField] private float clickHoldSeconds = 0.1f;

    private Action onConfirm;
    private Action onCancel;

    private bool busy = false;
    private Coroutine runningCo;

    private void Awake()
    {
        HideImmediate();
    }

    public void Show(Action onConfirm, Action onCancel = null)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        busy = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
            confirmButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnClickCancel);
            cancelButton.interactable = true;
        }

        if (runningCo != null)
        {
            StopCoroutine(runningCo);
        }

        runningCo = StartCoroutine(FadeRoutine(0f, 1f, fadeInSeconds, setInteractableAtEnd: true));
    }

    public void Hide()
    {
        if (runningCo != null)
        {
            StopCoroutine(runningCo);
        }

        runningCo = StartCoroutine(HideRoutine());
    }

    public void HideImmediate()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnClickConfirm()
    {
        if (busy) return;
        busy = true;

        if (confirmButton != null) confirmButton.interactable = false;
        if (cancelButton != null) cancelButton.interactable = false;

        if (runningCo != null)
        {
            StopCoroutine(runningCo);
        }

        runningCo = StartCoroutine(ExecuteThenClose(onConfirm));
    }

    private void OnClickCancel()
    {
        if (busy) return;
        busy = true;

        if (confirmButton != null) confirmButton.interactable = false;
        if (cancelButton != null) cancelButton.interactable = false;

        if (runningCo != null)
        {
            StopCoroutine(runningCo);
        }

        runningCo = StartCoroutine(ExecuteThenClose(onCancel));
    }

    private IEnumerator ExecuteThenClose(Action callback)
    {
        yield return new WaitForSecondsRealtime(clickHoldSeconds);
        yield return HideRoutine();
        callback?.Invoke();
    }

    private IEnumerator HideRoutine()
    {
        yield return FadeRoutine(1f, 0f, fadeOutSeconds, setInteractableAtEnd: false);
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, bool setInteractableAtEnd)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);

        canvasGroup.alpha = from;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        canvasGroup.alpha = to;
        canvasGroup.interactable = setInteractableAtEnd;
        canvasGroup.blocksRaycasts = setInteractableAtEnd;

        runningCo = null;
    }
}