using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class MyUGUIEnhanceItem : EnhanceItem
{
    private Button uButton;
    private RawImage rawImage;
    public Action onClickAction;

    protected override void OnStart()
    {
        rawImage = GetComponent<RawImage>();
        uButton = GetComponent<Button>();
        onClickAction = OnClickUGUIButton;
    }

    private void OnClickUGUIButton()
    {
        OnClickEnhanceItem();
    }

    // Set the item "depth" 2d or 3d
    protected override void SetItemDepth(float depthCurveValue, int depthFactor, float itemCount)
    {
        var newDepth = (int)(depthCurveValue * itemCount);
        this.transform.SetSiblingIndex(newDepth);
    }

    public override void SetSelectState(bool isCenter)
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
        
        rawImage.color = isCenter ? Color.white : Color.gray;
    }
}
