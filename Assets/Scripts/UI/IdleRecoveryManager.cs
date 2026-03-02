using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleRecoveryManager : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool enabledSystem = true;

    [Header("Idle -> Popup")]
    [SerializeField] private float idleToPopupSeconds = 90f;

    [Header("Popup -> Return To Hub")]
    [SerializeField] private float popupToReturnSeconds = 120f;

    [Header("Hub Scene")]
    [SerializeField] private string hubSceneName = "S_GashaponHub";

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Popup Prefab")]
    [SerializeField] private IdleRecoveryPopupController popupPrefab;

    // ★追加：抑制（Hub TitlePanelだけ想定の最小実装）
    [Header("Suppress Popup (Hub Title Only)")]
    [SerializeField] private bool suppressOnTitlePanel = true;

    // TitlePanelが SetActive 運用ならこれだけでOK
    [SerializeField] private GameObject titlePanelObject;

    // TitlePanelが CanvasGroup.alpha 運用ならこちら（あれば優先で判定）
    [SerializeField] private CanvasGroup titlePanelCanvasGroup;

    // CanvasGroup の表示判定用
    [SerializeField] private float titleVisibleAlphaThreshold = 0.01f;

    // ---- runtime ----
    private float idleTimer = 0f;

    private bool popupShowing = false;
    private float popupReturnRemaining = 0f;

    private IdleRecoveryPopupController popupInstance;

    public bool Enabled => enabledSystem;

    private void Start()
    {
        ResetIdleTimer();
    }

    private void Update()
    {
        if (!Enabled) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 入力があれば無操作タイマーをリセット
        if (DetectAnyUserInput())
        {
            ResetIdleTimer();
        }

        // ---- ポップアップ表示中：戻るカウントだけ進める（ここでのみReturn判定する） ----
        if (popupShowing)
        {
            popupReturnRemaining -= dt;

            if (popupInstance != null)
            {
                popupInstance.SetRemaining(popupToReturnSeconds, Mathf.Max(0f, popupReturnRemaining));
            }

            if (popupReturnRemaining <= 0f)
            {
                ReturnToHub();
            }

            return; // popup中は下の通常処理を走らせない
        }

        // ---- 通常：無操作カウント ----
        idleTimer += dt;
        if (idleTimer >= idleToPopupSeconds)
        {
            // ★追加：TitlePanel中は出さない（ただしタイマーはリセットして再判定）
            if (ShouldSuppressPopup())
            {
                ResetIdleTimer();
                return;
            }

            ShowPopup();
        }
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
    }

    // ★追加：抑制判定
    private bool ShouldSuppressPopup()
    {
        if (!suppressOnTitlePanel) return false;

        // CanvasGroupが指定されていればalphaで判定（透明Activeの罠を回避）
        if (titlePanelCanvasGroup != null)
        {
            if (!titlePanelCanvasGroup.gameObject.activeInHierarchy) return false;
            return titlePanelCanvasGroup.alpha > titleVisibleAlphaThreshold;
        }

        // GameObject指定ならActiveで判定（SetActive運用向け）
        if (titlePanelObject != null)
        {
            return titlePanelObject.activeInHierarchy;
        }

        // 指定が無いなら抑制しない
        return false;
    }

    private void ShowPopup()
    {
        if (popupShowing) return;
        if (popupPrefab == null)
        {
            Debug.LogError("IdleRecoveryManager: popupPrefab is not assigned.");
            return;
        }

        // ポップアップが出たタイミングで「戻るカウント」を新規スタート
        popupReturnRemaining = popupToReturnSeconds;

        popupInstance = Instantiate(popupPrefab, transform);
        popupInstance.Bind(this);

        popupInstance.StartReturnCountdownFromRemaining(popupToReturnSeconds, popupReturnRemaining);

        popupShowing = true;
    }

    // 「もどらない」→ ポップアップを閉じて通常に戻る（次の放置でまたポップアップを出す）
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
        // ここがポイント：Hubへ戻す判定はしない。次の放置でまたShowPopupされる。
    }

    // 「もどる」やタイムアップでHubへ
    public void ReturnToHub()
    {
        popupShowing = false;
        SceneManager.LoadScene(hubSceneName);
    }

    private bool DetectAnyUserInput()
    {
        // キー
        if (Input.anyKeyDown) return true;

        // マウス
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        // タッチ
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) return true;
            }
        }

        return false;
    }
}