using UnityEngine;

public class HubBootRouter : MonoBehaviour
{
    [Header("Targets (optional)")]
    [Tooltip("GameSelectPanelのGameObjectを直で指定できるなら入れる。無ければFindで探す。")]
    [SerializeField] private GameObject gameSelectPanel;

    void Start()
    {
        string key = ReturnToHubButton.HubBootKey;
        if (!PlayerPrefs.HasKey(key)) return;

        string target = PlayerPrefs.GetString(key, "");
        PlayerPrefs.DeleteKey(key);

        if (string.IsNullOrEmpty(target)) return;

        // 今回は GameSelectPanel を想定
        if (target == "GameSelectPanel")
        {
            OpenGameSelectPanel();
        }
    }

    void OpenGameSelectPanel()
    {
        if (gameSelectPanel == null)
        {
            // 名前が一致している前提で探す（必要なら階層パスに拡張可能）
            var found = GameObject.Find("GameSelectPanel");
            if (found != null) gameSelectPanel = found;
        }

        if (gameSelectPanel != null)
        {
            gameSelectPanel.SetActive(true);
        }

        // もしHub側に「今開いているタブを切り替える管理スクリプト」があるなら、
        // ここでそれを呼ぶのが理想（例: HubUIController.OpenGameSelect()）
    }
}