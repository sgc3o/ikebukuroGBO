using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    public string SelectedGameKey { get; private set; }
    public string NextOpeningScene { get; private set; }
    public string NextGameScene { get; private set; }

    public int CurrentStage { get; private set; } = 1;
    public int TotalStages { get; private set; } = 2;

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

    // ゲーム選択時に呼ぶ（PV02側など）
    public void SelectGame(string gameKey, string openingScene, string gameScene, int totalStages = 2)
    {
        SelectedGameKey = gameKey;
        NextOpeningScene = openingScene;
        NextGameScene = gameScene;

        TotalStages = Mathf.Max(1, totalStages);
        CurrentStage = 1; // 選び直したらステージは1に戻す
    }

    // 進められたら true（= 次ステージへ）
    public bool TryAdvanceStage()
    {
        if (CurrentStage < TotalStages)
        {
            CurrentStage++;
            return true;
        }
        return false;
    }

    // 最終クリア後など、必要なら明示リセット
    public void ResetStages()
    {
        CurrentStage = 1;
    }
}
