using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TransitionController : MonoBehaviour
{

    public static TransitionController I { get; private set; }
    [Header("Refs")]
    public CanvasGroup fadeCg;          // FadeOverlay の CanvasGroup
    public GameStateManager gsm;

    [Header("Config")]
    public float duration = 2f;

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
    }

    void ForceOff()
    {
        if (!fadeCg) return;
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;
        // ★FadeOverlayは常時Active運用（SetActiveしない）
    }

    void StartFadeThen(System.Action onSwitched)
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
        fadeCg.blocksRaycasts = true;
        fadeCg.interactable = false;

        if (audioSource && se) audioSource.PlayOneShot(se);

        // 0 -> 1 (duration秒)
        fadeCg.alpha = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeCg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        fadeCg.alpha = 1f;

        // 画面切替
        onSwitched?.Invoke();

        // 1 -> 0 (短めで戻す)
        float back = 0.25f;
        t = 0f;
        while (t < back)
        {
            t += Time.deltaTime;
            fadeCg.alpha = 1f - Mathf.Clamp01(t / back);
            yield return null;
        }

        // 解除
        fadeCg.alpha = 0f;
        fadeCg.blocksRaycasts = false;
        fadeCg.interactable = false;

        _running = false;
        _co = null;
    }

    public void StartTransitionToScene(string sceneName)
    {
        Debug.Log($"[Transition] LoadScene param = '{sceneName}'");

        StartFadeThen(() =>
        {
             SceneManager.LoadScene(sceneName);
        });
        Debug.Log("シーン遷移を実施します");
        Debug.Log($"[Transition] NOW LoadScene('{sceneName}')");
    }
}
