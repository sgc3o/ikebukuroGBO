using System.IO;
using UnityEngine;

public static class QrUsagePersistence
{
    private const string FileName = "qr_usage_state.json";

    public static string GetFilePath(string baseDirectory = null)
    {
        string root = string.IsNullOrWhiteSpace(baseDirectory)
            ? Application.persistentDataPath
            : baseDirectory;

        return Path.Combine(root, FileName);
    }

    public static QrUsageState Load(string baseDirectory = null)
    {
        string path = GetFilePath(baseDirectory);

        if (!File.Exists(path))
        {
            return new QrUsageState();
        }

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new QrUsageState();
            }

            var state = JsonUtility.FromJson<QrUsageState>(json);
            return state ?? new QrUsageState();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QrUsagePersistence] Load failed: {e.Message}");
            return new QrUsageState();
        }
    }

    public static void Save(QrUsageState state, string baseDirectory = null)
    {
        if (state == null) return;

        string path = GetFilePath(baseDirectory);

        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QrUsagePersistence] Save failed: {e.Message}");
        }
    }
}