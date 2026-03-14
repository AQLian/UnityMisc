using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class DraggableArenaEventHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    IDragArenaEventListerner listener;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(listener == null)
        {
            listener = GetComponentInChildren<IDragArenaEventListerner>();
        }
        listener?.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        listener?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        listener?.OnEndDrag(eventData);
    }
}
