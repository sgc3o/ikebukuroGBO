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

    [Header("HUD References")]
    [SerializeField] private Image stageCountImage;
    [SerializeField] private Sprite stageCount1of2;
    [SerializeField] private Sprite stageCount2of2;
    [SerializeField] private TMP_Text atariCounterText;

    [Header("Timer")]
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
        8,  9,  2,  3, 14, 15,
        7, 10,  1,  4, 13, 16,
        6, 11,  0,  5, 12, 17
    };

    private readonly List<CapsuleItem> spawned = new();
    private readonly List<Vector2> placedPositions = new();

    private int totalCount;
    private int missCount;
    private int foundHit = 0;

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

        foundHit = 0;
        ApplyAtariCounterHUD();

        missCount = Mathf.FloorToInt(hitCount * 0.6f);
        totalCount = hitCount + missCount;

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
        atariCounterText.text = $"{foundHit}/{hitCount}";
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
        if (timerText == null) return;
        int sec = Mathf.CeilToInt(timeLeft);
        timerText.text = sec.ToString();
    }

    private void OnTimeUp()
    {
        HandleStageEnd();
    }

    public void OnHitFound()
    {
        foundHit++;
        ApplyAtariCounterHUD();

        if (foundHit >= hitCount)
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
            SceneManager.LoadScene(pv04SceneName); // ★最後だけScene移動
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
            SceneManager.LoadScene(pv04SceneName); // ★最後だけScene移動
        }
    }

    private IEnumerator StartStageFlow()
    {
        EnableCapsuleInput(false);

        if (popup != null && GameSession.I != null)
            popup.ShowStage(GameSession.I.CurrentStage);

        if (popup != null)
            yield return new WaitUntil(() => !popup.IsShowing);

        SpawnCapsules(totalCount, hitCount);

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

    private void SpawnCapsules(int total, int hit)
    {
        List<Vector2> slotsRowMajor = BuildSlots18_RowMajor();

        List<Vector2> chosen = new List<Vector2>(total);
        for (int k = 0; k < total; k++)
        {
            int slotIndex = PlacementOrder18[k];
            chosen.Add(slotsRowMajor[slotIndex]);
        }

        HashSet<int> hitIndices = PickUniqueIndices(total, hit);
        List<Sprite> hitSpriteList = BuildHitSpriteList(hit);

        float minDist = capsuleSizePx - allowedOverlapPx;
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

            if (!IsFarEnough(pos, minDist))
                pos = basePos;

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

    private Vector2 ApplyDeterministicOffset(Vector2 basePos, int i)
    {
        float nx = Hash01(i * 17 + 3) * 2f - 1f;
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
