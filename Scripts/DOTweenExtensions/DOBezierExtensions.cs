using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DOBezierExtensions
{
    public static Tweener DOBezier(this Transform transform, Vector3 startPos, Vector3 controlPos, Vector3 endPos, float time, Ease ease = Ease.InQuad, Action callback = null)
    {
        if (!transform)
        {
            return null;
        }
        DOTween.Kill(transform);
        var path = BezierPath(startPos, controlPos, endPos);
        return transform.DOPath(path, time).OnComplete(() => 
        {
            callback?.Invoke();
        }).SetEase(ease);
    }

    private static Vector3[] BezierPath(Vector3 startPos, Vector3 controlPos, Vector3 endPos, int count = 20)
    {
        var path = new Vector3[count];
        for(var i = 1; i <= count; i++)
        {
            var t = i * 1f / count * 1f;
            path[i-1] = Bezier(startPos, controlPos, endPos, t); 
        }
        return path;
    }

    private static Vector3 Bezier(Vector3 startPos, Vector3 controlPos, Vector3 endPos, float t)
    => (1 - t) * (1 - t) * startPos + 2 * t * (1 - t) * controlPos + t * t * endPos;
}


/// <summary>
/// 随机的钻石收集效果
/// </summary>
public class RandomDiamondEffect
{
    public static void ShowEffect(GameObject prefab, Transform parent, Vector3 startPos, Vector3 endPos, int siblingIndex = -1, int num = 20, Action onFinish = null)
    {
        var seq = DOTween.Sequence();
        var val = 300;
        for(var i = 0; i < num; ++i)
        {
            var item = GameObject.Instantiate(prefab);
            item.transform.SetParent(parent, false);
            if(siblingIndex!=-1)
                item.transform.SetSiblingIndex(siblingIndex);
            var randomTime = UnityEngine.Random.Range(0.4f, 0.6f);
            item.transform.position = startPos;
            var rPos = startPos + UnityEngine.Random.insideUnitSphere * 200;
            seq.Insert(0, item.transform.DOMove(rPos, randomTime)).SetEase(Ease.OutSine);
            var pos = startPos;
            var ctrolPos = new Vector3(pos.x + val, pos.y - val, pos.z);
            seq.Insert(randomTime, item.transform.DOBezier(startPos, ctrolPos, endPos, UnityEngine.Random.Range(.8f, 1.2f), callback: () => 
            {
                GameObject.Destroy(item);
                onFinish?.Invoke();
            }));
        }
        seq.SetUpdate(true);
        seq.AppendCallback(() => { onFinish?.Invoke(); });
    }
}
