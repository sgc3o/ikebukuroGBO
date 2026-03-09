using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QrUsageManager : MonoBehaviour
{
    [Serializable]
    public class GameQrSetting
    {
        public string gameKey;
        public string resourcesFolder;
        public int maxQrCount = 10000;
    }

    private static QrUsageManager instance;
    public static QrUsageManager Instance
    {
        get
        {
            if (instance == null)
            {
                var found = FindObjectOfType<QrUsageManager>();
                if (found != null)
                {
                    instance = found;
                }
                else
                {
                    var go = new GameObject("QrUsageManager");
                    instance = go.AddComponent<QrUsageManager>();
                }
            }

            return instance;
        }
    }

    [Header("Game QR Settings")]
    [SerializeField]
    private List<GameQrSetting> gameSettings = new List<GameQrSetting>()
    {
        new GameQrSetting { gameKey = "Memory", resourcesFolder = "MemoryIncentive/qr", maxQrCount = 10000 },
        new GameQrSetting { gameKey = "Puzzle", resourcesFolder = "PuzzleIncentive/qr", maxQrCount = 10000 },
    };

    [Header("Daily CSV Export")]
    [SerializeField] private int exportHour = 21;
    [SerializeField] private int exportMinute = 10;
    [SerializeField] private string outputDirectory = "QRLogs";

    [Header("Update Check")]
    [SerializeField] private float clockCheckIntervalSeconds = 5f;

    private QrUsageState state;
    private float clockCheckTimer;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();
        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();
        SaveState();
    }

    private void Update()
    {
        clockCheckTimer -= Time.unscaledDeltaTime;
        if (clockCheckTimer > 0f) return;

        clockCheckTimer = Mathf.Max(1f, clockCheckIntervalSeconds);

        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();
        TryAutoExportToday();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveState();
        }
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    public Sprite GetNextQrSprite(string gameKey)
    {
        if (string.IsNullOrWhiteSpace(gameKey))
        {
            Debug.LogWarning("[QrUsageManager] gameKey is empty.");
            return null;
        }

        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();

        var setting = FindSetting(gameKey);
        if (setting == null)
        {
            Debug.LogWarning($"[QrUsageManager] game setting not found. gameKey={gameKey}");
            return null;
        }

        var gameState = GetOrCreateGameState(gameKey);

        int currentIndex = Mathf.Max(1, gameState.nextIndex);

        if (currentIndex > setting.maxQrCount)
        {
            Debug.LogWarning($"[QrUsageManager] QR exhausted. gameKey={gameKey}, nextIndex={currentIndex}");
            return null;
        }

        string resourcePath = $"{setting.resourcesFolder}/qr_{currentIndex:D5}";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);

        if (sprite == null)
        {
            Debug.LogWarning($"[QrUsageManager] Sprite not found: {resourcePath}");
            return null;
        }

        if (gameState.todayCount <= 0)
        {
            gameState.todayStartIndex = currentIndex;
        }

        gameState.todayCount++;
        gameState.todayEndIndex = currentIndex;
        gameState.nextIndex = currentIndex + 1;

        SaveState();
        return sprite;
    }

    private void LoadState()
    {
        state = QrUsagePersistence.Load();
        if (state == null)
        {
            state = new QrUsageState();
        }
    }

    private void SaveState()
    {
        QrUsagePersistence.Save(state);
    }

    private void EnsureTodayInitialized()
    {
        if (state == null)
        {
            state = new QrUsageState();
        }

        if (string.IsNullOrWhiteSpace(state.todayDate))
        {
            state.todayDate = GetTodayString();
        }

        if (state.games == null)
        {
            state.games = new List<QrGameUsageState>();
        }

        if (state.resetRecords == null)
        {
            state.resetRecords = new List<QrResetRecord>();
        }

    }

    private void TryFinalizePreviousDayIfNeeded()
    {
        string today = GetTodayString();

        if (string.IsNullOrWhiteSpace(state.todayDate))
        {
            state.todayDate = today;
            return;
        }

        if (state.todayDate == today)
        {
            return;
        }

        string previousDate = state.todayDate;

        if (HasAnyDailyCount())
        {
            QrDailyCsvExporter.ExportDailySummary(state, previousDate, outputDirectory);
        }

        foreach (var game in state.games)
        {
            if (game == null) continue;

            game.lastDayMaxCount = Mathf.Max(game.lastDayMaxCount, game.todayCount);
            game.todayCount = 0;
            game.todayStartIndex = 0;
            game.todayEndIndex = 0;
        }

        state.todayDate = today;
        state.lastAutoExportDate = "";
        SaveState();
    }

    private void TryAutoExportToday()
    {
        DateTime now = DateTime.Now;
        string today = GetTodayString();

        if (state.lastAutoExportDate == today)
        {
            return;
        }

        DateTime exportTime = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            Mathf.Clamp(exportHour, 0, 23),
            Mathf.Clamp(exportMinute, 0, 59),
            0);

        if (now < exportTime)
        {
            return;
        }

        QrDailyCsvExporter.ExportDailySummary(state, today, outputDirectory);
        state.lastAutoExportDate = today;
        SaveState();
    }

    private bool HasAnyDailyCount()
    {
        if (state == null || state.games == null) return false;

        foreach (var game in state.games)
        {
            if (game != null && game.todayCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private QrGameUsageState GetOrCreateGameState(string gameKey)
    {
        foreach (var game in state.games)
        {
            if (game != null && string.Equals(game.gameKey, gameKey, StringComparison.OrdinalIgnoreCase))
            {
                if (game.nextIndex < 1) game.nextIndex = 1;
                return game;
            }
        }

        var created = new QrGameUsageState
        {
            gameKey = gameKey,
            nextIndex = 1,
            todayCount = 0,
            todayStartIndex = 0,
            todayEndIndex = 0,
            lastDayMaxCount = 0
        };

        state.games.Add(created);
        return created;
    }

    private GameQrSetting FindSetting(string gameKey)
    {
        foreach (var setting in gameSettings)
        {
            if (setting == null) continue;

            if (string.Equals(setting.gameKey, gameKey, StringComparison.OrdinalIgnoreCase))
            {
                return setting;
            }
        }

        return null;
    }

    private string GetTodayString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    public QrGameUsageState GetGameStateCopy(string gameKey)
    {
        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();

        var game = GetOrCreateGameState(gameKey);
        if (game == null) return null;

        return new QrGameUsageState
        {
            gameKey = game.gameKey,
            nextIndex = game.nextIndex,
            todayCount = game.todayCount,
            todayStartIndex = game.todayStartIndex,
            todayEndIndex = game.todayEndIndex,
            lastDayMaxCount = game.lastDayMaxCount
        };
    }

    public string GetTodayDate()
    {
        EnsureTodayInitialized();
        return state != null ? state.todayDate : "";
    }

    public string GetJsonFilePath()
    {
        return QrUsagePersistence.GetFilePath();
    }

    public string GetCsvOutputDirectory()
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return Path.Combine(Application.persistentDataPath, "QRLogs");
        }

        if (Path.IsPathRooted(outputDirectory))
        {
            return outputDirectory;
        }

        return Path.Combine(Application.persistentDataPath, outputDirectory);
    }

    public string ExportTodayCsvNow()
    {
        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();

        string today = GetTodayString();
        string timeStamp = System.DateTime.Now.ToString("HHmmss");
        string fileName = $"qr_daily_{today}_BtnOut_{timeStamp}.csv";

        string path = QrDailyCsvExporter.ExportDailySummaryWithFileName(
            state,
            today,
            outputDirectory,
            fileName);

        SaveState();
        return path;
    }

    public bool ResetGame(string gameKey)
    {
        if (string.IsNullOrWhiteSpace(gameKey))
        {
            Debug.LogWarning("[QrUsageManager] ResetGame failed. gameKey is empty.");
            return false;
        }

        EnsureTodayInitialized();
        TryFinalizePreviousDayIfNeeded();

        var game = GetOrCreateGameState(gameKey);
        if (game == null)
        {
            Debug.LogWarning($"[QrUsageManager] ResetGame failed. state not found: {gameKey}");
            return false;
        }

        EnsureResetRecordsInitialized();

        var record = new QrResetRecord
        {
            date = GetTodayString(),
            dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameKey = game.gameKey,
            reason = "manual",
            previousNextIndex = game.nextIndex,
            previousTodayCount = game.todayCount,
            previousTodayStartIndex = game.todayStartIndex,
            previousTodayEndIndex = game.todayEndIndex
        };

        state.resetRecords.Add(record);

        game.nextIndex = 1;
        game.todayCount = 0;
        game.todayStartIndex = 0;
        game.todayEndIndex = 0;

        SaveState();

        Debug.Log($"[QrUsageManager] ResetGame success: {gameKey}");
        return true;
    }

    private void EnsureResetRecordsInitialized()
    {
        if (state == null)
        {
            state = new QrUsageState();
        }

        if (state.resetRecords == null)
        {
            state.resetRecords = new System.Collections.Generic.List<QrResetRecord>();
        }
    }

    public string GetTodayResetSummary()
    {
        EnsureTodayInitialized();

        if (state == null || state.resetRecords == null || state.resetRecords.Count == 0)
        {
            return "‚È‚µ";
        }

        string today = GetTodayString();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool found = false;

        for (int i = 0; i < state.resetRecords.Count; i++)
        {
            var record = state.resetRecords[i];
            if (record == null) continue;
            if (record.date != today) continue;

            found = true;
            sb.AppendLine($"{record.dateTime} / {record.gameKey}");
        }

        return found ? sb.ToString().Trim() : "‚È‚µ";
    }
}