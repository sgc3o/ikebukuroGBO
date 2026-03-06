using System.IO;
using System.Text;
using UnityEngine;

public static class QrDailyCsvExporter
{
    public static string ExportDailySummary(
        QrUsageState state,
        string targetDate,
        string outputDirectory)
    {
        if (state == null)
        {
            Debug.LogWarning("[QrDailyCsvExporter] state is null.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(targetDate))
        {
            Debug.LogWarning("[QrDailyCsvExporter] targetDate is empty.");
            return null;
        }

        string dir = ResolveOutputDirectory(outputDirectory);
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, $"qr_daily_{targetDate}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("Date,GameKey,TodayCount,StartIndex,EndIndex,NextIndex,LastDayMaxCount");

        if (state.games != null)
        {
            foreach (var game in state.games)
            {
                if (game == null) continue;

                sb.Append(targetDate).Append(",");
                sb.Append(Escape(game.gameKey)).Append(",");
                sb.Append(game.todayCount).Append(",");
                sb.Append(game.todayStartIndex).Append(",");
                sb.Append(game.todayEndIndex).Append(",");
                sb.Append(game.nextIndex).Append(",");
                sb.Append(game.lastDayMaxCount);
                sb.AppendLine();
            }
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[QrDailyCsvExporter] Exported: {filePath}");
        return filePath;
    }

    private static string ResolveOutputDirectory(string outputDirectory)
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

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}