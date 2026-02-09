using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Gashapon/Incentive Config", fileName = "IncentiveConfig")]
public class IncentiveConfig : ScriptableObject
{
    public string gameKey; // "Memory" / "Puzzle"

    [Header("Intro BG Video")]
    public VideoClip introBgClip;

    [Header("QR Popup")]
    public Sprite qrSprite;

    [Header("Confirm")]
    public int timeoutSeconds = 120;

    [Header("Return")]
    public string returnSceneName = "S_GashaponHub";
}
