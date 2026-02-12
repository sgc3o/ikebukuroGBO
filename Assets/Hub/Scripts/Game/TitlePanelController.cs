using UnityEngine;
using UnityEngine.UI;

public class TitleStartController : MonoBehaviour
{
    [Header("Refs")]
    public GameStateManager gsm;                 // GameSystem を刺す
    public TransitionController transition;      // 空でもOK（I が使えるなら）
    public Button startBtn;                     // StartBtn の Button を刺す
    public Image startBtnImage;                 // StartBtn の Image を刺す

    [Header("Sprites")]
    public Sprite spriteOff;                    // 通常
    public Sprite spriteOn;                     // 1回目クリック後

    bool armed = false;

    void Awake()
    {
        if (startBtn == null) startBtn = GetComponentInChildren<Button>(true);
        if (startBtnImage == null && startBtn != null) startBtnImage = startBtn.GetComponent<Image>();

        if (startBtn != null)
        {
            startBtn.onClick.RemoveListener(OnStartPressed);
            startBtn.onClick.AddListener(OnStartPressed);
        }

        // 初期表示
        if (startBtnImage && spriteOff) startBtnImage.sprite = spriteOff;
        armed = false;
    }

    public void OnStartPressed()
    {
        // 1回目：点灯（画像切替）だけ
        if (!armed)
        {
            armed = true;
            if (startBtnImage && spriteOn) startBtnImage.sprite = spriteOn;
            Debug.Log("[TitleStart] armed=true");
            return;
        }

        // 2回目：フェードして GameSelectPanel へ
        var tr = transition != null ? transition : TransitionController.I;
        if (tr == null)
        {
            Debug.LogError("[TitleStart] transition is null (InspectorにもIにも無い)");
            // 最悪フェード無しで遷移
            if (gsm) gsm.GoGameSelect();
            return;
        }

        if (gsm == null)
        {
            Debug.LogError("[TitleStart] gsm is null");
            return;
        }

        tr.StartFadeThen(() =>
        {
            gsm.GoGameSelect();
            // 必要ならここで armed を戻す（Titleに戻ったときはGoTitle側で戻すのが理想）
        });

        Debug.Log("[TitleStart] Fade -> GoGameSelect");
    }
}
