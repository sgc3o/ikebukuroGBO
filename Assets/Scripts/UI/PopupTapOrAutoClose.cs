using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PopupTapOrAutoClose : MonoBehaviour
{
    [Header("Auto Close")]
    [Tooltip("自動クローズ秒（0以下で無効）")]
    [SerializeField] private float autoCloseSec = 1.0f;

    [Header("Tap To Close (GameStart only)")]
    [Tooltip("1タップで閉じる（Finishには使わない）")]
    [SerializeField] private bool enableTapToClose = true;

    [Tooltip("画面全体を覆う透明Button。無ければ自動で探す")]
    [SerializeField] private Button tapAreaButton;

    public bool IsOpen { get; private set; }

    Action _onClosed;
    Coroutine _co;

    void Reset()
    {
        tapAreaButton = GetComponentInChildren<Button>(true);
    }

    void Awake()
    {
        if (tapAreaButton == null) tapAreaButton = GetComponentInChildren<Button>(true);

        if (tapAreaButton != null)
        {
            tapAreaButton.onClick.RemoveListener(OnTap);
            tapAreaButton.onClick.AddListener(OnTap);
        }

        gameObject.SetActive(false);
    }

    public void Open(Action onClosed)
    {
        _onClosed = onClosed;
        IsOpen = true;
        gameObject.SetActive(true);

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(AutoClose());
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;

        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }

        gameObject.SetActive(false);

        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
    }

    IEnumerator AutoClose()
    {
        if (autoCloseSec <= 0f) yield break;

        float t = 0f;
        while (t < autoCloseSec && IsOpen)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (IsOpen) Close();
    }

    void OnTap()
    {
        if (!enableTapToClose) return;
        Close();
    }
}