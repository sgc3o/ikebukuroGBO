using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition I { get; private set; }

    [Header("UI Overlay (DontDestroy)")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private CanvasGroup group;     // 全体の表示/入力ブロック
    [SerializeField] private Image fadeImage;       // フェード用（真っ黒画像でOK）

    [Header("Barrier/Wipe (Optional)")]
    [SerializeField] private Image barrierImage;    // バリア用（Shader付きMaterial想定）
    [SerializeField] private string barrierProp = "_Cutoff"; // Shader側のプロパティ名
    [SerializeField] private AnimationCurve barrierCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Timing")]
    [SerializeField] private float outDuration = 0.25f;
    [SerializeField] private float inDuration = 0.25f;

    public enum Mode { Fade, Barrier }
    [SerializeField] private Mode mode = Mode.Fade;

    bool busy;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // 初期状態：非表示
        if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(true);
        SetGroupVisible(false);
    }

    void SetGroupVisible(bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    // どこからでも呼べる入口（あなたが今使ってる Go(...) の置換先）
    public static void Go(string sceneName)
    {
        if (I == null)
        {
            Debug.LogError("[SceneTransition] SceneTransition がシーン上に存在しません。");
            SceneManager.LoadScene(sceneName);
            return;
        }
        I.StartCoroutine(I.CoGo(sceneName));
    }

    IEnumerator CoGo(string sceneName)
    {
        if (busy) yield break;
        busy = true;

        // オーバーレイON（ここで画面を確実に握る）
        if (transitionCanvas != null) transitionCanvas.sortingOrder = 999;
        SetGroupVisible(true);

        // OUT演出（旧画面を覆う）
        yield return PlayOut();

        // 非同期ロード
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        // 0.9到達待ち（ここで演出中にロードが進む）
        while (op.progress < 0.9f)
            yield return null;

        // 切替（ここは覆ってるので黒は基本見えない）
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 1フレームだけ待つ（新シーンのUI初期化が走る猶予）
        yield return null;

        // IN演出（新画面を見せる）
        yield return PlayIn();

        SetGroupVisible(false);
        busy = false;
    }

    IEnumerator PlayOut()
    {
        if (mode == Mode.Fade)
        {
            // フェード画像を使って 0→1
            if (fadeImage != null) fadeImage.gameObject.SetActive(true);
            if (barrierImage != null) barrierImage.gameObject.SetActive(false);

            yield return FadeCanvas(0f, 1f, outDuration);
        }
        else
        {
            // バリア：Cutoff 0→1（Shader次第で意味が逆の場合あり）
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            if (barrierImage != null) barrierImage.gameObject.SetActive(true);

            yield return Barrier(0f, 1f, outDuration);
        }
    }

    IEnumerator PlayIn()
    {
        if (mode == Mode.Fade)
        {
            yield return FadeCanvas(1f, 0f, inDuration);
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
        }
        else
        {
            yield return Barrier(1f, 0f, inDuration);
            if (barrierImage != null) barrierImage.gameObject.SetActive(false);
        }
    }

    IEnumerator FadeCanvas(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, dur <= 0f ? 1f : (t / dur));
            if (fadeImage != null)
            {
                var c = fadeImage.color;
                c.a = a;
                fadeImage.color = c;
            }
            yield return null;
        }
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = to;
            fadeImage.color = c;
        }
    }

    IEnumerator Barrier(float from, float to, float dur)
    {
        if (barrierImage == null || barrierImage.material == null)
        {
            // バリア未設定ならフェードにフォールバック
            yield return FadeCanvas(from, to, dur);
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float x = dur <= 0f ? 1f : (t / dur);
            float eased = barrierCurve.Evaluate(x);
            float v = Mathf.Lerp(from, to, eased);
            barrierImage.material.SetFloat(barrierProp, v);
            yield return null;
        }
        barrierImage.material.SetFloat(barrierProp, to);
    }
}
