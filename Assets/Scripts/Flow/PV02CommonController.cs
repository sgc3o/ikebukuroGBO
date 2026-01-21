using UnityEngine;
using UnityEngine.SceneManagement;

public class PV02CommonController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject openingPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Next Scene")]
    [SerializeField] private string nextGameSceneName = "PV03_Virusweets_Game";

    private void Start()
    {
        // 起動時はOpeningを表示
        ShowOpening();
    }

    // OpeningのStartボタン用
    public void OnStartPressed()
    {
        ShowHowToPlay();
    }

    // HowToPlayのOKボタン用
    public void OnOkPressed()
    {
        SceneManager.LoadScene(nextGameSceneName);
    }

    // もし「戻る」ボタンを置くなら使える
    public void OnBackToOpening()
    {
        ShowOpening();
    }

    private void ShowOpening()
    {
        if (openingPanel != null) openingPanel.SetActive(true);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    private void ShowHowToPlay()
    {
        if (openingPanel != null) openingPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }
}
