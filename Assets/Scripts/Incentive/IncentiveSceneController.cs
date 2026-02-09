using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IncentiveSceneController_Single : MonoBehaviour, IReturnConfirmOwner
{
    [Header("Config (このシーン専用を1つ刺す)")]
    [SerializeField] private IncentiveConfig config;

    [Header("Intro")]
    [SerializeField] private GameObject introRoot;
    [SerializeField] private VideoPlayer introVideoPlayer;
    [SerializeField] private float introFallbackSec = 1.0f;

    [Header("QR Popup")]
    [SerializeField] private CanvasGroup qrPopupCg;
    [SerializeField] private Image qrImage;
    [SerializeField] private Button btnBackToStart;

    [Header("Return Confirm Popup (Incentive専用)")]
    [SerializeField] private IncentiveReturnConfirmPopupController confirmPopup;

    [Header("Optional input block during intro")]
    [SerializeField] private CanvasGroup raycastBlockerCg;

    [Header("ポップアップ中のIdleRecovery機能させない")]
    [SerializeField] private IdleRecoveryIncentiveGuard idleGuard;


    private void Awake()
    {
        ShowCg(qrPopupCg, false);
        if (raycastBlockerCg != null) ShowCg(raycastBlockerCg, false);
        if (confirmPopup != null) confirmPopup.HideImmediate();
    }

    private void Start()
    {
        ApplyConfig();

        if (btnBackToStart != null)
        {
            btnBackToStart.onClick.RemoveAllListeners();
            btnBackToStart.onClick.AddListener(ShowConfirm);
        }

        StartCoroutine(RunFlow());
    }

    private void ApplyConfig()
    {
        if (config == null) return;

        if (qrImage != null) qrImage.sprite = config.qrSprite;
        // VideoClipは再生時にセットする
    }

    private IEnumerator RunFlow()
    {
        if (raycastBlockerCg != null) ShowCg(raycastBlockerCg, true);

        if (introRoot != null) introRoot.SetActive(true);
        yield return PlayIntroVideo();

        if (introRoot != null) introRoot.SetActive(false);

        if (raycastBlockerCg != null) ShowCg(raycastBlockerCg, false);

        ShowCg(qrPopupCg, true);
    }

    private IEnumerator PlayIntroVideo()
    {
        if (introVideoPlayer == null || config == null || config.introBgClip == null)
        {
            yield return new WaitForSeconds(introFallbackSec);
            yield break;
        }

        introVideoPlayer.Stop();
        introVideoPlayer.clip = config.introBgClip;
        introVideoPlayer.isLooping = false;
        introVideoPlayer.playOnAwake = false;
        introVideoPlayer.waitForFirstFrame = true;

        introVideoPlayer.Prepare();
        while (!introVideoPlayer.isPrepared) yield return null;

        introVideoPlayer.Play();

        float length = (float)introVideoPlayer.length;
        if (length <= 0.01f) length = introFallbackSec;

        yield return new WaitForSeconds(length);
    }
    private void ShowConfirm()
    {
        idleGuard?.Suspend();
        // QRは表示のまま（見せたい）
        if (qrPopupCg != null)
        {
            if (!qrPopupCg.gameObject.activeSelf) qrPopupCg.gameObject.SetActive(true);
            qrPopupCg.alpha = 1f;
            qrPopupCg.interactable = false;
            qrPopupCg.blocksRaycasts = false; // ←押せなくする
        }

        int sec = (config != null) ? config.timeoutSeconds : 120;

        if (confirmPopup != null)
        {
            if (!confirmPopup.gameObject.activeSelf) confirmPopup.gameObject.SetActive(true);
            confirmPopup.Show(this, sec);
        }
        confirmPopup.Show(this, sec);
    }


    // IReturnConfirmOwner
    public void ReturnToHub()
    {
        idleGuard?.Resume();
        string scene = (config != null && !string.IsNullOrEmpty(config.returnSceneName))
            ? config.returnSceneName
            : "S_GashaponHub";

        SceneTransition.Go(scene);
    }

    public void ClosePopupAndResume()
    {
        idleGuard?.Resume();
        if (qrPopupCg != null)
        {
            qrPopupCg.interactable = true;
            qrPopupCg.blocksRaycasts = true;
        }
    }



    private static void ShowCg(CanvasGroup cg, bool show)
    {
        if (cg == null) return;
        if (!cg.gameObject.activeSelf) cg.gameObject.SetActive(true);

        cg.alpha = show ? 1f : 0f;
        cg.blocksRaycasts = show;
        cg.interactable = show;
    }
}
