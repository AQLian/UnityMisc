using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class RainbowText : BaseMeshEffect
{
    [Tooltip("彩虹渐变强度")]
    public float saturation = 1f;
    [Tooltip("颜色变化速度")]
    public float speed = 1f;

    private Text textComponent;

    protected override void Awake()
    {
        base.Awake();
        textComponent = GetComponent<Text>();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        // 获取所有顶点
        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        // 每个字符6个顶点
        int step = 6;
        string text = textComponent.text;

        for (int charIndex = 0; charIndex < text.Length; charIndex++)
        {
            // 跳过空字符
            if (charIndex * step >= verts.Count) break;

            // 计算该字符的彩虹色（基于字符索引）
            float hue = (charIndex * 0.1f + Time.time * speed) % 1f; // 每个字符色相偏移0.1

            // 设置该字符所有顶点颜色
            for (int i = 0; i < step; i++)
            {
                int vertIndex = charIndex * step + i;
                if (vertIndex < verts.Count)
                {
                    UIVertex vert = verts[vertIndex];
                    vert.color = Color.HSVToRGB(hue, saturation + i * 0.1f, Time.time);
                    verts[vertIndex] = vert;
                }
            }
        }

        // 更新顶点数据
        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }

    void Update()
    {
        // 动态更新彩虹色
        if (Application.isPlaying && speed > 0)
        {
            graphic.SetVerticesDirty();
        }
    }
}
