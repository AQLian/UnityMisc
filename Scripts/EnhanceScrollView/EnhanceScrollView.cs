using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnhanceScrollView : MonoBehaviour, IDragArenaEventListerner
{
    // Control the item's scale curve
    public AnimationCurve scaleCurve;
    // Control the position curve
    public AnimationCurve positionCurve;
    // Control the "depth"'s curve(In 3d version just the Z value, in 2D UI you can use the depth(NGUI))
    // NOTE:
    // 1. In NGUI set the widget's depth may cause performance problem
    // 2. If you use 3D UI just set the Item's Z position
    public AnimationCurve depthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    // The start center index
    [Tooltip("The Start center index")]
    public int startCenterIndex = 0;
    // Offset width between item
    public float cellWidth = 10f;
    private float totalHorizontalWidth = 500.0f;
    // vertical fixed position value 
    public float yFixedPositionValue = 46.0f;

    // Lerp duration
    public float lerpDuration = 0.2f;
    private float mCurrentDuration = 0.0f;
    private int mCenterIndex = 0;
    public bool enableLerpTween = true;

    // center and preCentered item
    private EnhanceItem curCenterItem;
    private EnhanceItem preCenterItem;

    // if we can change the target item
    private bool canChangeItem = true;
    private float dFactor = 0.2f;

    // originHorizontalValue Lerp to horizontalTargetValue
    private float originHorizontalValue = 0.1f;
    [SerializeField]
    private float curHorizontalValue = 0.5f;

    // "depth" factor (2d widget depth or 3d Z value)
    public int depthFactor = 5;
    // targets enhance item in scroll view
    public List<EnhanceItem> listEnhanceItems;
    // sort to get right index
    private List<EnhanceItem> listSortedItems = new List<EnhanceItem>();

    public bool InitCenterlized = true;

    private static EnhanceScrollView instance;
    public static EnhanceScrollView GetInstance => instance;

    void Awake() => instance = this;

    public Transform maskArena;
    public int viewCount = 3;


    void Start()
    {
        canChangeItem = true;
        int count = listEnhanceItems.Count;
        dFactor = 1f / count;
        mCenterIndex = count / 2;
        if (count % 2 == 0)
            mCenterIndex = count / 2 - 1;
        int index = 0;
        for (int i = count - 1; i >= 0; i--)
        {
            listEnhanceItems[i].CurveOffSetIndex = i;
            listEnhanceItems[i].CenterOffSet = dFactor * (mCenterIndex - index);
            listEnhanceItems[i].SetSelectState(false);
            GameObject obj = listEnhanceItems[i].gameObject;
            index++;
        }

        // set the center item with startCenterIndex
        startCenterIndex = mCenterIndex;

        // restrict view size
        if (maskArena)
        {
            cellWidth = maskArena.GetComponent<RectTransform>().rect.width / viewCount;
        }

        // sorted items
        listSortedItems = new List<EnhanceItem>(listEnhanceItems.ToArray());
        totalHorizontalWidth = cellWidth * count;
        GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalHorizontalWidth);
        if (InitCenterlized)
        {
            var local = transform.localPosition;
            local.x -= totalHorizontalWidth * .5f;
            transform.localPosition = local;
        }
        curCenterItem = listEnhanceItems[startCenterIndex];
        curHorizontalValue = 0.5f - curCenterItem.CenterOffSet;
        LerpTweenToTarget(0f, curHorizontalValue, false);
        this.OnTweenOver();
    }

    private void LerpTweenToTarget(float originValue, float targetValue, bool needTween = false)
    {
        if (!needTween)
        {
            SortEnhanceItem();
            originHorizontalValue = targetValue;
            UpdateEnhanceScrollView(targetValue);
        }
        else
        {
            originHorizontalValue = originValue;
            curHorizontalValue = targetValue;
            mCurrentDuration = 0.0f;
        }
        enableLerpTween = needTween;
    }

    /// 
    /// Update EnhanceItem state with curve fTime value
    /// 
    public void UpdateEnhanceScrollView(float fValue)
    {
        for (var i = 0; i < listEnhanceItems.Count; i++)
        {
            var itemScript = listEnhanceItems[i];
            var evaluteValue = fValue + itemScript.CenterOffSet;
            var xValue = positionCurve.Evaluate(evaluteValue) * totalHorizontalWidth;
            var scaleValue = scaleCurve.Evaluate(evaluteValue);
            var depthCurveValue = depthCurve.Evaluate(evaluteValue);
            itemScript.UpdateScrollViewItems(xValue, depthCurveValue, depthFactor, listEnhanceItems.Count, yFixedPositionValue, scaleValue);
        }
    }

    void Update()
    {
        if (enableLerpTween)
            TweenViewToTarget();
    }

    private void TweenViewToTarget()
    {
        mCurrentDuration += Time.deltaTime;
        if (mCurrentDuration > lerpDuration)
            mCurrentDuration = lerpDuration;

        var percent = mCurrentDuration / lerpDuration;
        var value = Mathf.Lerp(originHorizontalValue, curHorizontalValue, percent);
        UpdateEnhanceScrollView(value);
        if (mCurrentDuration >= lerpDuration)
        {
            canChangeItem = true;
            enableLerpTween = false;
            OnTweenOver();
        }
    }

    private void OnTweenOver()
    {
        if (preCenterItem != null)
            preCenterItem.SetSelectState(false);
        if (curCenterItem != null)
            curCenterItem.SetSelectState(true);
    }

    private int GetMoveCurveFactorCount(EnhanceItem preCenterItem, EnhanceItem newCenterItem)
    {
        SortEnhanceItem();
        var factorCount = Mathf.Abs(newCenterItem.RealIndex) - Mathf.Abs(preCenterItem.RealIndex);
        return Mathf.Abs(factorCount);
    }

    // sort item with X so we can know how much distance we need to move the timeLine(curve time line)
    public int SortPosition(EnhanceItem a, EnhanceItem b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x);
    
    private void SortEnhanceItem()
    {
        listSortedItems.Sort(SortPosition);
        for (var i = listSortedItems.Count - 1; i >= 0; i--)
            listSortedItems[i].RealIndex = i;
    }

    public void SetHorizontalTargetItemIndex(EnhanceItem selectItem)
    {
        if (!canChangeItem)
            return;

        if (curCenterItem == selectItem)
            return;

        canChangeItem = false;
        preCenterItem = curCenterItem;
        curCenterItem = selectItem;

        // calculate the direction of moving
        var centerXValue = positionCurve.Evaluate(0.5f) * totalHorizontalWidth;
        var isRight = false;
        if (selectItem.transform.localPosition.x > centerXValue)
            isRight = true;

        // calculate the offset * dFactor
        var moveIndexCount = GetMoveCurveFactorCount(preCenterItem, selectItem);
        var dvalue = 0.0f;
        if (isRight)
        {
            dvalue = -dFactor * moveIndexCount;
        }
        else
        {
            dvalue = dFactor * moveIndexCount;
        }
        var originValue = curHorizontalValue;
        LerpTweenToTarget(originValue, curHorizontalValue + dvalue, true);
    }

    // Click the right button to select the next item.
    public void OnBtnRightClick()
    {
        if (!canChangeItem)
            return;
        var targetIndex = curCenterItem.CurveOffSetIndex + 1;
        if (targetIndex > listEnhanceItems.Count - 1)
            targetIndex = 0;
        SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);
    }

    // Click the left button the select next next item.
    public void OnBtnLeftClick()
    {
        if (!canChangeItem)
            return;
        int targetIndex = curCenterItem.CurveOffSetIndex - 1;
        if (targetIndex < 0)
            targetIndex = listEnhanceItems.Count - 1;
        SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    // On Drag Move
    public void OnDrag(PointerEventData eventData)
    {
        // In developing
        if (Mathf.Abs(eventData.delta.x) > 0.0f)
        {
            curHorizontalValue += eventData.delta.x /totalHorizontalWidth ;
            LerpTweenToTarget(0.0f, curHorizontalValue, false);
        }
    }

    // On Drag End
    public void OnEndDrag(PointerEventData eventData)
    {
        // find closed item to be centered
        int closestIndex = 0;
        float min = float.MaxValue;
        var center = .5f * totalHorizontalWidth;
        for (int i = 0; i < listEnhanceItems.Count; i++)
        {
            var dis = Vector3.Distance(listEnhanceItems[i].transform.localPosition,
                new Vector3(center, yFixedPositionValue, 0));
            if (dis < min)
            {
                closestIndex = i;
                min = dis;
            }
        }

        originHorizontalValue = curHorizontalValue;
        preCenterItem = curCenterItem;
        var closest = listEnhanceItems[closestIndex];
        if (eventData.delta.x > 0)
        {
            closest = listEnhanceItems[closestIndex - 1 < 0 ? listEnhanceItems.Count - 1 : closestIndex - 1];
            curCenterItem = closest;
            LerpTweenToTarget(originHorizontalValue, (float)Snap(originHorizontalValue, dFactor) + dFactor, true);
        }
        else if (eventData.delta.x < 0)
        {
            closest = listEnhanceItems[closestIndex + 1 > listEnhanceItems.Count - 1 ? 0 : closestIndex + 1];
            curCenterItem = closest;
            LerpTweenToTarget(originHorizontalValue, (float)Snap(originHorizontalValue, dFactor) - dFactor, true);
        }
        else
        {
            curCenterItem = closest;
            LerpTweenToTarget(originHorizontalValue, (float)Snap(originHorizontalValue, dFactor), true);
        }

        canChangeItem = false;
    }

    double Snap(double value, double interval)
    {
        if (listEnhanceItems.Count % 2 == 0)
        {
            var mul = value / interval;
            return Math.Round(mul, MidpointRounding.AwayFromZero) * interval;
        }
        else
        {
            interval = interval * .5f;
            var mul = value / interval;
            var r = Math.Round(mul, MidpointRounding.AwayFromZero);
            if (r > mul && Mathf.Floor((float)r) % 2 == 0)
            {
                r = r - 1;
            }
            else if ( r < mul && Mathf.Floor((float)r) % 2 == 0)
            {
                r = r + 1;
            }
            return r * interval;
        }
    }
}