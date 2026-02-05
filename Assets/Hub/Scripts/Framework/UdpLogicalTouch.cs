using UnityEngine;

public class UdpLogicalTouch : MonoBehaviour
{
    [Header("Input")]
    public UdpTouchReceiver receiver;   // TouchSystemのUdpTouchReceiverをここに入れる

    [Header("Logical Area")]
    public Vector2 logicalSize = new Vector2(256f, 256f); // 0..256 を想定

    [Header("Output (read by clicker)")]
    public Vector2 LogicalPosition { get; private set; } = new Vector2(128f, 128f);
    public bool IsDown { get; private set; } = false;
    public int Phase { get; private set; } = 0; // 0=Up,1=Down,2=Move
    private int _upHoldFrames = 0;
    public int upHoldMinFrames = 2; // Upを最低2フレーム維持（調整OK）

[Header("Debug (Fake Input)")]
public bool useMouseDebug = false;   // ★これをONにするとマウス入力
public RectTransform debugCanvas;    // CanvasのRectTransform

    void Update()
    {
        if (receiver == null) return;

        Phase = receiver.phase;

        // Phaseから生down
        bool rawDown = (Phase == 1 || Phase == 2);

        // Upになったら保持カウンタ起動
        if (!rawDown)
        {
            _upHoldFrames = upHoldMinFrames;
        }

        // Up保持中は down を強制false
        if (_upHoldFrames > 0)
        {
            _upHoldFrames--;
            IsDown = false;
        }
        else
        {
            IsDown = rawDown;
        }

        // 0..1 -> 0..logicalSize
        var p01 = receiver.latest01;
        float x = Mathf.Clamp01(p01.x) * logicalSize.x;
        float y = Mathf.Clamp01(p01.y) * logicalSize.y;

        LogicalPosition = new Vector2(x, y);

        if (useMouseDebug)
        {
            UpdateFromMouse();
        }
    }

    void UpdateFromMouse()
    {
        if (debugCanvas == null) return;

        Vector2 mouseScreen = Input.mousePosition;

        // Canvas基準のローカル座標に変換
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            debugCanvas,
            mouseScreen,
            null,
            out localPos
        );

        // Canvasサイズ
        float w = debugCanvas.rect.width;
        float h = debugCanvas.rect.height;

        // local (-w/2..w/2) → 0..1
        float x01 = Mathf.InverseLerp(-w * 0.5f, w * 0.5f, localPos.x);
        float y01 = Mathf.InverseLerp(-h * 0.5f, h * 0.5f, localPos.y);

        // 0..256 logical に変換
        LogicalPosition = new Vector2(
            Mathf.Clamp01(x01) * 256f,
            Mathf.Clamp01(y01) * 256f
        );

        bool mouseDown = Input.GetMouseButton(0);
        bool mousePressed = Input.GetMouseButtonDown(0);
        bool mouseReleased = Input.GetMouseButtonUp(0);

        // phase を擬似生成
        if (mousePressed)
            Phase = 1;        // Down
        else if (mouseDown)
            Phase = 2;        // Move
        else if (mouseReleased)
            Phase = 0;        // Up
        else
            Phase = -1;

        IsDown = mouseDown;
    }

}
