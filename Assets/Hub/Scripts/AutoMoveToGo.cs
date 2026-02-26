using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoMoveToGo : MonoBehaviour
{
    [Header("Config")]
    [Min(0.1f)] public float idleSeconds = 5f;

    [Header("Target Scene (Inspector)")]
    [Tooltip("ビルド設定(Build Settings)に登録されているScene名を指定")]
    public string targetSceneName;

#if UNITY_EDITOR
    [Header("Editor Helper (optional)")]
    [Tooltip("Editor上でSceneをドラッグすると targetSceneName に反映されます（ビルドにはScene名が使われます）")]
    public UnityEditor.SceneAsset targetSceneAsset;
#endif

    [Header("Optional UI (2 objects)")]
    public GameObject showWhileCounting;   // 任意：待機中に表示
    public GameObject showOnTimeout;       // 任意：タイムアウト時に表示

    [Header("Refs")]
    public GameStateManager gsm; // 無ければ自動検索

    float _timer;
    bool _fired;

    void Awake()
    {
        if (!gsm) gsm = FindObjectOfType<GameStateManager>();
    }

    void OnEnable()
    {
        _timer = 0f;
        _fired = false;
        if (showWhileCounting) showWhileCounting.SetActive(true);
        if (showOnTimeout) showOnTimeout.SetActive(false);
    }

    void OnDisable()
    {
        if (showWhileCounting) showWhileCounting.SetActive(false);
        if (showOnTimeout) showOnTimeout.SetActive(false);
    }

    void Update()
    {
        if (_fired) return;

        // ✅「タップ/クリックだけ」でリセット
        if (IsTapOrClickThisFrame())
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= idleSeconds)
        {
            _fired = true;
            if (showOnTimeout) showOnTimeout.SetActive(true);

            // フェードしつつ、GoOpening() → Scene遷移
            if (TransitionController.I != null)
            {
                TransitionController.I.StartFadeThen(() =>
                {
                    if (gsm) gsm.GoTransition(); // MUST
                    LoadTargetScene();
                });
            }
            else
            {
                if (gsm) gsm.GoTransition(); // MUST
                LoadTargetScene();
            }
        }
    }

    bool IsTapOrClickThisFrame()
    {
        // マウス左クリック「押した瞬間」だけ
        if (Input.GetMouseButtonDown(0)) return true;

        // タッチ「開始」だけ
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) return true;
        }

        return false;
    }

    void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[InactivityAutoAdvanceToScene] targetSceneName が空です");
            return;
        }

        Debug.Log($"[InactivityAutoAdvanceToScene] LoadScene '{targetSceneName}'");
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (targetSceneAsset != null)
        {
            targetSceneName = targetSceneAsset.name;
        }
    }
#endif
}