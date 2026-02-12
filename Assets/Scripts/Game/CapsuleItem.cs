using System.Collections;
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
    [SerializeField] private GameObject missVisual; // MissOverlay（はずれ表示）

    [Header("Reveal Flow")]
    [Tooltip("Closed → OpenVisual(少し) → Hit または Miss")]
    [SerializeField] private GameObject openVisual;
    [SerializeField, Min(0f)] private float openVisualDuration = 0.5f;

    [Header("Miss")]
    [Tooltip("ON: Miss時にOpenVisualを表示したまま、MissOverlayを重ねる / OFF: Miss時はOpenVisualを消してMissだけ残す")]
    [SerializeField] private bool keepOpenVisualOnMiss = true;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Animator hitEffectAnimator; // HitEffect に付いてる Animator
    [SerializeField] private string hitEffectStateName = "HitEffect"; // Animator内のState名

    // --- 状態 ---
    private bool isHit;
    private bool opened;
    private StageManager manager;

    // 当たり画像（Hitのときだけ使う）
    private Sprite hitSprite;

    public RectTransform Rotator => rotator;

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
        if (openVisual != null) openVisual.SetActive(false);
        if (hitVisual != null) hitVisual.SetActive(false);
        if (missVisual != null) missVisual.SetActive(false);

        // ヒットエフェクトを最初は消す
        if (hitEffect != null) hitEffect.SetActive(false);

        // クリック登録
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickCapsule);
            button.interactable = true;
        }
    }

    private void OnClickCapsule()
    {
        // 押されたカプセルを最前面へ（UIの重なり順を上げる）
        transform.SetAsLastSibling();

        // ★ポップアップ中などは無視（StageManager側に無ければ常にfalse扱い）
        if (manager != null && manager.IsInputBlocked) return;

        if (opened) return;
        opened = true;

        if (button != null) button.interactable = false;

        if (closedVisual != null) closedVisual.SetActive(false);

        // Closed → Open(0.5s) → Hit or Miss(keep)
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        // Open表示
        if (openVisual != null) openVisual.SetActive(true);

        if (openVisualDuration > 0f)
            yield return new WaitForSeconds(openVisualDuration);

        if (isHit)
        {
            // Hit: Openを消してHitへ
            if (openVisual != null) openVisual.SetActive(false);

            // HitVisual の Image の sprite を差し替え（キャラ画像）
            if (hitVisual != null)
            {
                var img = hitVisual.GetComponent<Image>();
                if (img != null && hitSprite != null)
                {
                    img.sprite = hitSprite;
                    // img.SetNativeSize(); // 必要ならON
                }

                hitVisual.SetActive(true);
                PlayHitEffect();
            }

            // 当たりだけ加算
            if (manager != null) manager.OnHitFound();
        }
        else
        {
            // Miss: 「閉じない」
            // Openを維持するならそのまま、消すならOFF
            if (!keepOpenVisualOnMiss && openVisual != null)
                openVisual.SetActive(false);

            if (missVisual != null)
                missVisual.SetActive(true);

            // Missはカウントしない
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
            var clips = hitEffectAnimator.runtimeAnimatorController != null
                ? hitEffectAnimator.runtimeAnimatorController.animationClips
                : null;

            float len = 0.3f;
            if (clips != null && clips.Length > 0) len = clips[0].length;

            StartCoroutine(HideHitEffectAfter(len));
        }
    }

    private IEnumerator HideHitEffectAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (hitEffect != null) hitEffect.SetActive(false);
    }
}
