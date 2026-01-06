using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class DragSpeedDetector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector2 dragStartPosition;
    private float dragStartTime;

    private Vector2 lastPosition;
    private float lastTime;

    public float speedThreshold = 500f; // 速度阈值，单位：像素/秒

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
        dragStartTime = Time.time;

        lastPosition = eventData.position;
        lastTime = Time.time;
    }

    public void OnDrag(PointerEventData eventData)
    {
        lastPosition = eventData.position;
        lastTime = Time.time;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 dragEndPosition = eventData.position;
        float dragEndTime = Time.time;

        // 计算总位移和总时间
        Vector2 totalDisplacement = dragEndPosition - dragStartPosition;
        float totalTime = dragEndTime - dragStartTime;

        // 防止除以零
        if (totalTime <= 0f)
            return;

        // 平均速度 = 总位移 / 总时间
        Vector2 averageVelocity = totalDisplacement / totalTime;

        // 取速度大小（模长）
        float speed = averageVelocity.magnitude;

        Debug.Log($"[Drag] Average Speed: {speed:F2} px/s");

        // 判断是否超过阈值
        if (speed > speedThreshold)
        {
            Debug.Log("🎉 滑动速度超过阈值，触发动作！");
            // TODO: 执行你的动作，比如翻页、切换界面等
        }
        else
        {
            Debug.Log("👋 滑动速度不足，不触发动作。");
        }
    }
}