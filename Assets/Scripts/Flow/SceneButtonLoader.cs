using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    [Header("遷移先シーン名（Build Settingsに入っている必要あり）")]
    [SerializeField] private string sceneName;

    // ButtonのOnClick()から呼ぶ
    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[SceneButtonLoader] sceneName が空です: {name}");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
