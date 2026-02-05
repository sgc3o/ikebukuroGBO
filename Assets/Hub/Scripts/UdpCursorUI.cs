using UnityEngine;
using UnityEngine.UI;

public class UdpCursorUi : MonoBehaviour
{
    public UdpTouchReceiver receiver;
    public RectTransform cursor;   // Canvas上のImageのRectTransform
    public RectTransform canvasRect;

    [Header("Options")]
    public bool flipY = false; // 必要ならON（Unity側で上が+にしたい等）

    void Update()
    {
        if (receiver == null || cursor == null || canvasRect == null) return;

        var p01 = receiver.latest01;

         // 必要なら反転
        if (flipY) p01.y = 1f - p01.y;

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        // 0..1 → Canvas中心基準(-w/2..+w/2, -h/2..+h/2)
        float x = Mathf.Lerp(-w * 0.5f, w * 0.5f, p01.x);
        float y = Mathf.Lerp(-h * 0.5f, h * 0.5f, p01.y);

        // ★安全のため画面内にクランプ（カーソル半径ぶん余白）
        float pad = 20f;
        x = Mathf.Clamp(x, -w * 0.5f + pad, w * 0.5f - pad);
        y = Mathf.Clamp(y, -h * 0.5f + pad, h * 0.5f - pad);

        cursor.anchoredPosition = new Vector2(x, y);
        cursor.localScale = receiver.isDown ? Vector3.one * 1.4f : Vector3.one;

        //後で消す
        //if (Time.frameCount % 30 == 0) // 0.5秒に1回くらい
        //Debug.Log($"p01={receiver.latest01} pos={cursor.anchoredPosition} down={receiver.isDown}");

    }
}
