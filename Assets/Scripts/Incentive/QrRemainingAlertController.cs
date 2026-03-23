using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QrRemainingAlertController : MonoBehaviour
{
    [Header("Game Key")]
    [SerializeField] private string gameKey = "Memory"; // "Memory" or "Puzzle"

    [Header("UI")]
    [SerializeField] private TMP_InputField totalQrCountInput;
    [SerializeField] private TMP_InputField alertThresholdInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private TMP_Text remainingCountText;
    [SerializeField] private TMP_Text alertStatusText;
    [SerializeField] private TMP_Text saveResultText;

    [Header("Refresh")]
    [SerializeField] private float refreshInterval = 1.0f;

    private float timer;
    private QrAlertSettings settings;

    private void Awake()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnClickSave);
        }
    }

    private void Start()
    {
        settings = QrAlertSettingsStore.Load();
        ApplySettingsToInputs();
        RefreshNow();
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;

        timer = Mathf.Max(0.2f, refreshInterval);
        RefreshNow();
    }

    private void RefreshNow()
{
    settings = QrAlertSettingsStore.Load();

    var gameSetting = GetCurrentSetting();
    if (gameSetting == null) return;

    int nextIndex = GetNextIndexFromManager();
    int usedCount = Mathf.Max(0, nextIndex - 1);
    int remainingCount = Mathf.Max(0, gameSetting.totalQrCount - usedCount);

    if (remainingCountText != null)
    {
        remainingCountText.text = $"残り枚数: {remainingCount}";
    }

    if (alertStatusText != null)
    {
        if (remainingCount <= 0)
        {
            alertStatusText.text = "状態: QR切れ";
            alertStatusText.color = Color.red;
        }
        else if (remainingCount <= gameSetting.alertThreshold)
        {
            alertStatusText.text = $"状態: アラート（残り{gameSetting.alertThreshold}枚以下）";
            alertStatusText.color = Color.yellow;
        }
        else
        {
            alertStatusText.text = "状態: 正常";
            alertStatusText.color = Color.white;
        }
    }
}

    private void ApplySettingsToInputs()
    {
        var gameSetting = GetCurrentSetting();
        if (gameSetting == null) return;

        if (totalQrCountInput != null)
        {
            totalQrCountInput.text = gameSetting.totalQrCount.ToString();
        }

        if (alertThresholdInput != null)
        {
            alertThresholdInput.text = gameSetting.alertThreshold.ToString();
        }
    }

  private void OnClickSave()
{
    // 毎回、保存直前に最新ファイルを読み直す
    settings = QrAlertSettingsStore.Load();

    var gameSetting = GetCurrentSetting();
    if (gameSetting == null) return;

    int currentNextIndex = GetNextIndexFromManager();
    int usedCount = Mathf.Max(0, currentNextIndex - 1);

    int total = ParseOrDefault(totalQrCountInput, gameSetting.totalQrCount);
    int threshold = ParseOrDefault(alertThresholdInput, gameSetting.alertThreshold);

    total = Mathf.Clamp(total, 1, 10000);

    if (total < usedCount)
    {
        total = usedCount;
    }

    threshold = Mathf.Clamp(threshold, 0, total);

    gameSetting.totalQrCount = total;
    gameSetting.alertThreshold = threshold;

    QrAlertSettingsStore.Save(settings);

    // 保存後も最新ファイルで揃え直す
    settings = QrAlertSettingsStore.Load();

    ApplySettingsToInputs();
    RefreshNow();

    if (saveResultText != null)
    {
        saveResultText.text = $"保存しました（総数:{total} / 閾値:{threshold}）";
    }

    Debug.Log($"[QrRemainingAlertController] Saved. gameKey={gameKey}, total={total}, threshold={threshold}");
}

    private QrAlertGameSetting GetCurrentSetting()
    {
        if (settings == null) return null;

        if (string.Equals(gameKey, "Memory", System.StringComparison.OrdinalIgnoreCase))
        {
            return settings.memory;
        }

        if (string.Equals(gameKey, "Puzzle", System.StringComparison.OrdinalIgnoreCase))
        {
            return settings.puzzle;
        }

        Debug.LogWarning($"[QrRemainingAlertController] Unsupported gameKey: {gameKey}");
        return null;
    }

    private int GetNextIndexFromManager()
    {
        var mgr = QrUsageManager.Instance;
        if (mgr == null) return 1;

        var gameState = mgr.GetGameStateCopy(gameKey);
        if (gameState == null) return 1;

        return Mathf.Max(1, gameState.nextIndex);
    }

    private int ParseOrDefault(TMP_InputField input, int defaultValue)
    {
        if (input == null) return defaultValue;

        if (int.TryParse(input.text, out int value))
        {
            return value;
        }

        return defaultValue;
    }
}