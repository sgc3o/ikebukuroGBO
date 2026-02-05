using UnityEngine;

public class UIButtonRelay : MonoBehaviour
{
     public static UIButtonRelay I { get; private set; }
     
    [Header("Scene Navigation")]
    public SceneId targetScene;

    // 汎用：Scene遷移
    public void GoToScene()
    {
        SceneNavigator.Go(targetScene);
    }

    // 明示的に呼びたい場合
    public void GoToHub()
    {
        SceneNavigator.Go(SceneId.S_2_Hub);
    }

    public void GoToPV03()
    {
        SceneNavigator.Go(SceneId.S_MemoryGameRoot);
    }

    public void GoToPV04()
    {
        SceneNavigator.Go(SceneId.S_MemoryGameRoot);
    }

    public void GoToPV05()
    {
        SceneNavigator.Go(SceneId.S_Insentive);
    }

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
