using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class TestEnhanceItem : EnhanceItem
{
    private RawImage rawImage;

    protected override void OnStart()
    {
        rawImage = GetComponent<RawImage>();
    }

    protected override void OnSelectStateChange(bool isCenter)
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        if (rawImage) { 
            rawImage.color = isCenter ? Color.white : Color.gray;
        }
    }
}
