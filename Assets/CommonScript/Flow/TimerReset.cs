using UnityEngine;
using UnityEngine.EventSystems;

public class TimerReset : MonoBehaviour,
    IPointerDownHandler, IPointerMoveHandler, IScrollHandler,
    ISubmitHandler, ISelectHandler
{
    public Timer timer;

    public void OnPointerDown(PointerEventData eventData) => timer?.NotifyActivity();
    public void OnPointerMove(PointerEventData eventData) => timer?.NotifyActivity();
    public void OnScroll(PointerEventData eventData)      => timer?.NotifyActivity();
    public void OnSubmit(BaseEventData eventData)         => timer?.NotifyActivity();
    public void OnSelect(BaseEventData eventData)         => timer?.NotifyActivity();
}
