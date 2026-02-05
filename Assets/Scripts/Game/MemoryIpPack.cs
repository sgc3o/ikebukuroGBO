using UnityEngine;

[CreateAssetMenu(menuName = "GashaponMemory/MemoryIpPack", fileName = "IpPack_01")]
public class MemoryIpPack : ScriptableObject
{
    [Header("Sprites (index 0 is TARGET)")]
    public Sprite[] characterSprites;

    public Sprite TargetSprite => (characterSprites != null && characterSprites.Length > 0) ? characterSprites[0] : null;

    public Sprite GetSprite(int index)
    {
        if (characterSprites == null) return null;
        if (index < 0 || index >= characterSprites.Length) return null;
        return characterSprites[index];
    }

    public int Count => characterSprites == null ? 0 : characterSprites.Length;
}
