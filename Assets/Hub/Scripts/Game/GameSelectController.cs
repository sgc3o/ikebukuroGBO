using System.Collections;
using System.Collections.Generic;   // ←あってもOK
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class GameSelectController : MonoBehaviour, IEndDragHandler
{
    [Header("Refs")]
    public GameStateManager gsm;
    public RectTransform viewport;   // 576x576
    public RectTransform content;    // カード4枚の親

    [Header("Config")]
    public int gameCount = 4;
    public float swipeThresholdPx = 80f; // マウスDragの閾値

    [Header("Confirm Frame")]
    public GameObject[] frames; // size=4（FrameCard0/1/2/3）
    int selectedIndex = -1;

    [Header("Confirm UI")]
    public GameObject confirmPanel;          // ConfirmPanelを入れる
    public CanvasGroup confirmCanvasGroup;   // ConfirmPanelのCanvasGroupを入れる
    public float confirmFadeSec = 2.0f;

    bool _confirming = false;

    [Header("UI Parts")]
    public GameObject scrollbarHorizontal; // Scrollbar Horizontal を入れる
    public UnityEngine.UI.ScrollRect scrollRect;
    public float snapSpeed = 12f;
    bool _snapping;
    Vector2 _snapTarget;
    int currentIndex = 0;

    float _dragStartX;
    bool _dragging;
    float _lastTapTime;
    const float ConfirmWindowSec = 2.0f; // 2秒以内の2回目で確定

    void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponentInChildren<UnityEngine.UI.ScrollRect>();
    }
    
   void OnEnable()
    {
        // ★Confirm中に勝手にリセットしない
        if (!_confirming)
        {
            if (confirmPanel) confirmPanel.SetActive(false);

            if (frames != null)
            {
                for (int i = 0; i < frames.Length; i++)
                    if (frames[i]) frames[i].SetActive(false);
            }

            selectedIndex = -1;
        }

        if (scrollbarHorizontal) scrollbarHorizontal.SetActive(true);
        if (scrollRect) scrollRect.enabled = true;

        if (!scrollRect) return;

        Canvas.ForceUpdateCanvases();
        scrollRect.horizontalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();

        UpdateHighlight();
    }



    void Update()
    { 
        if (_snapping)
        {
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, _snapTarget, Time.deltaTime * snapSpeed);
            if (Vector2.Distance(content.anchoredPosition, _snapTarget) < 0.5f)
            {
                content.anchoredPosition = _snapTarget;
                _snapping = false;
            }
        }
    }

    void SnapToNearest()
    {
        float w = viewport.rect.width;
        float x = content.anchoredPosition.x;
        int idx = Mathf.RoundToInt(-x / w);
        currentIndex = Mathf.Clamp(idx, 0, gameCount - 1);

        _snapTarget = new Vector2(-currentIndex * w, content.anchoredPosition.y);
        _snapping = true;

        // 表示が変わったら選択解除
        selectedIndex = -1;
        UpdateHighlight();
    }

    public void MoveLeft()
    {
        SetIndex(currentIndex - 1);
    }

    public void MoveRight()
    {
        SetIndex(currentIndex + 1);
    }

    void SetIndex(int idx)
    {
        currentIndex = Mathf.Clamp(idx, 0, gameCount - 1);
        SnapTo(currentIndex);
        // ここで「表示が変わったら選択解除」にしたいなら↓
        selectedIndex = -1;
        UpdateHighlight();
    }

    void SnapTo(int idx)
    {
        float w = viewport.rect.width;   // 576想定
        Vector2 p = content.anchoredPosition;
        p.x = -idx * w;
        content.anchoredPosition = p;
    }

     // Cardクリック（ScrollView内のCardボタンから呼ぶ）
    public void OnCardTapped(int idx)
    {
        selectedIndex = idx;

        if (confirmPanel) confirmPanel.SetActive(true); // ConfirmPanelを出す（使ってるなら）
        if (scrollbarHorizontal) scrollbarHorizontal.SetActive(false);

        // FrameCardは1つだけ表示
        for (int i = 0; i < frames.Length; i++)
            if (frames[i]) frames[i].SetActive(i == idx);

        Debug.Log($"[Select] idx={idx} -> show FrameCard");
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        float w = viewport.rect.width;
        float x = -content.anchoredPosition.x;   // 右へ行くほど正
        int idx = Mathf.RoundToInt(x / w);
        SetIndex(idx); // SnapTo +（必要なら）選択解除

        SnapToNearest();
    }

    void UpdateHighlight()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            var card = content.GetChild(i);
            var frame = card.Find("Frame");
            if (frame != null)
                frame.gameObject.SetActive(i == selectedIndex);

            // ついでに軽い強調
            float s = (i == selectedIndex) ? 1.03f : 1.0f;
            card.localScale = new Vector3(s, s, 1f);
        }
    }

     // ★ FrameCard側のButton OnClickから呼ぶ（idx不要）
    // ★ FrameCard側のButton OnClickから呼ぶ（idx不要）
    public void OnFrameConfirm()
    {
        _confirming = true; // ★追加：OnEnableでリセットされないため

        if (confirmCanvasGroup)
        {
            confirmCanvasGroup.interactable = false;
            confirmCanvasGroup.blocksRaycasts = false;
        }

        if (scrollRect) scrollRect.enabled = false;

        Debug.Log($"[GameSelect] OnFrameConfirm CurrentIndex={currentIndex} gsm={(gsm ? "OK" : "null")}");

        gsm.selectedGameIndex = (selectedIndex >= 0) ? selectedIndex : currentIndex;

        gsm.GoTransition();
    }
}