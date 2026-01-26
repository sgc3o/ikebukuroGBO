using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("Stage Rule")]
    [SerializeField] private int hitCount = 6;

    [Header("Spawn References")]
    [SerializeField] private CapsuleItem capsulePrefab;
    [SerializeField] private RectTransform capsuleRoot;   // CapsuleRoot（GridLayoutGroupはOFF推奨）
    [SerializeField] private RectTransform spawnArea;     // CapsuleArea（いまは使ってないけど参照チェック用）

    [Header("Hit Sprites (Inspectorで差し替え)")]
    [SerializeField] private List<Sprite> hitSprites = new(); // PV03_Common_Atari01〜などを入れる

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "PV04_Incentive";

    [Header("Placement (18 slots fixed = 6x3)")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 3;

    [Header("Cluster (compressed grid area)")]
    [SerializeField] private float clusterWidth = 420f;
    [SerializeField] private float clusterHeight = 240f;
    [SerializeField] private Vector2 clusterOffset = Vector2.zero;

    [Header("Offset (ばらつき演出)")]
    [SerializeField] private float offsetX = 18f;
    [SerializeField] private float offsetY = 10f;

    [Header("Overlap Rule (approx)")]
    [SerializeField] private float capsuleSizePx = 116f;
    [SerializeField] private float allowedOverlapPx = 10f;

    // 3枚目の「順位表」を埋め順（0〜17のslot index）
    // row-major（上段0..5 / 中段6..11 / 下段12..17）
    private static readonly int[] PlacementOrder18 =
   {
    8,  9,  2,  3, 14, 15,
    7, 10,  1,  4, 13, 16,
    6, 11,  0,  5, 12, 17
};

    private readonly List<CapsuleItem> spawned = new();
    private readonly List<Vector2> placedPositions = new();

    private int totalCount;
    private int missCount;

    private int foundHit = 0;

    private void Start()
    {
        StartStage();
    }

    private void StartStage()
    {
        foundHit = 0;

        missCount = Mathf.FloorToInt(hitCount * 0.6f); // floor(hit*0.6)
        totalCount = hitCount + missCount;

        if (totalCount > 18)
        {
            Debug.LogError($"[StageManager] totalCount={totalCount} は上限18を超えています。画像数を減らしてください。 (hit={hitCount}, miss={missCount})");
            return;
        }

        if (capsulePrefab == null || capsuleRoot == null || spawnArea == null)
        {
            Debug.LogError("[StageManager] Inspector参照が未設定です。capsulePrefab / capsuleRoot / spawnArea を確認してください。");
            return;
        }

        if (hitCount > 0 && (hitSprites == null || hitSprites.Count == 0))
        {
            Debug.LogWarning("[StageManager] hitSprites が空です。当たり画像が差し替えできません。Inspectorで入れてください。");
        }

        if (hitSprites != null && hitSprites.Count < hitCount)
        {
            Debug.LogWarning($"[StageManager] hitSprites の数({hitSprites.Count})が hitCount({hitCount})より少ないです。足りない分は先頭から使い回します。");
        }

        ClearSpawned();
        SpawnCapsules(totalCount, hitCount);
    }

    private void SpawnCapsules(int total, int hit)
    {
        // 1) 18枠の中心座標（row-majorで18個）
        List<Vector2> slotsRowMajor = BuildSlots18_RowMajor();

        // 2) 中心からの順で total 個だけ使う（配置自体は固定）
        List<Vector2> chosen = new List<Vector2>(total);
        for (int k = 0; k < total; k++)
        {
            int slotIndex = PlacementOrder18[k];
            chosen.Add(slotsRowMajor[slotIndex]);
        }

        // 3) chosen の中で「当たり位置」だけランダム抽選（重複なし）
        HashSet<int> hitIndices = PickUniqueIndices(total, hit);

        // 4) 当たり画像（重複なし）を作る：hitCount枚ぶん
        List<Sprite> hitSpriteList = BuildHitSpriteList(hit);

        // 5) 生成・配置
        float minDist = capsuleSizePx - allowedOverlapPx; // 116-10=106
        int hitSpriteCursor = 0;

        for (int i = 0; i < total; i++)
        {
            bool isHit = hitIndices.Contains(i);
            Sprite spriteForThis = null;

            if (isHit)
            {
                spriteForThis = hitSpriteList[hitSpriteCursor];
                hitSpriteCursor++;
            }

            Vector2 basePos = chosen[i];
            Vector2 pos = ApplyDeterministicOffset(basePos, i);

            // 近すぎ救済：オフセット無しに戻す（中心骨格寄り）
            if (!IsFarEnough(pos, minDist))
            {
                pos = basePos;
            }

            CapsuleItem item = Instantiate(capsulePrefab, capsuleRoot);
            item.Setup(this, isHit, spriteForThis);

            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;

            spawned.Add(item);
            placedPositions.Add(pos);
        }

        Debug.Log($"[StageManager] Spawn done. hit={hitCount}, miss={missCount}, total={totalCount}");
    }

    // 当たり画像を hit 枚ぶん作る（基本：重複なし、足りなければ使い回し）
    private List<Sprite> BuildHitSpriteList(int hit)
    {
        var result = new List<Sprite>(hit);

        if (hitSprites == null || hitSprites.Count == 0)
        {
            // 空でも落ちないように
            for (int i = 0; i < hit; i++) result.Add(null);
            return result;
        }

        // hitSprites をシャッフルして先頭から使う
        var pool = new List<Sprite>(hitSprites);
        Shuffle(pool);

        for (int i = 0; i < hit; i++)
        {
            // 足りない分は先頭から使い回し
            result.Add(pool[i % pool.Count]);
        }

        return result;
    }

    // total個のうち count個のインデックスを重複なしで選ぶ
    private HashSet<int> PickUniqueIndices(int total, int count)
    {
        var list = new List<int>(total);
        for (int i = 0; i < total; i++) list.Add(i);

        Shuffle(list);

        var set = new HashSet<int>();
        for (int i = 0; i < count; i++) set.Add(list[i]);
        return set;
    }

    // リストをシャッフル（UnityEngine.Random 使用）
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // 18枠を row-major（上段→中段→下段、左→右）で作る
    private List<Vector2> BuildSlots18_RowMajor()
    {
        var slots = new List<Vector2>(columns * rows);

        Vector2 center = clusterOffset;
        float left = center.x - clusterWidth * 0.5f;
        float bottom = center.y - clusterHeight * 0.5f;

        float pitchX = clusterWidth / columns;
        float pitchY = clusterHeight / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = left + pitchX * 0.5f + c * pitchX;
                float y = bottom + pitchY * 0.5f + r * pitchY;
                slots.Add(new Vector2(x, y));
            }
        }
        return slots;
    }

    // 決定論オフセット（毎回同じばらつき）
    private Vector2 ApplyDeterministicOffset(Vector2 basePos, int i)
    {
        float nx = Hash01(i * 17 + 3) * 2f - 1f;  // -1..1
        float ny = Hash01(i * 29 + 7) * 2f - 1f;
        return basePos + new Vector2(nx * offsetX, ny * offsetY);
    }

    private float Hash01(int n)
    {
        float s = Mathf.Sin(n * 12.9898f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

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

    public void OnHitFound()
    {
        foundHit++;
        // TODO: HUD更新（foundHit / hitCount）

        if (foundHit >= hitCount)
        {
            GoNextScene();
        }
    }

    public void GoNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
