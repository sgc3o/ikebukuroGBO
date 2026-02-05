using UnityEngine;

/// <summary>
/// 「もどる」ボタン用：TitlePanel を表示し、他パネルを隠す。
/// 既存のGameManagerに依存しない最小実装。
/// </summary>
public class BackToTitle : MonoBehaviour
{
    [Header("Panels (set in Inspector)")]
    public GameObject titlePanel;
    public GameObject gameSelectPanel;
    public GameObject confirmPanel;
    public GameObject panelTimeout;   // あれば
    public GameObject panelTimer;     // あれば（Timerコンポーネントを持つ親など）

    [Header("Optional")]
    public Timer timer;              // Timerを直接差す（無ければ panelTimer から自動取得も可）

    /// <summary>
    /// ボタンから呼ぶ
    /// </summary>
    public void GoTitle()
    {
        // まずタイムアウトUIが出てるなら閉じる（安全策）
        if (panelTimeout) panelTimeout.SetActive(false);

        // Panel表示切替
        if (titlePanel) titlePanel.SetActive(true);
        if (gameSelectPanel) gameSelectPanel.SetActive(false);
        if (!confirmPanel)
        {
        }
        else
            confirmPanel.SetActive(false);

        // タイマーをリセット（任意：戻る＝操作扱い）
        var t = timer;
        if (t == null && panelTimer != null) t = panelTimer.GetComponent<Timer>();
        if (t != null) t.Restart();
        
    }
}
