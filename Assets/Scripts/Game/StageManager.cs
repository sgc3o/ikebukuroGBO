using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class StageManager : MonoBehaviour
{
    [Header("Stage Rule")]
    [SerializeField] private int hitCount = 6;
    [SerializeField] private float hitRate = 0.55f;

    [Header("Spawn")]
    [SerializeField] private CapsuleItem capsulePrefab;
    [SerializeField] private Transform capsuleRoot;  // Gridの親（RectTransformでもOK）

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "PV04_Incentive"; // クリア後

    [Header("Placement (Grid-like Random)")]　//位置をランダムに
    [SerializeField] private RectTransform spawnArea; // CapsuleArea を入れる
    [SerializeField] private int columns = 4;         // 4列
    [SerializeField] private int rows = 3;            // 3行
    [SerializeField] private Vector2 cellSize = new Vector2(100, 100);
    [SerializeField] private Vector2 spacing = new Vector2(20, 20);
    [SerializeField] private Vector2 padding = new Vector2(0, 0); // 左下からの余白（必要なら）

    // --- 状態 ---
    private int foundHits = 0;
    private readonly List<CapsuleItem>  spawned = new();

    private void Start()
    {
        StartStage();
    }
    public void StartStage()
    {
        foundHits = 0;

        // 総数を計算（目安：round(hitCount / hitRate)）
        int totalCount = Mathf.RoundToInt(hitCount / hitRate); //四捨五入的な処理

        //安全　総数が当たり判定以下にならないように
        if (totalCount < hitCount) totalCount = hitCount;

        ClearSpawned();
        SpawnCapsules(totalCount, hitCount);

        Debug.Log($"Stage Start: total={totalCount}, hit={hitCount}");



    }
    private List<Vector2> BuildCellPositions()     //マス目座標”を作る（12個の座標リストを作る）

    {
        var positions = new List<Vector2>();

        // TODO: spawnArea の幅と高さを取得する
        float areaW = spawnArea.rect.width;
        float areaH = spawnArea.rect.height;

        // 左下基準のスタート位置（padding + セルの半分）
        float startX = -areaW * 0.5f + padding.x + cellSize.x * 0.5f;
        float startY = -areaH * 0.5f + padding.y + cellSize.y * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = startX + c * (cellSize.x + spacing.x);
                float y = startY + r * (cellSize.y + spacing.y);

                positions.Add(new Vector2(x, y));
            }
        }

        return positions;
    }

    private void ShufflePositions(List<Vector2> positions)  //座標リストをシャッフル（flagsと同じ）
    {
        for (int i = 0; i < positions.Count; i++)
        {
            int r = Random.Range(i, positions.Count);
            (positions[i], positions[r]) = (positions[r], positions[i]);
        }
    }



    private void SpawnCapsules(int totalCount, int hitCount)
    {
        // ===== ① 当たりフラグ配列を作る =====
        // true = 当たり, false = はずれ
        List<bool> flags = new List<bool>();

        for (int i = 0; i < totalCount; i++)
        {
            flags.Add(i < hitCount);
        }

        // シャッフル（どれが当たりかをランダムに）
        for (int i = 0; i < flags.Count; i++)
        {
            int r = Random.Range(i, flags.Count);
            (flags[i], flags[r]) = (flags[r], flags[i]);
        }

        // ===== ② マス目座標を作る =====
        List<Vector2> positions = BuildCellPositions();

        // 安全チェック：マスが足りなかったら中断
        if (totalCount > positions.Count)
        {
            Debug.LogError("配置マス数が足りません！");
            return;
        }

        // 座標もシャッフル（配置をランダムっぽく）
        ShufflePositions(positions);

        // ===== ③ カプセルを生成して配置 =====
        for (int i = 0; i < totalCount; i++)
        {
            CapsuleItem item = Instantiate(capsulePrefab, capsuleRoot);

            // 当たり or はずれを渡す
            item.Setup(this, flags[i]);

            // UIなので RectTransform で位置指定
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchoredPosition = positions[i];
        }
    }


    private void ClearSpawned()　//前のステージで作ったカプセルを全部消す(1st→2ndに行くとき邪魔)
    {
        for (int i = 0;i < spawned.Count;i++)
        {
            if (spawned[i] != null) Destroy(spawned[i].gameObject);
        }
        spawned.Clear();
    }


    // CapsuleItem から呼ばれる：当たり見つけた
    public void OnHitFound()
    {
        // TODO: foundHits を1増やす
        foundHits++;
        Debug.Log($"Hit Found: {foundHits}/{hitCount}");

        // TODO: foundHits >= hitCount ならクリア処理
        if (foundHits >= hitCount) {
            OnStageCleared();
        }
    }


    private void OnStageCleared()
    {
        Debug.Log("Stage Cleared!");
        // まずは次のシーンへ（ステージ2があるならここで分岐）
        SceneManager.LoadScene(nextSceneName);
    }

    


}


