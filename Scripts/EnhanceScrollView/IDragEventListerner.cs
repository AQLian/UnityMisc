using UnityEngine.EventSystems;

public interface IDragArenaEventListerner 
{
    void OnBeginDrag(PointerEventData eventData);
    void OnDrag(PointerEventData eventData);
    void OnEndDrag(PointerEventData eventData);
}
