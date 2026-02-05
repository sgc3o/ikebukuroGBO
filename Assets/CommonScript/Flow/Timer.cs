using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 無操作(Idle)監視タイマー。
/// - 入力があればタイマーをリセット
/// - timeoutSeconds経過で onTimeout を発火
/// 
/// 仕様:
/// - 全シーン共通で使える
/// - タイムアウト後は監視停止(必要なら再開可能)
/// </summary>
public class Timer : MonoBehaviour
{
    [Header("Config")]
    [Min(0.1f)] public float timeoutSeconds = 90f;
    public bool startOnEnable = true;

    [Header("Events")]
    public UnityEvent onTimeout;
    public UnityEvent onActivity; // 任意：入力があった時のフック

    float _lastActivityTime;
    bool _timedOut;

    void OnEnable()
    {
        if (startOnEnable) StartMonitoring();
    }

    void Update()
    {
        if (_timedOut) return;

        // --- 標準入力を「操作あり」として扱う（まずはこれでデバッグ可能） ---
        // マウスクリック/タップ開始
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.touchCount > 0)
            NotifyActivity();

        // キーボード入力（デバッグ用に便利）
        if (Input.anyKeyDown)
            NotifyActivity();

        // タイムアウト判定
        if (Time.unscaledTime - _lastActivityTime >= timeoutSeconds)
        {
            _timedOut = true;
            onTimeout?.Invoke();
        }
    }

    /// <summary>監視開始（タイマーリセット）</summary>
    public void StartMonitoring()
    {
        _timedOut = false;
        _lastActivityTime = Time.unscaledTime;
    }

    /// <summary>外部から「操作あり」を通知（UIイベント等から呼ぶ）</summary>
    public void NotifyActivity()
    {
        _lastActivityTime = Time.unscaledTime;
        onActivity?.Invoke();
    }

    /// <summary>タイムアウト後に再開したい場合</summary>
    public void Restart()
    {
        StartMonitoring();
    }

    /// <summary>停止したい場合（必要なら）</summary>
    public void Stop()
    {
        _timedOut = true;
    }
}
