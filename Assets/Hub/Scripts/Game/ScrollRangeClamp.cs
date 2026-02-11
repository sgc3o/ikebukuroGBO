using UnityEngine;
using UnityEngine.UI;

public class ScrollRangeClamp : MonoBehaviour
{
    [Header("Refs")]
    public ScrollRect scrollRect;          // 対象のScrollRect
    public Scrollbar scrollbar;            // 任意（入れなくても動く）

    [Header("Pages")]
    [Min(1)] public int totalPages = 4;    // 全カード数（将来増える前提の総数）
    [Min(1)] public int allowedPages = 2;  // 今「見せたい/行ける」ページ数
    [Min(0)] public int startPage = 0;     // 開始ページ（0=左端）

    [Header("Clamp")]
    [Range(0f, 0.49f)] public float padding01 = 0f; // 端に余白を作りたい時(0..0.49)

    void Reset()
    {
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
        if (!scrollbar) scrollbar = GetComponent<Scrollbar>();
    }

    void Awake()
    {
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollbar) scrollbar.onValueChanged.AddListener(_ => ClampNow());
    }

    void OnEnable()
    {
        SetStart();
        ClampNow();
    }

    void LateUpdate()
    {
        // スワイプ/ドラッグ/他スクリプト更新の後に確実に抑える
        ClampNow();
    }

    void SetStart()
    {
        if (!scrollRect) return;

        float v = PageToNorm(startPage);
        scrollRect.horizontalNormalizedPosition = v;

        if (scrollbar) scrollbar.value = v;
    }

    void ClampNow()
    {
        if (!scrollRect) return;
        if (totalPages <= 1) return;

        int ap = Mathf.Clamp(allowedPages, 1, totalPages);

        float min = 0f + padding01;
        float max = PageToNorm(ap - 1) - padding01;
        if (max < min) max = min;

        float v = scrollRect.horizontalNormalizedPosition;
        float c = Mathf.Clamp(v, min, max);

        if (!Mathf.Approximately(v, c))
        {
            scrollRect.horizontalNormalizedPosition = c;
            if (scrollbar) scrollbar.value = c;
        }
    }

    float PageToNorm(int pageIndex)
    {
        if (totalPages <= 1) return 0f;
        int idx = Mathf.Clamp(pageIndex, 0, totalPages - 1);
        return (float)idx / (totalPages - 1);
    }
}
