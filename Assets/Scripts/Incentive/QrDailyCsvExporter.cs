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
        string fileName = $"qr_daily_{targetDate}.csv";
        return ExportDailySummaryWithFileName(state, targetDate, outputDirectory, fileName);
    }

    public static string ExportDailySummaryWithFileName(
        QrUsageState state,
        string targetDate,
        string outputDirectory,
        string fileName)
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

        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("[QrDailyCsvExporter] fileName is empty.");
            return null;
        }

        string dir = ResolveOutputDirectory(outputDirectory);
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, fileName);

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

        sb.AppendLine();
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine($"集計日：{targetDate}");
        sb.AppendLine();

        if (state.games != null)
        {
            foreach (var game in state.games)
            {
                if (game == null) continue;

                sb.AppendLine($"【{ToJapaneseGameName(game.gameKey)}】");
                sb.AppendLine($"当日のQR表示回数：{game.todayCount}");
                sb.AppendLine($"当日最初に表示したQR番号：{game.todayStartIndex}");
                sb.AppendLine($"当日最後に表示したQR番号：{game.todayEndIndex}");
                sb.AppendLine($"次回表示予定のQR番号：{game.nextIndex}");
                sb.AppendLine($"過去1日あたり最大表示回数：{game.lastDayMaxCount}");
                sb.AppendLine();
            }
        }

        AppendResetSection(sb, state, targetDate);

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[QrDailyCsvExporter] Exported: {filePath}");
        return filePath;
    }

    private static void AppendResetSection(StringBuilder sb, QrUsageState state, string targetDate)
    {
        if (state == null || state.resetRecords == null) return;

        bool hasReset = false;
        for (int i = 0; i < state.resetRecords.Count; i++)
        {
            var record = state.resetRecords[i];
            if (record != null && record.date == targetDate)
            {
                hasReset = true;
                break;
            }
        }

        if (!hasReset) return;

        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine("リセット記録");
        sb.AppendLine();

        for (int i = 0; i < state.resetRecords.Count; i++)
        {
            var record = state.resetRecords[i];
            if (record == null) continue;
            if (record.date != targetDate) continue;

            sb.AppendLine($"{record.dateTime} に {ToJapaneseGameName(record.gameKey)} を手動リセット");
            sb.AppendLine($"リセット前 NextIndex: {record.previousNextIndex}");
            sb.AppendLine($"リセット前 TodayCount: {record.previousTodayCount}");
            sb.AppendLine($"リセット前 StartIndex: {record.previousTodayStartIndex}");
            sb.AppendLine($"リセット前 EndIndex: {record.previousTodayEndIndex}");
            sb.AppendLine();
        }
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

    private static string ToJapaneseGameName(string gameKey)
    {
        if (string.IsNullOrWhiteSpace(gameKey))
        {
            return "不明";
        }

        switch (gameKey.Trim().ToLowerInvariant())
        {
            case "memory":
                return "ガシャポンメモリーゲーム";
            case "puzzle":
                return "たまごっちのガシャポンかくれんぼ";
            default:
                return gameKey;
        }
    }
}