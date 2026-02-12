using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PulseFrameUI : MonoBehaviour
{
    [Header("Scale")]
    public float baseScale = 1.0f;
    public float scaleAmp = 0.05f;     // 5%くらいが気持ちいい
    public float scalePeriod = 1.2f;   // 秒（ゆっくりめ）

    [Header("Alpha")]
    public bool alphaPulse = true;
    public float alphaMin = 0.35f;
    public float alphaMax = 1.0f;
    public float alphaPeriod = 1.2f;

    [Header("Extra wobble (optional)")]
    public float wobbleAmp = 0.01f;    // ほんの少しの不規則感
    public float wobbleSpeed = 1.7f;

    RectTransform rt;
    Graphic graphic; // Image / RawImage / Text など

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();
    }

    void OnEnable()
    {
        // 付け外ししても初期化されるように
        SetScale(baseScale);
        if (graphic) SetAlpha(alphaMax);
    }

    void Update()
    {
        float t = Time.unscaledTime; // UIは時間停止の影響受けにくい方が便利なこと多い

        // スケール：sinで“呼吸”っぽく
        float s = baseScale + Mathf.Sin(t * (Mathf.PI * 2f / scalePeriod)) * scaleAmp;

        // ちょい不規則を足す（なくてもOK）
        if (wobbleAmp > 0f)
            s += Mathf.Sin(t * wobbleSpeed) * wobbleAmp;

        SetScale(s);

        // アルファ：ふわっと点滅
        if (alphaPulse && graphic)
        {
            float a01 = (Mathf.Sin(t * (Mathf.PI * 2f / alphaPeriod)) + 1f) * 0.5f; // 0..1
            float a = Mathf.Lerp(alphaMin, alphaMax, a01);
            SetAlpha(a);
        }
    }

    void SetScale(float s)
    {
        rt.localScale = new Vector3(s, s, 1f);
    }

    void SetAlpha(float a)
    {
        var c = graphic.color;
        c.a = a;
        graphic.color = c;
    }
}
