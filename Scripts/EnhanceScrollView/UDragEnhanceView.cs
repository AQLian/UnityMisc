using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UDragEnhanceView : EventTrigger
{
    private EnhanceScrollView enhanceScrollView;
    public void SetScrollView(EnhanceScrollView view)
    {
        enhanceScrollView = view;
    }
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        var dragging = eventData.dragging;
        if (dragging)
        {
            return;
        }
        
        if (this.TryGetComponent<MyUGUIEnhanceItem>(out var inst))
        {
            inst.onClickAction?.Invoke();
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        if (enhanceScrollView != null)
            enhanceScrollView.OnDragEnhanceViewMove(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        if (enhanceScrollView != null)
            enhanceScrollView.OnDragEnhanceViewEnd();
    }
}
