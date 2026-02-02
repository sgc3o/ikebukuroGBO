using UnityEngine;
using UnityEngine.UI;

// カプセル1個の挙動
public class CapsuleItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Button button;

    [Header("Visuals (GameObject参照)")]
    [SerializeField] private RectTransform rotator; // Button/Rotator を入れる
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject hitVisual;
    [SerializeField] private GameObject missVisual;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Animator hitEffectAnimator; // HitEffect に付いてる Animator
    [SerializeField] private string hitEffectStateName = "HitEffect"; // Animator内のState名




    // --- 状態 ---
    private bool isHit;
    private bool opened;
    private StageManager manager;
    public RectTransform Rotator => rotator;

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

        //ヒットエフェクトを最初は消す
        if (hitEffect != null) hitEffect.SetActive(false);


        button.interactable = true;
    }

    private void OnClickCapsule()
    {

        // 押されたカプセルを最前面へ（UIの重なり順を上げる）
        transform.SetAsLastSibling();

        if (manager != null && manager.IsInputBlocked) return; // ★追加：ポップアップ中は無視

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
                PlayHitEffect();
                hitVisual.SetActive(true);
            }

            manager.OnHitFound();
        }
        else
        {
            if (missVisual != null) missVisual.SetActive(true);
        }

    }

    public void SetInteractable(bool value)
    {
        if (button == null) return;

        // すでに開いてるカプセルは再度押せないままにする
        button.interactable = value && !opened;
    }
    private void PlayHitEffect()
    {
        if (hitEffect == null) return;

        hitEffect.SetActive(true);

        if (hitEffectAnimator != null)
        {
            // 毎回先頭から再生
            hitEffectAnimator.Play(hitEffectStateName, 0, 0f);

            // クリップ長で消す（Animatorから取得）
            var clips = hitEffectAnimator.runtimeAnimatorController.animationClips;
            float len = 0.3f;
            if (clips != null && clips.Length > 0) len = clips[0].length;

            StartCoroutine(HideHitEffectAfter(len));
        }
    }


    private System.Collections.IEnumerator HideHitEffectAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (hitEffect != null) hitEffect.SetActive(false);
    }


}
