using UnityEngine;
using UnityEngine.UI;

public class DebugIncentiveJumpButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button openConfirmButton;
    [SerializeField] private DebugIncentiveJumpConfirmPopup confirmPopup;

    [Header("Scene")]
    [SerializeField] private string targetIncentiveSceneName = "";

    [Header("Display Control")]
    [SerializeField] private bool showOnlyInEditorOrDevelopmentBuild = true;
    [SerializeField] private bool forceShow = false;

    [Header("Optional")]
    [SerializeField] private CanvasGroup gameplayBlockTarget;

    private bool isPopupOpen = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        ApplyVisibility();

        if (openConfirmButton != null)
        {
            openConfirmButton.onClick.RemoveAllListeners();
            openConfirmButton.onClick.AddListener(OnClickOpenConfirm);
        }

        if (confirmPopup != null)
        {
            confirmPopup.HideImmediate();
        }
    }

    private void ApplyVisibility()
    {
        bool shouldShow = forceShow;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showOnlyInEditorOrDevelopmentBuild)
        {
            shouldShow = true;
        }
#endif

        if (root != null)
        {
            root.SetActive(shouldShow);
        }
        else
        {
            gameObject.SetActive(shouldShow);
        }
    }

    private void OnClickOpenConfirm()
    {
        if (isTransitioning) return;
        if (isPopupOpen) return;
        if (confirmPopup == null) return;

        isPopupOpen = true;

        SetGameplayInteractable(false);

        confirmPopup.Show(
            onConfirm: OnConfirmJump,
            onCancel: OnCancelJump
        );
    }

    private void OnConfirmJump()
    {
        if (isTransitioning) return;

        isPopupOpen = false;
        isTransitioning = true;

        if (string.IsNullOrWhiteSpace(targetIncentiveSceneName))
        {
            Debug.LogError("[DebugIncentiveJumpButton] targetIncentiveSceneName is empty.");
            isTransitioning = false;
            SetGameplayInteractable(true);
            return;
        }

        SceneTransition.Go(targetIncentiveSceneName);
    }

    private void OnCancelJump()
    {
        isPopupOpen = false;
        SetGameplayInteractable(true);
    }

    private void SetGameplayInteractable(bool interactable)
    {
        if (gameplayBlockTarget == null) return;

        gameplayBlockTarget.interactable = interactable;
        gameplayBlockTarget.blocksRaycasts = interactable;
    }
}