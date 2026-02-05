using UnityEngine;

public class HubEntryPoint : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject titlePanel;
    public GameObject gameSelectPanel;
    public GameObject confirmPanel;

    void Start()
    {
        if (!ReturnToHub.ForceTitleOnEnter) return;

        ReturnToHub.ForceTitleOnEnter = false; // 使い切り

        // Title表示、他は閉じる（必要な分だけ）
        if (titlePanel) titlePanel.SetActive(true);
        if (gameSelectPanel) gameSelectPanel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);
    }
}
