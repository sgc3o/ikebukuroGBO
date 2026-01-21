using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    // =========================================================
    // Inspectorで設定する項目
    // =========================================================

    [Header("Stage Rule")]
    [SerializeField] private int hitCount = 6;          // 当たり数
    [SerializeField] private float hitRate = 0.55f;     // 今回は未使用でもOK（残しておく）

    [Header("Spawn References")]
    [SerializeField] private CapsuleItem capsulePrefab; // CapsuleItemが付いたPrefab
    [SerializeField] private RectTransform capsuleRoot; // 生成先（CapsuleRoot）
    [SerializeField] private RectTransform spawnArea;   // 配置エリア（CapsuleArea）

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "PV04_Incentive";

    [Header("Placement (18 slots fixed = 6x3)")]
    [SerializeField] private int columns = 6;           // 固定: 6
    [SerializeField] private int rows = 3;              // 固定: 3

    [Header("Cluster (compressed grid area)")]
    [SerializeField] private float clusterWidth = 420f;  // ★中心寄せの幅
    [SerializeField] private float clusterHeight = 240f; // ★中心寄せの高さ
    [SerializeField] private Vector2 clusterOffset = Vector2.zero; // ★中心からの微調整（必要なら）

    [Header("Jitter (random offset)")]
    [SerializeField] private float jitterX = 18f;
    [SerializeField] private float jitterY = 10f;
    [SerializeField] private int jitterTriesPerCapsule = 10;

    [Header("Overlap Rule (approx)")]
    [SerializeField] private float capsuleSizePx = 116f;
    [SerializeField] private float allowedOverlapPx = 10f;

    // =========================================================
    // 実行時に使う内部データ
    // =========================================================
    private readonly List<CapsuleItem> spawned = new();
    private readonly List<Vector2> placedPositions = new();

    private int totalCount;   // hit + miss
    private int missCount;    // floor(hit * 0.6)

    // =========================================================
    // Unity lifecycle
    // =========================================================
    private void Start()
    {
        StartStage();
    }

    // =========================================================
    // Stage start
    // =========================================================
    private void StartStage()
    {
        // missCount = floor(hit * 0.6) で確定
        missCount = Mathf.FloorToInt(hitCount * 0.6f);
        totalCount = hitCount + missCount;

        // total > 18 のときはエラー吐いて停止
        if (totalCount > 18)
        {
            Debug.LogError($"[StageManager] totalCount={totalCount} は上限18を超えています。画像数を減らしてください。 (hit={hitCount}, miss={missCount})");
            return;
        }

        // 参照チェック
        if (capsulePrefab == null || capsuleRoot == null || spawnArea == null)
        {
            Debug.LogError("[StageManager] Inspectorの参照が未設定です。capsulePrefab / capsuleRoot / spawnArea を確認してください。");
            return;
        }

        ClearSpawned();
        SpawnCapsules(totalCount, hitCount);
    }

    // =========================================================
    // カプセル生成と配置
    // =========================================================
    private void SpawnCapsules(int total, int hit)
    {
        // A) 当たり/はずれフラグを作る → シャッフル
        List<bool> flags = new List<bool>(total);
        for (int i = 0; i < total; i++)
        {
            flags.Add(i < hit);
        }
        Shuffle(flags);

        // B) 候補枠18（6x3）を「中心クラスタ(420x240)」で作る → シャッフル
        List<Vector2> slots = BuildSlots18_ClusterCentered();
        Shuffle(slots);

        // totalが18未満なら「一部だけ使う」（先頭から total 個）
        float minDist = capsuleSizePx - allowedOverlapPx; // 116-10=106

        for (int i = 0; i < total; i++)
        {
            Vector2 center = slots[i];

            // C) オフセットのみで乱雑感（回転・スケールなし）
            Vector2 decidedPos = DecidePositionWithJitter(center, minDist);

            // D) 生成してSetupして配置
            CapsuleItem item = Instantiate(capsulePrefab, capsuleRoot);
            item.Setup(this, flags[i]);

            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = decidedPos;

            spawned.Add(item);
            placedPositions.Add(decidedPos);
        }

        Debug.Log($"[StageManager] Spawn done. hit={hitCount}, miss={missCount}, total={totalCount}, cluster={clusterWidth}x{clusterHeight}");
    }

    // =========================================================
    // ★中心寄せクラスタで 18枠（6x3）の中心座標を作る
    // - spawnAreaの中心(0,0)を基準に clusterWidth/Height の箱を作り
    // - その箱の中に 6x3 の中心点を均等配置
    // =========================================================
    private List<Vector2> BuildSlots18_ClusterCentered()
    {
        var slots = new List<Vector2>(columns * rows);

        // spawnArea内での「クラスタ中心」
        Vector2 clusterCenter = clusterOffset; // (0,0)ならspawnArea中央。必要なら微調整可

        // クラスタの左下（中心から半分引く）
        float left = clusterCenter.x - clusterWidth * 0.5f;
        float bottom = clusterCenter.y - clusterHeight * 0.5f;

        // ピッチ（中心間隔）
        float pitchX = clusterWidth / columns;
        float pitchY = clusterHeight / rows;

        // 各セルの中心を追加
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = left + pitchX * 0.5f + c * pitchX;
                float y = bottom + pitchY * 0.5f + r * pitchY;
                slots.Add(new Vector2(x, y));
            }
        }

        // 念のため：クラスタがspawnAreaを超えてないか軽く警告（止めはしない）
        if (spawnArea != null)
        {
            float areaW = spawnArea.rect.width;
            float areaH = spawnArea.rect.height;

            if (clusterWidth > areaW || clusterHeight > areaH)
            {
                Debug.LogWarning($"[StageManager] cluster({clusterWidth}x{clusterHeight}) が spawnArea({areaW}x{areaH}) より大きい可能性があります。");
            }
        }

        return slots;
    }

    // =========================================================
    // 中心からオフセットして置く。近すぎたら救済（グリッド寄りに戻す）
    // =========================================================
    private Vector2 DecidePositionWithJitter(Vector2 center, float minDist)
    {
        // 1) 通常オフセットで試す
        for (int t = 0; t < jitterTriesPerCapsule; t++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-jitterX, jitterX),
                Random.Range(-jitterY, jitterY)
            );

            if (IsFarEnough(candidate, minDist))
                return candidate;
        }

        // 2) オフセットを弱めて試す（救済1）
        float weakX = Mathf.Min(8f, jitterX);
        float weakY = Mathf.Min(6f, jitterY);
        for (int t = 0; t < jitterTriesPerCapsule; t++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-weakX, weakX),
                Random.Range(-weakY, weakY)
            );

            if (IsFarEnough(candidate, minDist))
                return candidate;
        }

        // 3) 最後は中心に戻す（救済2）
        if (!IsFarEnough(center, minDist))
            Debug.LogWarning("[StageManager] 近接制限を満たせないため、中心配置で確定しました（密度が高い可能性）");

        return center;
    }

    // =========================================================
    // 既に置いた点から十分離れているか
    // =========================================================
    private bool IsFarEnough(Vector2 candidate, float minDist)
    {
        float minDistSqr = minDist * minDist;
        for (int i = 0; i < placedPositions.Count; i++)
        {
            if ((placedPositions[i] - candidate).sqrMagnitude < minDistSqr)
                return false;
        }
        return true;
    }

    // =========================================================
    // リストシャッフル
    // =========================================================
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // =========================================================
    // 既存生成物の掃除
    // =========================================================
    private void ClearSpawned()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i].gameObject);
        }
        spawned.Clear();
        placedPositions.Clear();
    }

    // =========================================================
    // 次シーンへ（必要なら）
    // =========================================================
    public void GoNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // CapsuleItemから呼ぶ想定（必要なら）
    public void OnHitFound()
    {
        // TODO: ヒット数UI更新など
    }
}
