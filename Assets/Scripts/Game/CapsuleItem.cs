using UnityEngine;
using UnityEngine.UI;


// カプセル1個の挙動
// ボタンを押されたら1回だけ開く
// 当たり / はずれの見た目切替
// 当たりなら StageManager に通知 


public class CapsuleItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Button button;

    [Header("Visuals")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject hitVisual;
    [SerializeField] private GameObject missVisual;

    // --- 状態 ---
    private bool isHit;              // このカプセルは当たり？
    private bool opened;             // もう開いた？
    private StageManager manager;    // 通知先

    public void Setup(StageManager managerRef, bool hitFlag) //Setup = このオブジェクトがちゃんと動くように、外部情報を注入する準備処理
    {
        this.manager = managerRef;
        isHit = hitFlag;
        opened = false;

        // 初期表示：閉じた状態だけON
        if(closedVisual != null) closedVisual.SetActive(true);
        if(hitVisual!= null) hitVisual.SetActive(false);
        if(missVisual!= null) missVisual.SetActive(false);

        //クリック登録
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickCapsule);

        //ボタン有効化
        button.interactable = true;
    }

    private void OnClickCapsule()
    {
        // TODO: すでに開いてたら何もしない（opened判定）
        if (opened) return;
        opened = true;

        //ボタン無効化（連打防止）
        button.interactable = false;

        // 見た目切り替え：閉じた表示OFF
        if (closedVisual != null) closedVisual.SetActive(false);

        if (isHit)
        {
            //当たり表示ON
            if(hitVisual != null)hitVisual.SetActive(true);

            // TODO: StageManager に「当たり見つけた」を通知
            manager.OnHitFound();
        }else
        {

            //はずれ表示
            if (missVisual != null)missVisual.SetActive(true);
        }
    }









}
