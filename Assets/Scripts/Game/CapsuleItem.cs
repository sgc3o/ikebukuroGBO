using UnityEngine;
using UnityEngine.UI;

// カプセル1個の挙動
public class CapsuleItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Button button;

    [Header("Visuals (GameObject参照)")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject hitVisual;
    [SerializeField] private GameObject missVisual;

    // --- 状態 ---
    private bool isHit;
    private bool opened;
    private StageManager manager;

    // 当たり画像（Hitのときだけ使う）
    private Sprite hitSprite;

    /// <summary>
    /// 外から「このカプセルは当たりか？」と「当たり画像」を注入して初期化
    /// </summary>
    public void Setup(StageManager managerRef, bool hitFlag, Sprite hitSpriteIfAny)
    {
        manager = managerRef;
        isHit = hitFlag;
        hitSprite = hitSpriteIfAny;
        opened = false;

        // 初期表示：閉じだけON
        if (closedVisual != null) closedVisual.SetActive(true);
        if (hitVisual != null) hitVisual.SetActive(false);
        if (missVisual != null) missVisual.SetActive(false);

        // クリック登録
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickCapsule);

        button.interactable = true;
    }

    private void OnClickCapsule()
    {
        if (opened) return;
        opened = true;

        button.interactable = false;

        if (closedVisual != null) closedVisual.SetActive(false);

        if (isHit)
        {
            // HitVisual の Image の sprite を差し替え
            if (hitVisual != null)
            {
                var img = hitVisual.GetComponent<Image>();
                if (img != null && hitSprite != null)
                {
                    img.sprite = hitSprite;
                    img.SetNativeSize(); // 必要ならON（サイズ揃えたいなら消してOK）
                }
                hitVisual.SetActive(true);
            }

            manager.OnHitFound();
        }
        else
        {
            if (missVisual != null) missVisual.SetActive(true);
        }
    }
}
