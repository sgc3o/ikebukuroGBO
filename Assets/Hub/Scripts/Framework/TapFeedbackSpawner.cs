using UnityEngine;

public class TapFeedbackSpawner : MonoBehaviour
{
    public UdpLogicalTouch touch;
    public RectTransform canvasRect;
    public TouchGate gate;

    [Header("FX")]
    public RectTransform fxPrefab;   // UIのPrefab（RectTransform）
    public Transform fxParent;       // Canvas配下
    public AudioSource audioSource;
    public AudioClip tapSe;

    private bool _prevDown = false;

    void Update()
    {
        if (touch == null || canvasRect == null || fxPrefab == null || fxParent == null) return;

        bool down = touch.IsDown;

        // Down瞬間だけ
        if (down && !_prevDown)
        {
            if (gate != null && !gate.IsAllowed(touch.LogicalPosition)) { _prevDown = down; return; }

            Vector2 anchored = LogicalToCanvasPos(touch.LogicalPosition, canvasRect);

            var fx = Instantiate(fxPrefab, fxParent);
            fx.anchoredPosition = anchored;

            if (audioSource != null && tapSe != null)
                audioSource.PlayOneShot(tapSe);
        }

        _prevDown = down;
    }

    private static Vector2 LogicalToCanvasPos(Vector2 logical, RectTransform canvasRect)
    {
        float lx = logical.x / 256f;
        float ly = logical.y / 256f;

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        float x = Mathf.Lerp(-w * 0.5f, w * 0.5f, lx);
        float y = Mathf.Lerp(-h * 0.5f, h * 0.5f, ly);
        return new Vector2(x, y);
    }
}