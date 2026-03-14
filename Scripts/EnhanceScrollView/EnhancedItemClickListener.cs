using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class EnhancedItemClickListener : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var dragging = eventData.dragging;
        if (dragging)
        {
            return;
        }

        if (this.TryGetComponent<EnhanceItem>(out var inst))
        {
            inst.onClickAction?.Invoke();
        }
    }
}
