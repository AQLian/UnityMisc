using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public class VideoFitHandler : MonoBehaviour
{
    private RawImage rawImage;
    private RectTransform rt;
    private VideoPlayer videoPlayer;

    public FitInsideOrOutSide side; 

    public enum FitInsideOrOutSide
    {
        Outside,
        Inside
    }

    public enum WidthOrHeight
    {
        Width,
        Height
    }

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rt = rawImage.rectTransform;
        videoPlayer = GetComponent<VideoPlayer>();
    }

    void Start()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorHandle;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.Prepare();
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        var rect = rt.rect;
        DeterminedWidthOrHeigh(side, out var axis, out var ratio);
        if(axis == WidthOrHeight.Width)
        {
            var realWidth = rect.height * ratio;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, realWidth);
        }
        else
        {
            var realHeigh = rect.width / ratio;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, realHeigh);
        }
    }

    private void DeterminedWidthOrHeigh(FitInsideOrOutSide side, out WidthOrHeight mainAxis, out float ratio)
    {
        ratio = (float)videoPlayer.width / (float)videoPlayer.height;
        var widthRatio = (float)videoPlayer.width / rt.rect.width;
        var heightRatio = (float)videoPlayer.height / rt.rect.height;
        if(side == FitInsideOrOutSide.Outside)
        {
            //max(widthRatio, heightRatio)
            mainAxis = widthRatio > heightRatio ? WidthOrHeight.Width : WidthOrHeight.Height;
        }
        else
        {
            //min(widthRatio, heightRatio)
            mainAxis = widthRatio < heightRatio ? WidthOrHeight.Width : WidthOrHeight.Height;
        }
    }

    private void OnVideoPlayerErrorHandle(VideoPlayer source, string message)
    {
    }
}