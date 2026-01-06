using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Pool;
using UnityEngine.Assertions;

[RequireComponent(typeof(RectTransform))]
public class LoopingPageView : UIBehaviour, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Axis { Horizontal, Vertical }

    [Header("Layout Settings")]
    public Vector2 Spacing = new Vector2(20, 0);
    public Axis MoveAxis = Axis.Horizontal;

    [Header("Auto Loop Settings")]
    public bool AutoLoop = false;
    public float LoopInterval = 3f;
    public int LoopDir = 1;

    [Header("Snaping")]
    public float SpeedThreshold = 200f;
    [SerializeField] private float _minSnapDuration = 0.15f; 
    [SerializeField] private float _maxSnapDuration = 0.5f;
    private float m_snapDuration;

    private bool m_Dragging = false;
    private bool m_IsNormalizing = false;
    private RectTransform m_SnapChild;
    private RectTransform viewRectTran;
    private Vector2 m_PrePos;
    private float currTimeDelta = 0;

    private RectTransform beginDragOver;

    Vector3[] firstCorners = new Vector3[4];
    Vector3[] lastCorners = new Vector3[4];
    Vector3[] viewCorners = new Vector3[4];
    private Vector2 dragStartPosition;
    private float dragStartTime;

    [Header("Setup Pages")]
    public RectTransform pagePrefab;
    public int PageCount;
    [SerializeField]
    private Vector2 CellSize;

    protected override void Awake()
    {
        base.Awake();
        viewRectTran = GetComponent<RectTransform>();

        SetupPages();
    }

    void DestroyAll()
    {
        for (var i = transform.childCount - 1; i >= 0; --i)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    private void SetupPages()
    {
        DestroyAll();

        if (pagePrefab != null)
        {
            CellSize = pagePrefab.rect.size;
            for (var i = 0; i < PageCount; ++i)
            {
                var ins = GameObject.Instantiate(pagePrefab);
                ins.SetParent(transform, false);
                ins.localPosition = (MoveAxis == Axis.Horizontal) ? new Vector3(i * (CellSize.x + Spacing.x), 0, 0) : new Vector3(0, i * (CellSize.y + Spacing.y), 0);
                ins.GetComponentInChildren<Text>().text = i.ToString();
            }
        }
        else
        {
            Assert.IsTrue(viewRectTran.childCount > 0, "Must at least one child");
            PageCount = viewRectTran.childCount;
            CellSize = viewRectTran.GetChild(0).GetComponent<RectTransform>().rect.size;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected virtual void Update()
    {
        if (ContentIsLongerThanRect())
        {
            CheckForLoop();

            if (m_IsNormalizing && EnsureListCanAdjust())
            {
                StopSnapChild();
                m_ChildSnapCoroutine = StartCoroutine(SnapChild(m_snapDuration));
                m_IsNormalizing = false;
            }

            if (AutoLoop && !m_Dragging && m_ChildSnapCoroutine == null && EnsureListCanAdjust())
            {
                StopSnapChild();
                currTimeDelta += Time.deltaTime;
                if (currTimeDelta >= LoopInterval)
                {
                    currTimeDelta = 0;
                    MoveToIndex(LoopDir);
                }
            }
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!ContentIsLongerThanRect()) return;
        m_Dragging = true;
        dragStartPosition = eventData.position;
        dragStartTime = Time.time;
        m_PrePos = eventData.position;
        m_IsNormalizing = false;
        m_SnapChild = null;
        StopSnapChild();
        beginDragOver = GetOverChild(eventData.rawPointerPress.transform);
    }

    private RectTransform GetOverChild(Transform transform)
    {
        while (transform != null)
        {
            foreach (RectTransform r in viewRectTran) { 
                if(r == transform)
                {
                    return r;
                }
            }
            transform = transform.parent;
        }
        return null;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!ContentIsLongerThanRect()) return;
        Vector2 currentPos = eventData.position;
        Vector2 delta = currentPos - m_PrePos;
        SetContentPosition(delta);
        m_PrePos = currentPos;
        m_IsNormalizing = false;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        var dragEndPosition = eventData.position;
        var dragEndTime = Time.time;
        var totalDisplacement = dragEndPosition - dragStartPosition;
        var totalTime = dragEndTime - dragStartTime;
        var averageVelocity = totalDisplacement / totalTime;
        var speed = averageVelocity.magnitude;
        m_Dragging = false;
        if(Vector3.Distance(dragStartPosition, dragEndPosition) > (MoveAxis == Axis.Horizontal ? CellSize.x : CellSize.y))
        {
            m_IsNormalizing = true;
            m_SnapChild = GetNearest();
        }
        else
        {
            if (speed < SpeedThreshold)
            {
                m_IsNormalizing = true;
                m_SnapChild = GetNearest();
            }
            else
            {
                m_IsNormalizing = true;
                m_SnapChild = GetNextPage(beginDragOver, totalDisplacement.x < 0 ? 1 : -1);
            }
        }
        var distance = MoveAxis == Axis.Horizontal ? ((Mathf.Abs(totalDisplacement.x)% CellSize.x) / CellSize.x): ((Mathf.Abs(totalDisplacement.y)%CellSize.y) / CellSize.y);
        m_snapDuration = Mathf.Lerp(_minSnapDuration, _maxSnapDuration, distance);
    }

    RectTransform GetNextPage(RectTransform from, int direction)
    {
        for (var i = 0; i < viewRectTran.childCount; ++i)
        {
            var c = viewRectTran.GetChild(i);
            if (c == from)
            {
                var nextIndex = direction == 1 ? i + 1 : i - 1;
                if((i == viewRectTran.childCount - 1 && direction == 1) || (i == 0 && direction == -1))
                {
                    LoopCell(direction);
                    if(i == viewRectTran.childCount - 1)
                    {
                        nextIndex = viewRectTran.childCount - 1;
                    }else if (i == 0)
                    {
                        nextIndex = 0;
                    }
                }
                
                return  viewRectTran.GetChild(nextIndex) as RectTransform;
            }
        }
        return null;
    }

    /// <summary>
    /// Calculates the local position of the page closest to the center.
    /// </summary>
    /// <returns>The target local position for snapping.</returns>
    public virtual RectTransform GetNearest()
    {
        float minDistance = float.MaxValue;
        RectTransform snapChild = null;
        foreach (RectTransform child in viewRectTran)
        {
            float distance = Mathf.Abs(child.localPosition.x);
            if (distance < minDistance)
            {
                minDistance = distance;
                snapChild= child;
            }
        }
        return snapChild;
    }

    public void StopSnapChild()
    {
        if (m_ChildSnapCoroutine != null)
        {
            StopCoroutine(m_ChildSnapCoroutine);
            m_ChildSnapCoroutine = null;
        }
    }

    private Coroutine m_ChildSnapCoroutine;
    private IEnumerator SnapChild(float duration)
    {
        if (m_SnapChild)
        {
            float elapsed = 0f;
            var startPos = m_SnapChild.localPosition;
            using var _ = DictionaryPool<RectTransform, (Vector3,Vector3)>.Get(out var kv);
            foreach(RectTransform c in viewRectTran)
            {
                if (c == m_SnapChild) continue;
                kv.TryAdd(c, (c.localPosition, c.localPosition - startPos));
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = 1f - Mathf.Pow(1f - t, 3);
                m_SnapChild.anchoredPosition = Vector2.Lerp(startPos, Vector3.zero, t);
                foreach(var other in kv)
                {
                    other.Key.anchoredPosition = Vector2.Lerp(other.Value.Item1, other.Value.Item2, t);
                }
                yield return null;
                if (t >= 1)
                {
                    m_SnapChild = null;
                    StopSnapChild();
                    yield break;
                }
            }
        }
    }


    /// <summary>
    /// Checks if the content has reached a boundary and needs to loop.
    /// </summary>
    protected virtual void CheckForLoop()
    {
        int boundaryState = GetBoundaryState();
        if (boundaryState != 0)
        {
            LoopCell(boundaryState);
        }
    }

    /// <summary>
    /// Performs the loop by moving a cell from one end to the other.
    /// </summary>
    /// <param name="dir">Direction of the loop: -1 for left/top, 1 for right/bottom.</param>
    protected virtual void LoopCell(int dir)
    {
        if (dir == 0) return;

        RectTransform moveCell;
        RectTransform tarBorder;
        Vector2 tarPos;

        if (dir == 1) // Move first cell to the end
        {
            moveCell = GetChild(viewRectTran, 0);
            tarBorder = GetChild(viewRectTran, viewRectTran.childCount - 1);
            moveCell.SetSiblingIndex(viewRectTran.childCount - 1);
        }
        else // Move last cell to the beginning
        {
            tarBorder = GetChild(viewRectTran, 0);
            moveCell = GetChild(viewRectTran, viewRectTran.childCount - 1);
            moveCell.SetSiblingIndex(0);
        }

        if (MoveAxis == Axis.Horizontal)
        {
            tarPos = tarBorder.localPosition + new Vector3((CellSize.x + Spacing.x) * dir, 0, 0);
        }
        else
        {
            tarPos = (Vector2)tarBorder.localPosition + new Vector2(0, (CellSize.y + Spacing.y) * dir);
        }
        moveCell.localPosition = tarPos;
    }

    private static RectTransform GetChild(RectTransform parent, int index)
    {
        if (parent == null || index >= parent.childCount)
        {
            return null;
        }
        return parent.GetChild(index) as RectTransform;
    }

    /// <summary>
    /// Checks if the total content size is larger than the viewport.
    /// </summary>
    public virtual bool ContentIsLongerThanRect()
    {
        if (viewRectTran == null || viewRectTran.childCount == 0) return false;

        var contentSize = 0f;
        if (MoveAxis == Axis.Horizontal)
        {
            contentSize = viewRectTran.childCount * (CellSize.x + Spacing.x) - Spacing.x;
            return contentSize > viewRectTran.rect.width;
        }
        else
        {
            contentSize = viewRectTran.childCount * (CellSize.y + Spacing.y) - Spacing.y;
            return contentSize > viewRectTran.rect.height;
        }
    }

    /// <summary>
    /// Detects if the content has reached a boundary.
    /// </summary>
    /// <returns>-1 for left/top boundary, 1 for right/bottom, 0 otherwise.</returns>
    public virtual int GetBoundaryState()
    {
        if (viewRectTran.childCount < 2) return 0;

        var firstChild = GetChild(viewRectTran, 0);
        var lastChild = GetChild(viewRectTran, viewRectTran.childCount - 1);

        if(!firstChild || !lastChild)
            return 0;

        firstChild.GetWorldCorners(firstCorners);
        lastChild.GetWorldCorners(lastCorners);
        viewRectTran.GetWorldCorners(viewCorners);

        if (MoveAxis == Axis.Horizontal)
        {
            if (firstCorners[0].x > viewCorners[0].x) return -1;
            if (lastCorners[3].x < viewCorners[3].x) return 1;
        }
        else
        {
            if (firstCorners[0].y > viewCorners[0].y) return -1;
            if (lastCorners[1].y < viewCorners[1].y) return 1;
        }
        return 0;
    }

    private bool EnsureListCanAdjust() 
    { 
        return !m_Dragging && viewRectTran.childCount > 0;
    }

    private void SetContentPosition(Vector2 delta)
    {
        var vec3Delta = (Vector3)delta;
        if (MoveAxis == Axis.Horizontal) 
        {
            vec3Delta.y = 0;
        }
        else
        {
            vec3Delta.x = 0;
        }
        foreach (RectTransform child in viewRectTran)
        {
            child.localPosition += vec3Delta;
        }
    }

    public void MoveToIndex(int dir)
    {
        StopSnapChild();
        m_IsNormalizing = true;
        var nearst = GetNearest();
        m_SnapChild = GetNextPage(nearst, dir);
        m_snapDuration = _maxSnapDuration;
    }
}