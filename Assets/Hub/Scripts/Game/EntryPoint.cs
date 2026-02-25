using System.Collections;
using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    public const string HubBootKey = "HubBootTarget";

    [Header("Assign in Inspector")]
    public GameObject titlePanel;
    public GameObject gameSelectPanel;
    public GameObject confirmPanel;

    [Header("IDs")]
    public string gameSelectTargetId = "GameSelectPanel"; // ← Return側と完全一致させる

    private IEnumerator Start()
    {
        // 他の初期化（Panel_Switcher等）が走るのを待つ
        yield return null;

        // Returnボタンからの指定があれば最優先
        if (PlayerPrefs.HasKey(HubBootKey))
        {
            var target = PlayerPrefs.GetString(HubBootKey, "");
            PlayerPrefs.DeleteKey(HubBootKey);

            if (target == gameSelectTargetId)
            {
                SetPanels(showTitle: false, showGameSelect: true, showConfirm: false);
                yield break;
            }
        }

        // 既存の挙動（タイトル表示に戻す等）があるならここに続ける
        // 例: SetPanels(true,false,false);
    }

    private void SetPanels(bool showTitle, bool showGameSelect, bool showConfirm)
    {
        if (titlePanel) titlePanel.SetActive(showTitle);
        if (gameSelectPanel) gameSelectPanel.SetActive(showGameSelect);
        if (confirmPanel) confirmPanel.SetActive(showConfirm);
    }
}

/*
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
*/