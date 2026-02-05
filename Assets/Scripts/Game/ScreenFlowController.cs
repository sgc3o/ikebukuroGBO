using UnityEngine;

public class ScreenFlowController : MonoBehaviour
{
    public static ScreenFlowController I { get; private set; }

    [Header("Roots in the SAME scene")]
    [SerializeField] private GameObject introRoot; // IntroRoot
    [SerializeField] private GameObject gameRoot;  // GameRoot

    public enum StartScreen { Intro, Game }
    [SerializeField] private StartScreen startScreen = StartScreen.Intro;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        ApplyStartState();
    }

    private void ApplyStartState()
    {
        if (startScreen == StartScreen.Game) ShowGame();
        else ShowIntro();
    }

    public void ShowIntro()
    {
        if (introRoot != null) introRoot.SetActive(true);
        if (gameRoot != null) gameRoot.SetActive(false);
    }

    public void ShowGame()
    {
        if (introRoot != null) introRoot.SetActive(false);
        if (gameRoot != null) gameRoot.SetActive(true);
    }
}
