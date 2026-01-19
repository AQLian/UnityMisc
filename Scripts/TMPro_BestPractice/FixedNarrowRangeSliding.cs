using EasyTextEffects;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class FixedNarrowRangeSliding : MonoBehaviour
{
    [Header("渐变配置")]
    public Gradient gradient;

    [Header("滑动时间")]
    public float slideTime = 2f;

    [Range(0.02f, 0.3f)]
    public float observationWidth = 0.15f; 
    [Range(0f,1f)]
    public float allTextMaxWidth = .5f;
    private float _lastMaxWidth = -1f;

    private TextMeshProUGUI tmpText;
    public float adjustedObservationWidth;


    [Space(5)][Range(1, 120)] public int updatesPerSecond = 30;
    
    private float nextUpdateTime_ = 0;

    public float time { get; set; }
    
    private List<float> observationCenters;
    
    private int _lastVisible=-1;

    public float now;

    public bool nextFrameArrived = false;

    public AnimationCurve curve;

    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        UpdateColors();
    }

    void Update()
    {
        now = Time.time;

        time = Time.time;

        if (time < nextUpdateTime_)
            return;
        nextUpdateTime_ = time + 1f / updatesPerSecond*1.0f;

        UpdateColors();
    }

    void UpdateColors()
    {
        var textInfo = tmpText.textInfo;

        var visibleCount = CountVisibleCharacters(textInfo);
        if (visibleCount == 0) return;
        if (_lastVisible != visibleCount || _lastMaxWidth != allTextMaxWidth)
        {
            observationCenters = new List<float>(visibleCount);
            for (var i = 0; i < visibleCount; i++) { observationCenters.Add(0); } 
            _lastVisible = visibleCount;
            var seeWidth = Mathf.Min(allTextMaxWidth, observationWidth * visibleCount);
            _lastMaxWidth = allTextMaxWidth;
            adjustedObservationWidth = seeWidth / visibleCount;
        }

        var totalWidth = visibleCount * adjustedObservationWidth;
        var maxOffset = 1f - totalWidth;
        var slideSpeed = maxOffset / slideTime;
        var rawOffset = maxOffset > 0 ? Mathf.PingPong(Time.time * slideSpeed, maxOffset) : 0;
        //apply curve
        var clampedOffset = 0f;
        if (maxOffset > 0 && curve != null)
        {
            var normalizedProgress = Mathf.Clamp01(rawOffset / maxOffset);
            var remappedProgress = curve.Evaluate(normalizedProgress);
            clampedOffset = remappedProgress * maxOffset;
        }
        else
        {
            clampedOffset = 0;
        }

        for (int i = 0; i < visibleCount; i++)
        {
            observationCenters[i] = (i + 0.5f) * adjustedObservationWidth;
        }
        var charIndex = 0;
        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            var center = observationCenters[charIndex];
            var left = Mathf.Clamp(center - adjustedObservationWidth / 2f + clampedOffset, 0f, 1f);
            var right = Mathf.Clamp(center + adjustedObservationWidth / 2f + clampedOffset, 0f, 1f);

            var leftColor = gradient.Evaluate(left);
            var rightColor = gradient.Evaluate(right);

            if (charIndex == 0)
            {
                capList.Add(new CaptureList
                {
                    charIdx = charIndex,
                    leftRatio = left,
                    rightRatio = right,
                    time = Time.time,
                    delta = Time.deltaTime,
                    smoothDelta =Time.smoothDeltaTime,
                    frameCnt = Time.frameCount,
                });
            }

            var vertexIndex = charInfo.vertexIndex;
            for (var v = 0; v < 4; v++)
            {
                var idx = vertexIndex + v;
                var ratio = v < 2 ? 0f : 1f;
                textInfo.meshInfo[0].colors32[idx] = Color.Lerp(leftColor, rightColor, ratio);
            }

            charIndex++;
        }

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    public List<CaptureList> capList = new List<CaptureList>();

    [System.Serializable]
    public class CaptureList
    {
        public int charIdx;
        public float leftRatio;
        public float rightRatio;
        public float time;
        public float delta;
        public float smoothDelta;
        public int frameCnt;
    }

    int CountVisibleCharacters(TMP_TextInfo textInfo)
    {
        var count = 0;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
                count++;
        }
        return count;
    }
}