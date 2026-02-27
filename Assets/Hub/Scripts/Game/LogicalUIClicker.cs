using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LogicalUiClicker : MonoBehaviour
{
    [Header("Input (Framework)")]
    public UdpLogicalTouch touch;             // 0..256 logical
    public RectTransform canvasRect;          // CanvasのRectTransform
    public GraphicRaycaster raycaster;        // Canvasに付いてる

    [Header("Options")]
    public float clickCooldownSec = 0.2f;
    public bool debugLog = true;

    private float _cooldown = 0f;
    private bool _prevDown = false;

    // Downしたときの対象を保持（重要）
    private Button _pressedButton = null;

    public TouchGate gate; // ←追加（InspectorでTouchSystemのTouchGateを入れる）

    void Awake()
    {
        if (raycaster == null && canvasRect != null)
            raycaster = canvasRect.GetComponentInParent<GraphicRaycaster>();
    }

    void Update()
    {
        if (touch == null || canvasRect == null || raycaster == null) return;

        _cooldown -= Time.deltaTime;

        if (gate != null && !gate.IsAllowed(touch.LogicalPosition)) return;

        bool down = touch.IsDown;

        // Downした瞬間：押したボタンを記録
        if (down && !_prevDown)
        {
            if (_cooldown <= 0f)
            {
                var btn = RaycastButtonAtLogical(touch.LogicalPosition);
                _pressedButton = btn;

                //if (debugLog) Debug.Log($"[Clicker] DOWN btn={(_pressedButton ? _pressedButton.name : "null")}");
            }
        }

        // Upした瞬間：同じボタンなら確定
        bool releasedThisFrame = !down && _prevDown;
        if (releasedThisFrame)
        {
            if (_cooldown > 0f)
            {
                _pressedButton = null;
            }
            else
            {
                var btnUp = RaycastButtonAtLogical(touch.LogicalPosition);

                if (debugLog) Debug.Log($"[Clicker] UP btnUp={(btnUp ? btnUp.name : "null")} pressed={(_pressedButton ? _pressedButton.name : "null")}");

                if (btnUp != null && btnUp == _pressedButton)
                {
                    btnUp.onClick.Invoke();
                    _cooldown = clickCooldownSec;

                    if (debugLog) Debug.Log($"[Clicker] CLICK {btnUp.name}");
                }

                _pressedButton = null;
            }
        }

        if (gate != null && !gate.IsAllowed(touch.LogicalPosition)) return;

        _prevDown = down;
    }

    private Button RaycastButtonAtLogical(Vector2 logical)
    {
        // logical(0..256) -> screenPos
        Vector2 canvasPos = LogicalToCanvasPos(logical);

        Vector3 worldPos = canvasRect.TransformPoint(canvasPos);
        var canvas = canvasRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null) ? canvas.worldCamera : null; // OverlayならnullでOK
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        var es = EventSystem.current;
        if (es == null) return null;

        var ped = new PointerEventData(es) { position = screenPos };
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        foreach (var r in results)
        {
            var btn = r.gameObject.GetComponentInParent<Button>();
            if (btn != null) return btn;
        }
        return null;
    }

    private Vector2 LogicalToCanvasPos(Vector2 logical)
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