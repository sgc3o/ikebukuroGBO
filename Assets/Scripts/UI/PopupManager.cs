using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
public class PopupManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;// PopupLayerのCanvasGroup
    [SerializeField] private Image popupImage;

    [Header("Sprites")]
    [SerializeField] private Sprite stage1Sprite;
    [SerializeField] private Sprite stage2Sprite;
    [SerializeField] private Sprite gameStartSprite;
    [SerializeField] private Sprite finishSprite;

    [Header("Timing")]
    [SerializeField] private float showSec = 2f;
    [SerializeField] private float fadeOutSec = 0.35f;


    Coroutine routine;

    private bool isShowing;
    public bool IsShowing => isShowing;

    /*public bool IsShowing
    {
        get
        {
            // ルーチンが回ってる＝表示中
            if (routine != null) return true;

            // 念のため：alphaやenabledでも判定（保険）
            if (canvasGroup != null && canvasGroup.alpha > 0.01f) return true;
            if (popupImage != null && popupImage.enabled) return true;

            return false;
        }
    }*/

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        if(popupImage != null) popupImage.enabled = false;
        //isShowing = false;

    }

    public void ShowStage(int stageIndex)
    {
        var sp = (stageIndex <= 1) ? stage1Sprite : stage2Sprite;
        Show(sp);

    }

    public void ShowGameStart() => Show(gameStartSprite);
    public void ShowFinish() => Show(finishSprite);

    public void Show(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[PopupManager] sprite is null");
            return;
        }

        if(routine != null)StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(sprite));

    }

    IEnumerator ShowRoutine(Sprite sprite)
    {
        //isShowing = true;


        popupImage.sprite = sprite;
        popupImage.SetNativeSize();
        popupImage.enabled = true;

        canvasGroup.alpha= 1f;
        canvasGroup.blocksRaycasts= true;
        canvasGroup.interactable = false;

        yield return new WaitForSeconds(showSec);

        float t = 0f;
        float startA = canvasGroup.alpha;
        while(t < fadeOutSec)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startA,0f, t / fadeOutSec);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        popupImage.enabled = false;
        routine = null;

        //isShowing = false;


    }





}