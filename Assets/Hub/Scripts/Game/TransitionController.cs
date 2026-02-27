using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionController : MonoBehaviour
{
    public static TransitionController I { get; private set; }

    [Header("Refs")]
    public CanvasGroup fadeCg;          // FadeOverlay の CanvasGroup
    public GameStateManager gsm;        // UI遷移で使うなら残す（Scene遷移だけなら未使用でもOK）


    //
    //ここで全秒数設定
    [Header("Fade Config (Inspector)")]
    [Min(0.01f)] public float fadeOutSeconds = 0.5f; // 0→targetAlpha
    [Min(0.01f)] public float fadeInSeconds  = 0.5f; // targetAlpha→0
    [Min(0f)]    public float holdSeconds    = 1.0f;   // シーン遷移全体の時間
    [Range(0f, 1f)] public float targetAlpha = 1f;   // 暗転の濃さ（通常1）
    //
    //

    public bool useUnscaledTime = true;              // ポーズ中でも動かす

    [Header("Input Block")]
    public bool blockInputDuringFade = true;

    [Header("Optional SE")]
    public AudioSource audioSource;
    public AudioClip se;

    Coroutine _co;
    bool _running;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        ForceOff();
    }

    void ForceOff()
    {
        if (!fadeCg) return;
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;
        // ★FadeOverlayは常時Active運用（SetActiveしない）
    }

    /// <summary>
    /// フェードしてから任意処理を実行（UI遷移/Scene遷移どちらにも使える）
    /// </summary>
    public void StartFadeThen(System.Action onSwitched)
    {
        if (_running) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeRoutine(onSwitched));
    }

    IEnumerator FadeRoutine(System.Action onSwitched)
    {
        _running = true;

        if (!fadeCg)
        {
            Debug.LogError("[Transition] fadeCg が空欄です");
            _running = false;
            yield break;
        }

        // 入力遮断
        if (blockInputDuringFade)
        {
            fadeCg.blocksRaycasts = true;
            fadeCg.interactable = false;
        }

        if (audioSource && se) audioSource.PlayOneShot(se);

        // 0 -> targetAlpha（フェードアウト）
        yield return FadeAlpha(0f, targetAlpha, fadeOutSeconds);

        // 暗転保持（任意）
        if (holdSeconds > 0f)
            yield return WaitSeconds(holdSeconds);

        // 切替処理
        onSwitched?.Invoke();

        // ★これを入れる
        yield return null;

        // targetAlpha -> 0（フェードイン）
        yield return FadeAlpha(targetAlpha, 0f, fadeInSeconds);

        // 解除
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;

        _running = false;
        _co = null;
    }

    IEnumerator FadeAlpha(float from, float to, float sec)
    {
        if (sec <= 0f)
        {
            fadeCg.alpha = to;
            yield break;
        }

        float t = 0f;
        fadeCg.alpha = from;

        while (t < sec)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float a = Mathf.Clamp01(t / sec);
            fadeCg.alpha = Mathf.Lerp(from, to, a);
            yield return null;
        }

        fadeCg.alpha = to;
    }

    IEnumerator WaitSeconds(float sec)
    {
        if (sec <= 0f) yield break;

        float t = 0f;
        while (t < sec)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// フェード付きでScene遷移（禁止事項：SceneManager直呼びを各所でしない）
    /// </summary>
    public void StartTransitionToScene(string sceneName)
    {
        Debug.Log($"[Transition] LoadScene param = '{sceneName}'");

        StartFadeThen(() =>
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        });

        Debug.Log($"[Transition] StartTransitionToScene('{sceneName}') called");
    }
}