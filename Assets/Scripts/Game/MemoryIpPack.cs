using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "GashaponMemory/MemoryIpPack", fileName = "IpPack_01")]
public class MemoryIpPack : ScriptableObject
{
    [Header("Branding")]
    [Tooltip("各パネル（MissionIntro / Memorize / GamePlay）のロゴ")]
    [SerializeField] private Sprite logoSprite;

    [Tooltip("各パネル（MissionIntro / Memorize / GamePlay）のコピーライト")]
    [SerializeField] private Sprite copyrightSprite;

    [Header("Sprites (index 0 is TARGET)")]
    public Sprite[] characterSprites;

    public Sprite LogoSprite => logoSprite;
    public Sprite CopyrightSprite => copyrightSprite;

    public Sprite TargetSprite => (characterSprites != null && characterSprites.Length > 0) ? characterSprites[0] : null;

    public Sprite GetSprite(int index)
    {
        if (characterSprites == null) return null;
        if (index < 0 || index >= characterSprites.Length) return null;
        return characterSprites[index];
    }

    public int Count => characterSprites == null ? 0 : characterSprites.Length;
}
