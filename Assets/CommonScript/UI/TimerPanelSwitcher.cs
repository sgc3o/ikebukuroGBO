using UnityEngine;

/// 90秒触られなかったら→Panel_Timeout を表示するためのスイッチャー
/// 
public class TimerResetSwitcher : MonoBehaviour
{
    public GameObject panel_Timer;
    public GameObject panel_Timeout;

    void Awake()
    {
        // 起動時の初期状態を確実にする
        ShowTimer();
    }

    public void ShowTimeout()
    {
        // panel_Timer は消さない（Timerを止めない）
        // if (panel_Timer) panel_Timer.SetActive(false);

        if (panel_Timeout) panel_Timeout.SetActive(true);
    }

    public void ShowTimer()
    {
        if (panel_Timeout) panel_Timeout.SetActive(false);
    }
}
