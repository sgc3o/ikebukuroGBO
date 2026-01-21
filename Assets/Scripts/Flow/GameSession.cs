using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; } //全ての情報を吸い上げるところ

    public string SelectedGameKey { get; private set;} //ゲーム選択画面のインスタンスを吸い上げる
    public string NextOpeningScene { get; private set; } //PV02
    public string NextGameScene { get; private set; } //PV03

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectGame(string gameKey, string openingScene, string gameScene)
    {
        SelectedGameKey = gameKey;
        NextOpeningScene = openingScene;
        NextGameScene = gameScene;
    }


}
