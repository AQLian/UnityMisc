using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
/// <summary>
/// Modified Gradient effect script from http://answers.unity3d.com/questions/1086415/gradient-text-in-unity-522-basevertexeffect-is-obs.html
/// -Uses Unity's Gradient class to define the color
/// -Offset is now limited to -1,1
/// -Multiple color blend modes
/// 
/// Remember that the colors are applied per-vertex so if you have multiple points on your gradient where the color changes and there aren't enough vertices, you won't see all of the colors.
/// </summary>
[AddComponentMenu("UI/Effects/Gradient")]
public class Gradient1 : BaseMeshEffect
{
    [SerializeField]
    Type _gradientType;

    [SerializeField]
    Blend _blendMode = Blend.Multiply;

    [SerializeField]
    [Range(-1, 1)]
    float _offset = 0f;

    [SerializeField]
    UnityEngine.Gradient _effectGradient = new UnityEngine.Gradient()
    {
        colorKeys = new GradientColorKey[]
        {
                new GradientColorKey(Color.black, 0),
                new GradientColorKey(Color.white, 1)
        }
    };

    #region Properties
    public Blend BlendMode
    {
        get { return _blendMode; }
        set { _blendMode = value; }
    }

    public UnityEngine.Gradient EffectGradient
    {
        get { return _effectGradient; }
        set { _effectGradient = value; }
    }
    #endregion


    [Header("渐变配置")]
    public Gradient gradient;

    [Header("滑动时间")]
    public float slideTime = 2f;

    [Range(0.05f, 0.3f)]
    public float observationWidth = 0.15f;
    [Range(0f, 1f)]
    public float allTextMaxWidth = .5f;
    private float _lastMaxWidth = -1f;

    private TextMeshProUGUI tmpText;
    public float adjustedObservationWidth;
    public float time;

    private List<float> observationCenters;
    private int _lastVisible = -1;



    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || helper.currentVertCount == 0)
            return;

        var visibleCount = CountVisibleCharacters(helper);
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
        var slideSpeed = 1f / slideTime;
        var clampedOffset = maxOffset > 0 ? Mathf.PingPong(Time.time * slideSpeed, maxOffset) : 0;
        for (var i = 0; i < visibleCount; i++)
        {
            observationCenters[i] = (i + 0.5f) * adjustedObservationWidth;
        }


        using var _ = ListPool<UIVertex>.Get(out var _vertexList);
        helper.GetUIVertexStream(_vertexList);
        int nCount = _vertexList.Count;
        var leftKey = _vertexList[0].position.x;
        var rightKey = _vertexList[0].position.x;
        float x = 0f;
        for (int i = nCount - 1; i >= 1; --i)
        {
            x = _vertexList[i].position.x;
            if (x > rightKey) rightKey = x;
            else if (x < leftKey) leftKey = x;
        }
        float width = 1f / (rightKey - leftKey);

        UIVertex vertex = new UIVertex();

        var charIndex = 0;
        for (int i = 0; i < helper.currentVertCount; i += 4)
        {
            var center = observationCenters[charIndex];
            var left = Mathf.Clamp(center - adjustedObservationWidth / 2f + clampedOffset, 0f, 1f);
            var right = Mathf.Clamp(center + adjustedObservationWidth / 2f + clampedOffset, 0f, 1f);

            var leftColor = gradient.Evaluate(left);
            var rightColor = gradient.Evaluate(right);

            for (var v = 0; v < 4; v++)
            {
                var idxIn = i + v;
                helper.PopulateUIVertex(ref vertex, idxIn);
                var ratio = v == 0 || v == 3 ? 0f : 1f;
                vertex.color = Color.Lerp(leftColor, rightColor, ratio);
                helper.SetUIVertex(vertex, idxIn);
            }

            charIndex++;
        }

        //for (int i = 0; i < helper.currentVertCount; i++)
        //{
        //    helper.PopulateUIVertex(ref vertex, i);
        //    vertex.color = BlendColor(vertex.color, EffectGradient.Evaluate((vertex.position.x - leftKey) * width));
        //    helper.SetUIVertex(vertex, i);
        //}
    }

    void Update()
    {
        graphic.SetVerticesDirty();
    }

    private int CountVisibleCharacters(VertexHelper vh)
    {
        var count = 0;
        for (int i = 0; i < vh.currentVertCount; i += 4)
        {
            count++;
        }
        return count;
    }

    Color BlendColor(Color colorA, Color colorB)
    {
        switch (BlendMode)
        {
            default: return colorB;
            case Blend.Add: return colorA + colorB;
            case Blend.Multiply: return colorA * colorB;
        }
    }

    public enum Type
    {
        Horizontal,
        Vertical
    }

    public enum Blend
    {
        Override,
        Add,
        Multiply
    }
}
