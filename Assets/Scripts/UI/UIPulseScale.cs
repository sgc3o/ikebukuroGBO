using UnityEngine;

[DisallowMultipleComponent]
public class UIPulseScale : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    [Header("Pulse Settings")]
    [SerializeField] private float pulseScale = 1.08f;   // ç≈ëÂägëÂó¶
    [SerializeField] private float pulsePeriod = 1.2f;   // 1âùïúéûä‘
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 _baseScale;

    void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
        _baseScale = target.localScale;
    }

    void OnEnable()
    {
        if (target != null)
            _baseScale = target.localScale;
    }

    void OnDisable()
    {
        if (target != null)
            target.localScale = _baseScale;
    }

    void Update()
    {
        if (target == null) return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;

        // sinîgÇ≈Ç”ÇÌÇ”ÇÌ
        float phase = time * (Mathf.PI * 2f) / Mathf.Max(0.01f, pulsePeriod);
        float wave01 = (Mathf.Sin(phase) + 1f) * 0.5f; // 0..1

        float scale = Mathf.Lerp(1f, pulseScale, wave01);
        target.localScale = _baseScale * scale;
    }
}