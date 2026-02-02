using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleRecoveryManager : MonoBehaviour
{
    [Header("Target (Hub)")]
    [SerializeField] private string hubSceneName = "S_GashaponHub";

    [Header("Timers (seconds)")]
    [SerializeField] private float idleToPopupSeconds = 90f;
    [SerializeField] private float popupToReturnSeconds = 120f;

    [Header("Popup Prefab")]
    [SerializeField] private IdleRecoveryPopupController popupPrefab;

    [Header("Behavior")]
    [SerializeField] private bool useUnscaledTime = true; // Time.timeScale の影響を受けない

    // 状態
    private float idleTimer;
    private bool popupShowing;

    private IdleRecoveryPopupController popupInstance;

    private bool returnCountdownActive;
    private float returnDeadlineUnscaled;


    /// <summary> 外部から一時停止したい場合用（Intro中は止めたい等） </summary>
    public bool Enabled { get; set; } = true;

    private void Awake()
    {
        // Hubには付けない前提だけど、念のため保険
        if (SceneManager.GetActiveScene().name == hubSceneName)
        {
            Enabled = false;
        }
    }

    private void Start()
    {
        ResetIdleTimer();
    }

    private void Update()
    {
        if (!Enabled) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 入力検知（タッチ/クリック/キーのいずれか）
        if (DetectAnyUserInput())
        {
            // 通常時：無操作タイマーリセット
            ResetIdleTimer();

            // ポップアップ表示中：戻りカウントをリセット（ポップアップは消さない）
            if (DetectAnyUserInput())
            {
                // ポップアップが出てない時だけ idle 判定をリセット
                if (!popupShowing)
                {
                    ResetIdleTimer();
                }
                // popupShowing中は "戻りカウント" は継続させるので何もしない
            }


        }

        if (!popupShowing)
        {
            idleTimer += dt;
            if (idleTimer >= idleToPopupSeconds)
            {
                ShowPopup();
            }
        }
        else
        {
            float remaining = returnDeadlineUnscaled - Time.unscaledTime;
            if (remaining <= 0f)
            {
                ReturnToHub();
                return;
            }

            // 表示だけ更新（Timeはdeadlineで進む）
            if (popupInstance != null)
                popupInstance.SetRemaining(popupToReturnSeconds, remaining);
        }

    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
    }

    private void ShowPopup()
    {
        if (popupShowing) return;
        if (popupPrefab == null) { Debug.LogError("popupPrefab none"); return; }

        // ★初回だけ締切を作る（継続の核）
        if (!returnCountdownActive)
        {
            returnCountdownActive = true;
            returnDeadlineUnscaled = Time.unscaledTime + popupToReturnSeconds;
        }

        popupInstance = Instantiate(popupPrefab, transform);
        popupInstance.Bind(this);

        // ★残り秒数でRadial開始位置を復元
        float remaining = Mathf.Max(0f, returnDeadlineUnscaled - Time.unscaledTime);
        popupInstance.StartReturnCountdownFromRemaining(popupToReturnSeconds, remaining);

        popupShowing = true;
    }


    public void ClosePopupAndResume()
    {
        if (!popupShowing) return;

        if (popupInstance != null)
        {
            Destroy(popupInstance.gameObject);
            popupInstance = null;
        }

        popupShowing = false;
        ResetIdleTimer();
    }

    public void ReturnToHub()
    {
        // 念のため
        popupShowing = false;

        SceneManager.LoadScene(hubSceneName);
    }

    private bool DetectAnyUserInput()
    {
        // New Input Systemが入っていても、まずはレガシーInputでカバーする。
        // （現場ではこれが一番壊れにくい）
        if (Input.anyKeyDown) return true;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        if (Input.touchCount > 0)
        {
            // 触れた瞬間だけで良いなら Began でOK
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) return true;
            }
        }

        return false;
    }
}
