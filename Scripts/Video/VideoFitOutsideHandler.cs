using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public class VideoFitOutsideHandler : MonoBehaviour
{
    private RawImage rawImage;
    private RectTransform rt;
    private VideoPlayer videoPlayer;

    enum WidthOrHeight
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
        DeterminedWidthOrHeigh(out var axis, out var ratio);
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

    private void DeterminedWidthOrHeigh(out WidthOrHeight mainAxis, out float ratio)
    {
        ratio = (float)videoPlayer.width / (float)videoPlayer.height;
        var widthRatio = (float)videoPlayer.width / rt.rect.width;
        var heightRatio = (float)videoPlayer.height / rt.rect.height;
        mainAxis = widthRatio > heightRatio ? WidthOrHeight.Width : WidthOrHeight.Height;
    }

    private void OnVideoPlayerErrorHandle(VideoPlayer source, string message)
    {
    }
}