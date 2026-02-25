using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToHubButton : MonoBehaviour
{
    public const string HubBootKey = "HubBootTarget";

    [Header("Scene")]
    [SerializeField] private string hubSceneName = "S_2_Hub";

    [Header("Hub Target")]
    [SerializeField] private string hubTargetId = "GameSelectPanel";

    [Header("Option")]
    [SerializeField] private bool useSceneTransitionIfAny = true;

    [Header("Auto Bind")]
    [SerializeField] private Button button; // 未設定なら自分から探す

    void Reset()
    {
        button = GetComponent<Button>();
    }

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(Return);
            button.onClick.AddListener(Return);
        }
    }

    public void Return()
    {
        PlayerPrefs.SetString(HubBootKey, hubTargetId);
        PlayerPrefs.Save();

        if (useSceneTransitionIfAny && HasSceneTransition())
            SceneTransition.Go(hubSceneName);
        else
            SceneManager.LoadScene(hubSceneName);
    }

    bool HasSceneTransition()
    {
        // SceneTransitionがプロジェクトに存在する前提
        return true;
    }
}