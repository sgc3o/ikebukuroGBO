using UnityEngine;
using UnityEngine.UI;

public class CircularCountdownUI : MonoBehaviour
{
    [SerializeField] private Image radialImage;

    private float duration = 1f;
    private float remaining = 1f;
    private bool running;

    public void StartCountdown(float totalSeconds)
    {
        duration = Mathf.Max(0.01f, totalSeconds);
        remaining = duration;
        running = true;

        SetFill(0f); // ★空から始める
    }

    // ★途中から再開したい用（totalは固定、remainingだけ差し込む）
    public void StartCountdownFromRemaining(float totalSeconds, float remainingSeconds)
    {
        duration = Mathf.Max(0.01f, totalSeconds);
        remaining = Mathf.Clamp(remainingSeconds, 0f, duration);
        running = true;

        SetFill(Progress01()); // ★残りに応じた位置から表示
    }

    public bool Tick(float dt)
    {
        if (!running) return false;

        remaining -= dt;
        if (remaining < 0f) remaining = 0f;

        SetFill(Progress01()); // ★0→1 で埋まる

        if (remaining <= 0f)
        {
            running = false;
            return true;
        }
        return false;
    }

    private float Progress01()
    {
        // 0→1（空→満）
        return 1f - (remaining / duration);
    }

    private void SetFill(float v)
    {
        if (radialImage == null) return;
        radialImage.fillAmount = Mathf.Clamp01(v);
    }
}
