using UnityEngine;

public class BackToHubTitle : MonoBehaviour
{
    [Header("Hub Scene Name (should come from catalog ideally)")]
    public string hubSceneName = "S_2_Hub"; // SceneCatalog運用なら後で差し替え可

    public void GoHubTitle()
    {
        ReturnToHub.ForceTitleOnEnter = true;

        // フェード付きScene切替（禁止事項回避）
        TransitionController.I.StartTransitionToScene(hubSceneName);
    }
}
