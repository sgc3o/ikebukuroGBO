using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    [Header("Next Scene (PV04)")]
    [SerializeField] private string pv04SceneName = "PV04_Incentive"; // ★PV04のScene名

    [Header("Stage Rule")]
    [SerializeField] private int hitCount = 6;

    [Header("Spawn References")]
    [SerializeField] private CapsuleItem capsulePrefab;
    [SerializeField] private RectTransform capsuleRoot;

    [Header("Hit Sprites (Inspectorで差し替え)")]
    [SerializeField] private List<Sprite> hitSprites = new();

    [Header("Fixed Placement (Optional, up to 10)")]
    [SerializeField] private bool useFixedPositions = false;
    [SerializeField] private List<RectTransform> fixedPoints = new();

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
    [SerializeField, Range(0f, 1f)] private float offsetMinRatio = 0.25f; // 0付近でも最低これだけズラす
    [SerializeField, Range(0.1f, 2f)] private float offsetPower = 0.6f;   // 0.6くらいで「均等に散る」寄り


    [Header("Offset Tuning (Global)")]
    [SerializeField, Range(0.2f, 2f)] private float offsetBias = 0.7f;    // <1で大きめが出やすい

    [Header("Offset Tuning (Falloff + Clamp)")]
    [SerializeField, Range(0f, 0.49f)] private float minJitter01 = 0.25f; // 0.25=常に最大の25%はズレる(=約6px)
    [SerializeField, Range(0f, 1f)] private float edgeMinFactor = 0.35f;  // 端でもこれ以上は効く
    [SerializeField] private float edgeFalloffX = 60f; // 端からこの距離以内だとXオフセットが弱まる
    [SerializeField] private float edgeFalloffY = 60f; // 端からこの距離以内だとYオフセットが弱まる
    [SerializeField] private float clampPadding = 0f;   // 枠内に余白を取りたい場合（0でOK）


    [Header("Overlap Rule (approx)")]
    [SerializeField] private float capsuleSizePx = 116f;
    [SerializeField] private float allowedOverlapPx = 10f;

    [Header("HUD References")]
    [SerializeField] private Image stageCountImage;
    [SerializeField] private Sprite stageCount1of2;
    [SerializeField] private Sprite stageCount2of2;
    [SerializeField] private TMP_Text atariCounterText;

    [Header("Timer")]
    [SerializeField] private CountdownUI countdownUI;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float stage1LimitSec = 30f;
    [SerializeField] private float stage2LimitSec = 30f;

    [Header("Popup")]
    [SerializeField] private PopupManager popup;

    [Header("Spawn In Animation")]
    [SerializeField] private float spawnDuration = 1.0f;
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float startAngleRight = 180f;
    [SerializeField] private float startAngleOther = -180f;
    [SerializeField] private float offscreenPadding = 10f;
    [SerializeField, Range(0f, 0.5f)] private float centerBandRatio = 0.18f;
    [SerializeField] private float edgeJitterX = 30f;
    [SerializeField] private float edgeJitterY = 25f;

    private static readonly int[] PlacementOrder18 =
    {
    // 1マス内側の「角っぽい」場所から始める（端すぎない）
    1, 4, 13, 16,
    2, 3, 14, 15,
    6, 11, 7, 10,
    0, 5, 12, 17,
    8, 9
};

    private readonly List<CapsuleItem> spawned = new();
    private readonly List<Vector2> placedPositions = new();

    private int totalCount;
    private int missCount;
    // "当たり"概念撤廃後は「開封数」として使う
    private int openedCount = 0;

    private float timeLeft;
    private Coroutine timerCo;

    private bool stageEnding = false;

    private enum SpawnEdge { Left, Right, Bottom }

    private void Start()
    {
        //Debug.Log("[StageManager] Start()");

        StartStage();
    }

    public void StartStage()
    {
        if (timerGroup != null) timerGroup.SetActive(false);

        stageEnding = false;

        ApplyStageCountHUD();

        openedCount = 0;
        ApplyAtariCounterHUD();

        int maxSlots = columns * rows; // 6x3想定なら18

        // 旧仕様の比率（hitCount * 0.6）を総数の密度調整として流用
        missCount = Mathf.FloorToInt(hitCount * 0.6f);

        // 18を超えないように上限カット（hit=12なら missは6までに制限される）
        missCount = Mathf.Clamp(missCount, 0, Mathf.Max(0, maxSlots - hitCount));

        totalCount = hitCount + missCount;


        

        // Fixed placement: if points are fewer than total, shrink total to fit (no error)
        if (useFixedPositions && fixedPoints != null && fixedPoints.Count > 0)
            totalCount = Mathf.Min(totalCount, fixedPoints.Count);
ClearSpawned();

        StartCoroutine(StartStageFlow());
    }

    private void ApplyStageCountHUD()
    {
        if (stageCountImage == null) return;
        if (GameSession.I == null) return;

        if (GameSession.I.CurrentStage == 1)
        {
            if (stageCount1of2 != null) stageCountImage.sprite = stageCount1of2;
        }
        else
        {
            if (stageCount2of2 != null) stageCountImage.sprite = stageCount2of2;
        }
    }

    private void ApplyAtariCounterHUD()
    {
        if (atariCounterText == null) return;
        // 当たり概念撤廃：開封数 / 総数
        int denom = Mathf.Max(1, totalCount);
        atariCounterText.text = $"{openedCount}/{denom}";
    }

    private void StartTimer()
    {
        if (timerCo != null) StopCoroutine(timerCo);

        int stage = (GameSession.I != null) ? GameSession.I.CurrentStage : 1;
        timeLeft = (stage >= 2) ? stage2LimitSec : stage1LimitSec;

        timerCo = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;

            UpdateTimerText();
            yield return null;
        }

        OnTimeUp();
    }

    private void UpdateTimerText()
    {
        int sec = Mathf.CeilToInt(timeLeft);

        // 画像カウントダウン
        if (countdownUI != null)
            countdownUI.ShowNumber(sec);

        // 旧TMP（残したいなら）
        if (timerText != null)
            timerText.text = sec.ToString();
    }



    private void OnTimeUp()
    {
        HandleStageEnd();
    }

    public void OnHitFound()
    {
        openedCount++;
        ApplyAtariCounterHUD();

        // 当たり概念撤廃：全て開いたらクリア
        if (openedCount >= totalCount)
            HandleStageEnd();
    }

    private void HandleStageEnd()
    {
        if (stageEnding) return;
        stageEnding = true;

        if (timerCo != null)
        {
            StopCoroutine(timerCo);
            timerCo = null;
        }

        if (GameSession.I == null)
        {
            Debug.LogError("[StageManager] GameSession が見つかりません。PV04へ遷移します。");
            SceneManager.LoadScene(pv04SceneName);
            return;
        }

        bool hasNext = GameSession.I.TryAdvanceStage();

        if (popup != null)
        {
            popup.ShowFinish();
            StartCoroutine(AfterFinish(hasNext));
            return;
        }

        // popup無し
        if (hasNext) StartStage();
        else
        {
            GameSession.I.ResetStages();
            SceneTransition.Go(pv04SceneName); // ★ここにする
        }
    }

    private IEnumerator AfterFinish(bool hasNext)
    {
        // 既存と同じ固定秒。Popup側で管理したいなら差し替えOK
        yield return new WaitForSeconds(2.4f);

        if (hasNext)
        {
            StartStage(); // ★シーンリロードせず次ステージへ
        }
        else
        {
            GameSession.I.ResetStages();
            SceneTransition.Go(pv04SceneName); // ★最後だけScene移動
        }
    }

    private IEnumerator StartStageFlow()
    {
        EnableCapsuleInput(false);

        if (popup != null && GameSession.I != null)
            popup.ShowStage(GameSession.I.CurrentStage);

        if (popup != null)
            yield return new WaitUntil(() => !popup.IsShowing);

        // 当たり概念撤廃：全カプセルがキャラ画像になる
        SpawnCapsules(totalCount);

        yield return new WaitForSeconds(spawnDuration);

        if (popup != null)
            popup.ShowGameStart();

        if (popup != null)
            yield return new WaitUntil(() => !popup.IsShowing);

        EnableCapsuleInput(true);

        if (timerGroup != null) timerGroup.SetActive(true);
        StartTimer();
        UpdateTimerText();
    }

    private void SpawnCapsules(int total)
    {
                int actualTotal = total;

        List<Vector2> chosen;

        if (useFixedPositions && fixedPoints != null && fixedPoints.Count > 0)
        {
            actualTotal = Mathf.Min(total, fixedPoints.Count);
            chosen = new List<Vector2>(actualTotal);
            for (int k = 0; k < actualTotal; k++)
                chosen.Add(ToCapsuleRootAnchoredPosition(fixedPoints[k]));
        }
        else
        {
            List<Vector2> slotsRowMajor = BuildSlots18_RowMajor();

            chosen = new List<Vector2>(actualTotal);
            for (int k = 0; k < actualTotal; k++)
            {
                int slotIndex = PlacementOrder18[k];
                chosen.Add(slotsRowMajor[slotIndex]);
            }
        }

        // 全員キャラ：actualTotal分のスプライトを作る
        List<Sprite> spriteList = BuildHitSpriteList(actualTotal);

        float minDist = capsuleSizePx - allowedOverlapPx;
        int spriteCursor = 0;

        for (int i = 0; i < actualTotal; i++)
        {
            // 当たり概念撤廃：常にHit扱いでキャラ画像を割り当て
            bool isHit = true;
            Sprite spriteForThis = spriteList[spriteCursor % spriteList.Count];
            spriteCursor++;

            Vector2 basePos = chosen[i];
            Vector2 pos = ApplyDeterministicOffset(basePos, i);
            pos = ResolveOverlapByShrinking(basePos, pos, minDist);


            CapsuleItem item = Instantiate(capsulePrefab, capsuleRoot);
            item.Setup(this, isHit, spriteForThis);

            RectTransform rt = item.GetComponent<RectTransform>();

            SpawnEdge edge;
            Vector2 startPos = CalcSpawnStartPos(pos, rt.sizeDelta, capsuleRoot, out edge);

            rt.anchoredPosition = startPos;
            item.SetInteractable(false);

            StartCoroutine(PlaySpawnIn(rt, item.Rotator, startPos, pos, edge));
            StartCoroutine(EnableAfter(item, spawnDuration));

            spawned.Add(item);
            placedPositions.Add(pos);
        }
    }

    [Header("Offset Overlap Fix (Shrink)")]
    [SerializeField, Range(0.1f, 0.95f)] private float overlapShrink = 0.7f;
    [SerializeField, Range(1, 20)] private int overlapShrinkIters = 8;

    private Vector2 ResolveOverlapByShrinking(Vector2 basePos, Vector2 candidate, float minDist)
    {
        Vector2 offset = candidate - basePos;
        Vector2 cur = candidate;

        for (int t = 0; t < overlapShrinkIters; t++)
        {
            if (IsFarEnough(cur, minDist)) return cur;

            offset *= overlapShrink;          // 減衰
            cur = basePos + offset;
        }

        // どうしてもダメなら最後の保険
        return basePos;
    }

    private Vector2 ApplyOffsetFalloffAndClamp(Vector2 basePos, int i, RectTransform root)
    {
        // ラフなカプセル半径（RectTransformサイズを取るより安定するので既存の capsuleSizePx を使う）
        float half = capsuleSizePx * 0.5f;

        Rect rect = root.rect;

        // 安全に収める領域（カプセル半径ぶん内側）
        float minX = rect.xMin + half + clampPadding;
        float maxX = rect.xMax - half - clampPadding;
        float minY = rect.yMin + half + clampPadding;
        float maxY = rect.yMax - half - clampPadding;

        // まずベース位置自体が安全域に入ってる前提にする（ここで軽く補正）
        float bx = Mathf.Clamp(basePos.x, minX, maxX);
        float by = Mathf.Clamp(basePos.y, minY, maxY);
        Vector2 safeBase = new Vector2(bx, by);

        float nx = Hash01(i * 17 + 3) * 2f - 1f;
        float ny = Hash01(i * 29 + 7) * 2f - 1f;

        // 0付近を避けて「必ずちょいズレ」を作る（最大値は変えない）
        nx = WithMinAbs(nx, minJitter01);
        ny = WithMinAbs(ny, minJitter01);

        Vector2 rawOffset = new Vector2(nx * offsetX, ny * offsetY);


        // 端に近いほど弱める（減衰）
        float distLeft = safeBase.x - minX;
        float distRight = maxX - safeBase.x;
        float distBottom = safeBase.y - minY;
        float distTop = maxY - safeBase.y;

        float fxRaw = Mathf.Clamp01(Mathf.Min(distLeft, distRight) / Mathf.Max(1f, edgeFalloffX));
        float fyRaw = Mathf.Clamp01(Mathf.Min(distBottom, distTop) / Mathf.Max(1f, edgeFalloffY));

        // 端でも edgeMinFactor は残す
        float fx = Mathf.Lerp(edgeMinFactor, 1f, fxRaw);
        float fy = Mathf.Lerp(edgeMinFactor, 1f, fyRaw);

        Vector2 dampedOffset = new Vector2(rawOffset.x * fx, rawOffset.y * fy);


        // 最終位置（まだ出そうならクランプ）
        Vector2 candidate = safeBase + dampedOffset;
        candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
        candidate.y = Mathf.Clamp(candidate.y, minY, maxY);

        return candidate;
    }

    private float WithMinAbs(float v, float min01)
    {
        float s = Mathf.Sign(v);
        float a = Mathf.Abs(v);              // 0..1

        // ★ここでバイアス：0付近を避けたいなら offsetBias < 1 が効く
        a = Mathf.Pow(a, Mathf.Max(0.001f, offsetBias));

        a = Mathf.Lerp(min01, 1f, a);        // min01..1 （底上げ）
        return s * a;
    }



    private List<Sprite> BuildHitSpriteList(int hit)
    {
        var result = new List<Sprite>(hit);

        if (hitSprites == null || hitSprites.Count == 0)
        {
            for (int i = 0; i < hit; i++) result.Add(null);
            return result;
        }

        var pool = new List<Sprite>(hitSprites);
        Shuffle(pool);

        for (int i = 0; i < hit; i++)
            result.Add(pool[i % pool.Count]);

        return result;
    }

    private HashSet<int> PickUniqueIndices(int total, int count)
    {
        var list = new List<int>(total);
        for (int i = 0; i < total; i++) list.Add(i);

        Shuffle(list);

        var set = new HashSet<int>();
        for (int i = 0; i < count; i++) set.Add(list[i]);
        return set;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

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


    private Vector2 ToCapsuleRootAnchoredPosition(RectTransform point)
    {
        if (point == null || capsuleRoot == null) return Vector2.zero;

        // 同じ座標系（CapsuleRoot直下）ならそのまま使える
        if (point.transform.parent == capsuleRoot)
            return point.anchoredPosition;

        // Canvasのモードに応じてカメラを取得（OverlayならnullでOK）
        Canvas canvas = capsuleRoot.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, point.position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(capsuleRoot, screen, cam, out Vector2 local))
            return local;

        // 予備（ほぼ来ない想定）
        return (Vector2)capsuleRoot.InverseTransformPoint(point.position);
    }

    private Vector2 ApplyDeterministicOffset(Vector2 basePos, int i)
    {
        float nx = Hash01(i * 17 + 3) * 2f - 1f;
        float ny = Hash01(i * 29 + 7) * 2f - 1f;

        // ここが「変換を挟む」ポイント
        nx = BiasAwayFromZero(nx, offsetMinRatio, offsetPower);
        ny = BiasAwayFromZero(ny, offsetMinRatio, offsetPower);

        return basePos + new Vector2(nx * offsetX, ny * offsetY);
    }

    private float BiasAwayFromZero(float v, float minRatio, float power)
    {
        float sign = Mathf.Sign(v);
        float a = Mathf.Abs(v);                    // 0..1
        a = Mathf.Pow(a, power);                   // 0付近を持ち上げやすくする
        a = Mathf.Lerp(minRatio, 1f, a);           // 最低ズレ量を保証（0に近くても minRatio）
        return sign * a;                           // -1..1 に戻す
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

    private void EnableCapsuleInput(bool enable)
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                spawned[i].SetInteractable(enable);
        }
    }

    private SpawnEdge DecideEdge(Vector2 targetLocal, Rect rect)
    {
        float band = rect.width * centerBandRatio;
        if (targetLocal.x < -band) return SpawnEdge.Left;
        if (targetLocal.x > band) return SpawnEdge.Right;
        return SpawnEdge.Bottom;
    }

    private Vector2 CalcSpawnStartPos(Vector2 targetLocal, Vector2 size, RectTransform root, out SpawnEdge edge)
    {
        Rect rect = root.rect;
        edge = DecideEdge(targetLocal, rect);

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        float leftX = rect.xMin - halfW - offscreenPadding;
        float rightX = rect.xMax + halfW + offscreenPadding;
        float bottomY = rect.yMin - halfH - offscreenPadding;

        Vector2 start = targetLocal;

        switch (edge)
        {
            case SpawnEdge.Left:
                start.x = leftX;
                start.y += Random.Range(-edgeJitterY, edgeJitterY);
                break;

            case SpawnEdge.Right:
                start.x = rightX;
                start.y += Random.Range(-edgeJitterY, edgeJitterY);
                break;

            default:
                start.y = bottomY;
                start.x += Random.Range(-edgeJitterX, edgeJitterX);
                break;
        }

        return start;
    }

    private IEnumerator PlaySpawnIn(
        RectTransform rt,
        RectTransform rotator,
        Vector2 startPos,
        Vector2 endPos,
        SpawnEdge edge)
    {
        float t = 0f;
        float startAngle = (edge == SpawnEdge.Right) ? startAngleRight : startAngleOther;

        rt.anchoredPosition = startPos;
        if (rotator != null) rotator.localEulerAngles = new Vector3(0, 0, startAngle);

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, spawnDuration);
            float u = Mathf.Clamp01(t);

            float posT = moveEase.Evaluate(u);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, posT);

            if (rotator != null)
            {
                float ang = Mathf.Lerp(startAngle, 0f, u);
                rotator.localEulerAngles = new Vector3(0, 0, ang);
            }

            yield return null;
        }

        rt.anchoredPosition = endPos;
        if (rotator != null) rotator.localEulerAngles = Vector3.zero;
    }

    private IEnumerator EnableAfter(CapsuleItem item, float sec)
    {
        yield return new WaitForSeconds(sec);
        if (item != null) item.SetInteractable(true);
    }

    public bool IsInputBlocked
    {
        get { return popup != null && popup.IsShowing; }
    }

}
