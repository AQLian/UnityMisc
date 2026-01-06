using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OptimizedSwipeDetector : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public float speedThreshold = 500f;
    public float lastCheckedSpeed;

    private Vector2 startPos;
    private float startTime;
    private Vector2 lastPos;
    private float lastTime;

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = eventData.position;
        startTime = Time.time;
        lastPos = startPos;
        lastTime = startTime;
    }

    public void OnDrag(PointerEventData eventData)
    {
        lastPos = eventData.position;
        lastTime = Time.time;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 totalDisplacement = lastPos - startPos;
        float totalTime = lastTime - startTime;

        if (totalTime < 0.001f) return;


    }
}
