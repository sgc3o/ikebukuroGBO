using UnityEngine;

public class TouchGate : MonoBehaviour
{
    [Range(0f, 1f)]
    public float activeMinY01 = 0.5f; // 下半分だけ有効なら 0.5

    public bool debugLog = false;

    public bool IsAllowed(Vector2 logicalPos, float logicalMax = 256f)
    {
        float y01 = logicalPos.y / logicalMax;
        bool ok = (y01 >= activeMinY01);
        if (debugLog && Time.frameCount % 60 == 0)
            Debug.Log($"[TouchGate] y01={y01:F2} min={activeMinY01:F2} ok={ok}");
        return ok;
    }
}