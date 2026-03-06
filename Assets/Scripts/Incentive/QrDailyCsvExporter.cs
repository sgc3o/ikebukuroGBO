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

        // ----------------------------------------
        // 1) 機械向けの集計表
        // ----------------------------------------
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

        // 空行
        sb.AppendLine();

        // ----------------------------------------
        // 2) 人向けの日本語サマリー
        // ----------------------------------------
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