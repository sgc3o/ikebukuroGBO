using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QrResetConfirmPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action onConfirm;

    private void Awake()
    {
        HideImmediate();

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Hide);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
    }

    public void Show(string gameName, System.Action confirmAction)
    {
        onConfirm = confirmAction;

        if (messageText != null)
        {
            messageText.text =
                $"<b><{gameName}></b>\n" +
                "本当にリセットしますか？\n" +
                "・次回表示されるQR番号は 1 に戻ります\n" +
                "・本日のカウントはリセットされます\n" +
                "・この操作は記録に残ります";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    private void OnClickConfirm()
    {
        onConfirm?.Invoke();
        Hide();
    }

    public void Hide()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void HideImmediate()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}