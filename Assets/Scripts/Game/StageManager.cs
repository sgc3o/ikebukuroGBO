using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("HUD References")]
    [SerializeField] private Image stageCountImage;     // Hud/StageCount の Image
    [SerializeField] private Sprite stageCount1of2;     // 1/2 のSprite
    [SerializeField] private Sprite stageCount2of2;     // 2/2 のSprite
    [SerializeField] private TMP_Text atariCounterText; // AtariCounterText を入れる

    [Header("Timer")]
    [SerializeField] private GameObject timerGroup; // Hud/TimerGroup
    [SerializeField] private TMP_Text timerText;     // Hud/TimerText を入れる
    [SerializeField] private float stage1LimitSec = 30f;
    [SerializeField] private float stage2LimitSec = 30f;

    [Header("Popup")]
    [SerializeField] private PopupManager popup;

    [Header("Spawn Animation")]
    [SerializeField] private float spawnAnimDuration = 1.0f;
    [SerializeField] private AnimationCurve spawnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);


    [Header("Rotate Pivot (Rotator)")]
    [SerializeField] private Vector2 rotatorPivot = new Vector2(0.75f, 0.25f); // 緑×のイメージ（調整OK）
    [SerializeField] private float spawnDuration = 1.0f;
   // [SerializeField] private AnimationCurve spawnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float spawnOffscreenPadding = 5f;

    [Header("Spawn In Animation")]

    // 位置だけイーズ（AEの赤線っぽい形にインスペクタで調整する）
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 回転はリニア固定（角度は端で変える）
    [SerializeField] private float startAngleRight = 180f;   // 右から：180 → 0
    [SerializeField] private float startAngleOther = -180f;  // 左/下：-180 → 0

    // どれくらい画面外から出すか
    [SerializeField] private float offscreenPadding = 10f;

    // “近い端”判定：中央帯なら下から、左右ならそれぞれ
    [SerializeField, Range(0f, 0.5f)] private float centerBandRatio = 0.18f;

    // 端スタートのばらつき（下からはX、左右からはYに効かせる）
    [SerializeField] private float edgeJitterX = 30f;
    [SerializeField] private float edgeJitterY = 25f;


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

    private float timeLeft;
    private Coroutine timerCo;
    private bool stageEndedByTimer = false;

    private bool stageEnding = false; // クリア/タイムアップの多重防止

    private enum SpawnEdge { Left, Right, Bottom }


    private void Start()
    {
        StartStage();
    }


    private void StartStage()
    {

        if (timerGroup != null) timerGroup.SetActive(false);

        stageEnding = false;
        ApplyStageCountHUD(); // ← まず表示を更新

        foundHit = 0;
        ApplyAtariCounterHUD(); // ★ここで 0/6 にする

        stageEndedByTimer = false;

        missCount = Mathf.FloorToInt(hitCount * 0.6f); // floor(hit*0.6)
        totalCount = hitCount + missCount;

        ClearSpawned();
        StartCoroutine(StartStageFlow());





    }

    private void ApplyStageCountHUD()
    {
        if (stageCountImage == null) return;
        if (GameSession.I == null) return;



        // 今は2ステージ固定の想定
        if (GameSession.I.CurrentStage == 1)
        {
            if (stageCount1of2 != null) stageCountImage.sprite = stageCount1of2;
        }
        else
        {
            if (stageCount2of2 != null) stageCountImage.sprite = stageCount2of2;
        }

        // Imageの表示更新（念のため）
        //stageCountImage.SetNativeSize(); // サイズをスプライトに合わせたい場合
        // ↑ サイズ固定なら、この行は消してOK
    }

    private void ApplyAtariCounterHUD()
    {
        if (atariCounterText == null) return;

        // foundHit は「見つけた当たり数」
        // hitCount は「総当たり数（例:6）」
        atariCounterText.text = $"{foundHit}/{hitCount}";
    }

    private void StartTimer()
    {
        if (timerCo != null) StopCoroutine(timerCo);

        int stage = (GameSession.I != null) ? GameSession.I.CurrentStage : 1;
        timeLeft = (stage >= 2) ? stage2LimitSec : stage1LimitSec;

        timerCo = StartCoroutine(TimerRoutine());
    }

    private System.Collections.IEnumerator TimerRoutine()
    {
        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;

            UpdateTimerText();
            yield return null;
        }

        // 時間切れ
        OnTimeUp();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        // 表示は整数が見やすい
        int sec = Mathf.CeilToInt(timeLeft);
        timerText.text = sec.ToString();
    }

    private void OnTimeUp()
    {
        Debug.Log("[StageManager] Time Up!");
        HandleStageEnd(); // ← これだけ
    }

    private System.Collections.IEnumerator StageIntroSequence()
    {
        // ① STAGE 表示
        if (popup != null && GameSession.I != null)
        {
            popup.ShowStage(GameSession.I.CurrentStage);
            yield return new WaitWhile(() => popup.IsShowing);

        }
        yield return new WaitForSeconds(2.4f);

        // ② カプセルIN（仮）
        yield return new WaitForSeconds(1.0f);

        // ③ GameStart
        if (popup != null)
        {
            popup.ShowGameStart();
        }
        yield return new WaitForSeconds(2.4f);

        // ④ ここからゲーム開始（タイマー表示→開始）
        if (timerGroup != null) timerGroup.SetActive(true);

        StartTimer();
        UpdateTimerText();

        // 入力ブロックしたいなら、ここで解除する（後で）
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
            // 端スタート位置（jitter込み）を計算
            SpawnEdge edge;
            Vector2 startPos = CalcSpawnStartPos(pos, rt.sizeDelta, capsuleRoot, out edge);

            // いったん開始位置に置いて、コルーチンで pos へ
            rt.anchoredPosition = startPos;

            // 生成直後は押せない方が安全（演出中タップ事故防止）
            item.SetInteractable(false);

            StartCoroutine(PlaySpawnIn(rt, item.Rotator, startPos, pos, edge));

            // 演出が終わったら押せるようにする（簡易版）
            StartCoroutine(EnableAfter(item, spawnDuration));

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

    // 生成後に呼ぶ（CapsuleのRectTransformと、内部のRotatorのRectTransformが取れる前提）
    private void PlaySpawnAnim(RectTransform capsuleRt, RectTransform rotatorRt, Button button, Vector2 finalAnchoredPos, float capsuleSizePx)
    {
        // クリック無効化（アニメ中に触れない）
        if (button != null) button.interactable = false;

        // Rotator の pivot を緑×へ（prefabで固定してもOK）
        if (rotatorRt != null)
        {
            rotatorRt.pivot = rotatorPivot;
        }

        // どの端から来るか決める
        var edge = DecideEdge(finalAnchoredPos.x);

        // startPos 計算（見切れないギリギリ）
        Vector2 startPos = CalcStartPos(edge, finalAnchoredPos, capsuleSizePx);

        // 初期配置：startPos に置いてからアニメで final へ
        capsuleRt.anchoredPosition = startPos;
        if (rotatorRt != null) rotatorRt.localEulerAngles = Vector3.zero;

        StartCoroutine(SpawnAnimCo(capsuleRt, rotatorRt, startPos, finalAnchoredPos, button));
    }

    private SpawnEdge DecideEdge(float finalX)
    {
        float rootW = capsuleRoot.rect.width;
        float threshold = rootW * centerBandRatio;

        if (finalX < -threshold) return SpawnEdge.Left;
        if (finalX > threshold) return SpawnEdge.Right;
        return SpawnEdge.Bottom;
    }

    private Vector2 CalcStartPos(SpawnEdge edge, Vector2 finalPos, float capsuleSizePx)
    {
        float rootW = capsuleRoot.rect.width;
        float rootH = capsuleRoot.rect.height;
        float halfW = rootW * 0.5f;
        float halfH = rootH * 0.5f;
        float halfCapsule = capsuleSizePx * 0.5f;

        switch (edge)
        {
            case SpawnEdge.Left:
                return new Vector2(-halfW - halfCapsule, finalPos.y);
            case SpawnEdge.Right:
                return new Vector2(+halfW + halfCapsule, finalPos.y);
            default: // Bottom
                return new Vector2(finalPos.x, -halfH - halfCapsule);
        }
    }

    private IEnumerator SpawnAnimCo(RectTransform capsuleRt, RectTransform rotatorRt, Vector2 start, Vector2 end, Button button)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, spawnAnimDuration);
            float eased = spawnEase != null ? spawnEase.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

            capsuleRt.anchoredPosition = Vector2.LerpUnclamped(start, end, eased);

            if (rotatorRt != null)
            {
                // 360度回転（Z軸）
                float z = Mathf.LerpUnclamped(0f, 360f, eased);
                rotatorRt.localEulerAngles = new Vector3(0f, 0f, z);
            }

            yield return null;
        }

        capsuleRt.anchoredPosition = end;
        if (rotatorRt != null) rotatorRt.localEulerAngles = Vector3.zero;

        // アニメ終わったらクリック可
        if (button != null) button.interactable = true;
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
        ApplyAtariCounterHUD();

        if (foundHit >= hitCount)
        {
            HandleStageEnd(); // ← これだけ呼ぶ
        }
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
            Debug.LogError("[StageManager] GameSession が見つかりません。");
            SceneTransition.Go("PV04_Incentive");
            return;
        }

        // 次ステージがある？
        bool hasNext = GameSession.I.TryAdvanceStage();

        // まずFinishを出す（ステージ1でも2でも）
        if (popup != null)
        {
            popup.ShowFinish();
            StartCoroutine(LoadAfterFinish(hasNext));
            return;
        }

        // popupが無い場合のフォールバック
        if (hasNext)
            SceneTransition.Go("PV03_Virusweets_Game");
        else
        {
            GameSession.I.ResetStages();
            SceneTransition.Go("PV04_Incentive");
        }
    }

    private System.Collections.IEnumerator LoadAfterFinish(bool hasNext)
    {
        yield return new WaitForSeconds(2.4f);

        if (hasNext)
        {
            SceneManager.LoadScene("PV03_Virusweets_Game");
        }
        else
        {
            GameSession.I.ResetStages();
            SceneManager.LoadScene("PV04_Incentive");
        }
    }
    public bool IsInputBlocked
    {
        get { return popup != null && popup.IsShowing; }
    }


    private Vector2 CalcSpawnStartPos(Vector2 endPos, Vector2 size)
    {
        // cluster（capsuleRoot）のローカル座標系を前提
        float halfW = clusterWidth * 0.5f;
        float halfH = clusterHeight * 0.5f;

        float leftX = -halfW - size.x * 0.5f - spawnOffscreenPadding;
        float rightX = halfW + size.x * 0.5f + spawnOffscreenPadding;
        float bottomY = -halfH - size.y * 0.5f - spawnOffscreenPadding;

        // ルール：左寄り→左 / 右寄り→右 / 真ん中→下
        float x = endPos.x;
        float threshold = halfW * 0.2f;

        if (x < -threshold) return new Vector2(leftX, endPos.y);
        if (x > threshold) return new Vector2(rightX, endPos.y);
        return new Vector2(endPos.x, bottomY);
    }

    private System.Collections.IEnumerator PlaySpawnIn(
        RectTransform moveRt,
        RectTransform rotatorRt,
        Vector2 startPos,
        Vector2 endPos)
    {
        float t = 0f;

        // 念のため null ガード（ここが null だと回転しない）
        if (rotatorRt != null) rotatorRt.localEulerAngles = Vector3.zero;

        while (t < spawnDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / spawnDuration);
            float e = spawnEase.Evaluate(u);

            // 移動
            moveRt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, e);

            // 回転（360度）
            if (rotatorRt != null)
            {
                float z = Mathf.LerpUnclamped(0f, 360f, e);
                rotatorRt.localEulerAngles = new Vector3(0f, 0f, z);
            }

            yield return null;
        }

        // 最終固定
        moveRt.anchoredPosition = endPos;
        if (rotatorRt != null) rotatorRt.localEulerAngles = Vector3.zero;
    }

    private SpawnEdge DecideEdge(Vector2 targetLocal, Rect rect)
    {
        float band = rect.width * centerBandRatio; // 中央帯の幅
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

        // 画面外（rootのRectの外側）へ
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

            default: // Bottom
                start.y = bottomY;
                start.x += Random.Range(-edgeJitterX, edgeJitterX);
                break;
        }

        return start;
    }


    private System.Collections.IEnumerator PlaySpawnIn(
    RectTransform rt,
    RectTransform rotator,
    Vector2 startPos,
    Vector2 endPos,
    SpawnEdge edge)
    {
        float t = 0f;

        // 回転開始角：右だけ 180、それ以外 -180
        float startAngle = (edge == SpawnEdge.Right) ? startAngleRight : startAngleOther;

        // 初期化
        rt.anchoredPosition = startPos;
        if (rotator != null) rotator.localEulerAngles = new Vector3(0, 0, startAngle);

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, spawnDuration);
            float u = Mathf.Clamp01(t);

            // 位置だけイーズ
            float posT = moveEase.Evaluate(u);
            rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, posT);

            // 回転はリニア
            if (rotator != null)
            {
                float ang = Mathf.Lerp(startAngle, 0f, u);
                rotator.localEulerAngles = new Vector3(0, 0, ang);
            }

            yield return null;
        }

        // 最終固定
        rt.anchoredPosition = endPos;
        if (rotator != null) rotator.localEulerAngles = Vector3.zero;
    }
    private System.Collections.IEnumerator EnableAfter(CapsuleItem item, float sec)
    {
        yield return new WaitForSeconds(sec);
        if (item != null) item.SetInteractable(true);
    }

    private IEnumerator StartStageFlow()
    {
        // 念のため最初は入力OFF
        EnableCapsuleInput(false);

        // ① ステージポップアップ
        if (popup != null && GameSession.I != null)
            popup.ShowStage(GameSession.I.CurrentStage);

        // ② フェードアウト完了まで待つ
        if (popup != null)
            yield return new WaitUntil(() => !popup.IsShowing);

        // ③ カプセルIN（この時点で初めて生成）
        SpawnCapsulesWithAnimation();

        // ★カプセルINが終わるまで待つ（spawnDuration を使う）
        yield return new WaitForSeconds(spawnDuration);

        // ④ GameStart ポップアップ
        if (popup != null)
            popup.ShowGameStart();

        if (popup != null)
            yield return new WaitUntil(() => !popup.IsShowing);

        // ⑤ ここで初めてゲーム操作可能
        EnableCapsuleInput(true);

        if (timerGroup != null) timerGroup.SetActive(true);
        StartTimer();
    }

    // SpawnCapsules を「演出込みで呼ぶ」ためのラッパー
    private void SpawnCapsulesWithAnimation()
    {
        SpawnCapsules(totalCount, hitCount);
    }

    // まとめて入力ON/OFF（popup中に押せない、などに使う）
    private void EnableCapsuleInput(bool enable)
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                spawned[i].SetInteractable(enable);
        }
    }

}
