using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resources配下のQRスプライトを自動収集し、
/// シャッフル順（袋方式）で「被りなし」で順番に表示する。
/// - 全消化するまで同じQRは出ない
/// - 全消化したら再シャッフルして次周へ
/// - PlayerPrefsで順番と位置を保存できる（端末内で継続）
/// </summary>
[DisallowMultipleComponent]
public class RandomQrImageBag : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Source (Resources)")]
    [Tooltip("Resources配下の相対パス。例: PuzzleIncentive/qr")]
    [SerializeField] private string resourcesPath = "PuzzleIncentive/qr";

    [Tooltip("対象ファイル名の接頭辞。例: qr_00001 -> \"qr_\"")]
    [SerializeField] private string namePrefix = "qr_";

    [Header("Rule")]
    [Tooltip("OnEnableで自動抽選する")]
    [SerializeField] private bool chooseOnEnable = true;

    [Tooltip("端末内で順番/位置を保存して、アプリ再起動しても被りなく続ける")]
    [SerializeField] private bool persistAcrossSessions = true;

    [Tooltip("保存キーの接頭辞。PuzzleとMemoryで分けたいなら変える")]
    [SerializeField] private string prefsKeyPrefix = "QR_BAG";

    private List<Sprite> sprites;      // ソート済み（デバッグしやすい）
    private int[] order;               // シャッフル順（spritesのインデックス配列）
    private int cursor;                // 次に出す位置

    private string KeyOrder => $"{prefsKeyPrefix}:{resourcesPath}:order";
    private string KeyCursor => $"{prefsKeyPrefix}:{resourcesPath}:cursor";
    private string KeyHash => $"{prefsKeyPrefix}:{resourcesPath}:hash";

    private void Start()
    {
        if (chooseOnEnable) PickAndApplyNext();
    }

    private void Reset()
    {
        if (!targetImage) targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!targetImage) targetImage = GetComponent<Image>();
        EnsureLoaded();
        EnsureOrder();
    }

    private void OnEnable()
    {
        if (chooseOnEnable) PickAndApplyNext();
    }

    /// <summary>外部から明示的に次のQRに進めたい時</summary>
    public void PickAndApplyNext()
    {
        EnsureLoaded();
        if (sprites == null || sprites.Count == 0 || targetImage == null) return;

        EnsureOrder();

        // 全消化したら次周へ
        if (cursor >= order.Length)
        {
            ShuffleOrder();
            cursor = 0;
            SaveState();
        }

        int spriteIndex = order[cursor];
        cursor++;
        SaveState();

        var sp = sprites[spriteIndex];
        targetImage.sprite = sp;
        targetImage.preserveAspect = true;
        targetImage.enabled = true;

    }

    /// <summary>中身差し替え/枚数増減した時などに強制リロード</summary>
    public void ReloadAndReshuffle()
    {
        LoadSprites(forceReload: true);
        ShuffleOrder();
        cursor = 0;
        SaveState();
        PickAndApplyNext();
    }

    private void EnsureLoaded()
    {
        if (sprites != null && sprites.Count > 0) return;
        LoadSprites(forceReload: false);
    }

    private void LoadSprites(bool forceReload)
    {
        if (!forceReload && sprites != null && sprites.Count > 0) return;

        sprites = new List<Sprite>();
        if (string.IsNullOrWhiteSpace(resourcesPath))
        {
            Debug.LogWarning("[RandomQrImageBag] resourcesPath is empty.");
            return;
        }

        var all = Resources.LoadAll<Sprite>(resourcesPath);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning($"[RandomQrImageBag] No sprites found in Resources/{resourcesPath}. " +
                             $"(Put files under Assets/Resources/{resourcesPath})");
            return;
        }

        var filtered = all.Where(s => s != null);
        if (!string.IsNullOrEmpty(namePrefix))
            filtered = filtered.Where(s => s.name.StartsWith(namePrefix));

        // 名前順で安定化（保存した順番が環境で変わりにくい）
        sprites = filtered.OrderBy(s => s.name).ToList();

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[RandomQrImageBag] Sprites exist but none matched prefix '{namePrefix}' in Resources/{resourcesPath}.");
            return;
        }
    }

    private void EnsureOrder()
    {
        if (sprites == null || sprites.Count == 0) return;

        string currentHash = ComputeListHash(sprites);

        if (persistAcrossSessions)
        {
            // 枚数が増減した場合など、保存データが古ければ作り直す
            string savedHash = PlayerPrefs.GetString(KeyHash, "");
            if (!string.Equals(savedHash, currentHash, StringComparison.Ordinal) ||
                !TryLoadState())
            {
                ShuffleOrder();
                cursor = 0;
                SaveState(currentHash);
            }
        }
        else
        {
            if (order == null || order.Length != sprites.Count)
            {
                ShuffleOrder();
                cursor = 0;
            }
        }
    }

    private void ShuffleOrder()
    {
        int n = sprites.Count;
        order = Enumerable.Range(0, n).ToArray();

        // Fisher–Yates shuffle
        for (int i = n - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
    }

    private void SaveState(string hashOverride = null)
    {
        if (!persistAcrossSessions || order == null) return;

        PlayerPrefs.SetString(KeyOrder, string.Join(",", order));
        PlayerPrefs.SetInt(KeyCursor, cursor);

        if (hashOverride != null)
            PlayerPrefs.SetString(KeyHash, hashOverride);

        PlayerPrefs.Save();
    }

    private bool TryLoadState()
    {
        string orderStr = PlayerPrefs.GetString(KeyOrder, "");
        if (string.IsNullOrEmpty(orderStr)) return false;

        var parts = orderStr.Split(',');
        if (parts.Length != sprites.Count) return false;

        var tmp = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out tmp[i])) return false;
            if (tmp[i] < 0 || tmp[i] >= sprites.Count) return false;
        }

        order = tmp;
        cursor = Mathf.Clamp(PlayerPrefs.GetInt(KeyCursor, 0), 0, order.Length);
        return true;
    }

    private static string ComputeListHash(List<Sprite> list)
    {
        // 名前列をハッシュ化（簡易）。枚数/内容が変わったら異なる値になる。
        unchecked
        {
            int hash = 17;
            foreach (var s in list)
                hash = hash * 31 + (s ? s.name.GetHashCode() : 0);
            return hash.ToString();
        }
    }
}