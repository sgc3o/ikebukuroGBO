using System.IO;
using UnityEngine;

[System.Serializable]
public class QrAlertGameSetting
{
    public int totalQrCount = 5000;
    public int alertThreshold = 500;
}

[System.Serializable]
public class QrAlertSettings
{
    public QrAlertGameSetting memory = new QrAlertGameSetting();
    public QrAlertGameSetting puzzle = new QrAlertGameSetting();
}

public static class QrAlertSettingsStore
{
    private const string FileName = "qr_alert_settings.json";

   public static string GetFilePath()
{
    string baseDir = Application.persistentDataPath;

var mgr = UnityEngine.Object.FindFirstObjectByType<QrUsageManager>();   
 if (mgr != null)
    {
        string resolved = mgr.GetResolvedBaseOutputDirectory();
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            baseDir = resolved;
        }
    }

    return Path.Combine(baseDir, "qr_alert_settings.json");
}

 public static QrAlertSettings Load()
{
    string path = GetFilePath();
    Debug.Log($"[QrAlertSettingsStore] Load path = {path}");

    if (!File.Exists(path))
    {
        Debug.LogWarning($"[QrAlertSettingsStore] File not found. Create default. path={path}");
        var defaultData = CreateDefault();
        Save(defaultData);
        return defaultData;
    }

    try
    {
        string json = File.ReadAllText(path);
        Debug.Log($"[QrAlertSettingsStore] Load json = {json}");

        if (string.IsNullOrWhiteSpace(json))
        {
            var emptyData = CreateDefault();
            Save(emptyData);
            return emptyData;
        }

        var data = JsonUtility.FromJson<QrAlertSettings>(json);
        if (data == null)
        {
            Debug.LogWarning("[QrAlertSettingsStore] FromJson returned null. Use default.");
            data = CreateDefault();
        }

        EnsureValid(data);
        return data;
    }
    catch (System.Exception e)
    {
        Debug.LogWarning($"[QrAlertSettingsStore] Load failed: {e.Message}");
        return CreateDefault();
    }
}

public static void Save(QrAlertSettings data)
{
    if (data == null) return;

    EnsureValid(data);

    string path = GetFilePath();
    Debug.Log($"[QrAlertSettingsStore] Save path = {path}");
    Debug.Log($"[QrAlertSettingsStore] Save json = {JsonUtility.ToJson(data, true)}");

    try
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }
    catch (System.Exception e)
    {
        Debug.LogWarning($"[QrAlertSettingsStore] Save failed: {e.Message}");
    }
}

    private static QrAlertSettings CreateDefault()
    {
        return new QrAlertSettings
        {
            memory = new QrAlertGameSetting
            {
                totalQrCount = 5000,
                alertThreshold = 500
            },
            puzzle = new QrAlertGameSetting
            {
                totalQrCount = 5000,
                alertThreshold = 500
            }
        };
    }

    private static void EnsureValid(QrAlertSettings data)
    {
        if (data.memory == null) data.memory = new QrAlertGameSetting();
        if (data.puzzle == null) data.puzzle = new QrAlertGameSetting();

        ClampGameSetting(data.memory);
        ClampGameSetting(data.puzzle);
    }

    private static void ClampGameSetting(QrAlertGameSetting setting)
    {
        if (setting == null) return;

        setting.totalQrCount = Mathf.Clamp(setting.totalQrCount, 1, 10000);
        setting.alertThreshold = Mathf.Clamp(setting.alertThreshold, 0, setting.totalQrCount);
    }
}