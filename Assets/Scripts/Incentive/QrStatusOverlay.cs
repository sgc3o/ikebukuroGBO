using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QrStatusOverlay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private bool showInDevelopmentBuildOnly = false;
    [SerializeField] private float refreshInterval = 1.0f;

    [Header("Game Keys")]
    [SerializeField] private string memoryGameKey = "Memory";
    [SerializeField] private string puzzleGameKey = "Puzzle";

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button exportNowButton;
    [SerializeField] private TMP_Text exportNowButtonLabel;
    [SerializeField] private TMP_Text lastActionText;

    [Header("Developer Tools")]
    [SerializeField] private bool showDeveloperControls = true;
    [SerializeField] private string developerButtonLabel = "【開発確認用】CSVを今すぐ出力";

    [Header("Reset Controls")]
    [SerializeField] private Button resetMemoryButton;
    [SerializeField] private Button resetPuzzleButton;
    [SerializeField] private QrResetConfirmPopup resetPopup;

    private float timer;

    private void Awake()
    {
        bool visible = showOverlay;

        if (showInDevelopmentBuildOnly && !Debug.isDebugBuild)
        {
            visible = false;
        }

        if (root != null)
        {
            root.SetActive(visible);
        }

        if (exportNowButton != null)
        {
            exportNowButton.gameObject.SetActive(visible && showDeveloperControls);
            exportNowButton.onClick.RemoveAllListeners();
            exportNowButton.onClick.AddListener(OnClickExportNow);
        }

        if (exportNowButtonLabel != null)
        {
            exportNowButtonLabel.text = developerButtonLabel;
        }

        if (resetMemoryButton != null)
        {
            resetMemoryButton.onClick.AddListener(OnClickResetMemory);
        }

        if (resetPuzzleButton != null)
        {
            resetPuzzleButton.onClick.AddListener(OnClickResetPuzzle);
        }

        RefreshView();
    }

    private void Update()
    {
        if (root != null && !root.activeSelf) return;

        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;

        timer = Mathf.Max(0.2f, refreshInterval);
        RefreshView();
    }

    private void RefreshView()
    {
        if (statusText == null) return;

        var mgr = QrUsageManager.Instance;
        if (mgr == null)
        {
            statusText.text = "QrUsageManager が見つかりません。";
            return;
        }

        var memory = mgr.GetGameStateCopy(memoryGameKey);
        var puzzle = mgr.GetGameStateCopy(puzzleGameKey);

        string yesterdaySummary = ReadYesterdayCsvSummary(mgr);

        var sb = new StringBuilder();

        sb.AppendLine("【QR運用情報】");
        sb.AppendLine();
        sb.AppendLine($"今日: {mgr.GetTodayDate()}");
        sb.AppendLine();

        AppendGameBlock(sb, "ガシャポンメモリーゲーム", memory);
        sb.AppendLine();
        AppendGameBlock(sb, "たまごっちのガシャポンかくれんぼ", puzzle);
        sb.AppendLine();

        sb.AppendLine("【前日CSV要約】");
        sb.AppendLine(string.IsNullOrWhiteSpace(yesterdaySummary) ? "前日CSVなし" : yesterdaySummary);
        sb.AppendLine();

        sb.AppendLine("【保存先】");
        sb.AppendLine($"JSON: {mgr.GetJsonFilePath()}");
        sb.AppendLine($"CSV : {mgr.GetCsvOutputDirectory()}");

        sb.AppendLine("【本日のリセット履歴】");
        sb.AppendLine(mgr.GetTodayResetSummary());
        sb.AppendLine();

        statusText.text = sb.ToString();
    }

    private void AppendGameBlock(StringBuilder sb, string title, QrGameUsageState game)
    {
        sb.AppendLine($"【{title}】");

        if (game == null)
        {
            sb.AppendLine("情報なし");
            return;
        }

        sb.AppendLine($"今日の表示回数: {game.todayCount}");
        sb.AppendLine($"次回QR番号: {game.nextIndex}");
        sb.AppendLine($"今日最後に表示した番号: {game.todayEndIndex}");
    }

    private string ReadYesterdayCsvSummary(QrUsageManager mgr)
    {
        try
        {
            string dir = mgr.GetCsvOutputDirectory();
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            string path = Path.Combine(dir, $"qr_daily_{yesterday}.csv");

            if (!File.Exists(path))
            {
                return "";
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines == null || lines.Length <= 1)
            {
                return "";
            }

            var sb = new StringBuilder();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("Date,GameKey")) continue;
                if (line.StartsWith("-")) break;
                if (line.StartsWith("集計日")) break;
                if (!line.Contains(",")) continue;

                string[] cols = line.Split(',');
                if (cols.Length < 6) continue;

                string gameKey = cols[1].Trim();
                string todayCount = cols[2].Trim();
                string endIndex = cols[4].Trim();
                string nextIndex = cols[5].Trim();

                string jpName = ToJapaneseGameName(gameKey);
                sb.AppendLine($"{jpName}: 回数={todayCount} / 最終={endIndex} / 次回={nextIndex}");
            }

            return sb.ToString().Trim();
        }
        catch (Exception e)
        {
            return $"前日CSV読込失敗: {e.Message}";
        }
    }

    private void OnClickExportNow()
    {
        var mgr = QrUsageManager.Instance;
        if (mgr == null)
        {
            SetLastAction("QrUsageManager が見つかりません。");
            return;
        }

        string path = mgr.ExportTodayCsvNow();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetLastAction("CSV出力に失敗しました。");
        }
        else
        {
            SetLastAction($"CSV出力完了: {path}");
        }

        RefreshView();
    }

    private void SetLastAction(string message)
    {
        if (lastActionText != null)
        {
            lastActionText.text = message;
        }

        Debug.Log("[QrStatusOverlay] " + message);
    }

    private string ToJapaneseGameName(string gameKey)
    {
        if (string.IsNullOrWhiteSpace(gameKey)) return "不明";

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

    private void OnClickResetMemory()
    {
        if (resetPopup == null) return;

        resetPopup.Show("ガシャポンメモリーゲーム", () =>
        {
            var ok = QrUsageManager.Instance.ResetGame(memoryGameKey);
            SetLastAction(ok
                ? "メモリーゲームのQR使用状況をリセットしました。"
                : "メモリーゲームのリセットに失敗しました。");
            RefreshView();
        });
    }

    private void OnClickResetPuzzle()
    {
        if (resetPopup == null) return;

        resetPopup.Show("たまごっちのガシャポンかくれんぼ", () =>
        {
            var ok = QrUsageManager.Instance.ResetGame(puzzleGameKey);
            SetLastAction(ok
                ? "たまごっちのガシャポンかくれんぼ のQR使用状況をリセットしました。"
                : "たまごっちのガシャポンかくれんぼ のリセットに失敗しました。");
            RefreshView();
        });
    }

}