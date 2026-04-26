using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public static class DOBezierExtensions
{
    public static Tweener DOBezier(this Transform transform,
    Vector3 startPos, 
    Vector3 controlPos, 
    Vector3 endPos, 
    float time, 
    Ease ease = Ease.InQuad, 
    Action callback = null,
    int count = 20)
    {
        if (!transform)
        {
            return null;
        }
        DOTween.Kill(transform);
        var path = BezierPath(startPos, controlPos, endPos, count);
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
/// 注意需要autokill启用否则也用不了
/// </summary>
public static class DoTweenUtil
{
    /// <summary>
    /// 检查一组tweener是否正常完成，正常完成是指没有被kill掉，如果被kill掉了说明是非正常完成，可能是被外部强行kill掉了，也可能是因为对象被销毁了导致的kill掉了
    /// </summary>
    /// <param name="stateNotify"></param>
    /// <param name="ts"></param> <summary>
    public static void NormalCompletedListener(Action<bool> stateNotify, params Tweener[] ts)
    {
        var normalCounter = ts.Length;
        var len = ts.Length;
        foreach (var t in ts)
        {
            var existingComplete = t.onComplete;
            t.OnComplete(existingComplete += () =>
            {
                normalCounter--;
            });
            var existingKill = t.onKill;
            t.OnKill(existingKill += () =>
            {
                len--;
                if (len == 0)
                {
                    stateNotify(normalCounter == 0);
                }
            });
        }
    }

    public static void DOPunchScale(Transform transform, float punch = .3f, float duration = .5f, float elasticity=.5f)
    {
        transform.DOPunchScale(
            punch: new Vector3(punch, punch, 0), // XY 方向最大放大 30%
            duration: duration,
            vibrato: 1,                        // 仅一次来回
            elasticity: elasticity             // 回弹力度，0=无弹性，1=完全弹性
        ).SetEase(Ease.OutQuad);
    }

    public static void DoBreathEffect(Transform transform, float scaleTarget = 1.2f, float duration = .5f)
    {
        transform.localScale = Vector3.one;
        transform.DOScale(
            endValue: new Vector3(scaleTarget, scaleTarget, 1f),
            duration: duration
        )
        .SetLoops(-1, LoopType.Yoyo)   // 无限循环，来回模式
        .SetEase(Ease.InOutSine);      // 慢进慢出，更自然
    }
}

/// <summary>
/// 随机的收集效果，可以创建一个实例后面反复使用，内部有池子会服用创建后的移动对象
/// </summary>
public class DiamondCollectEffect : IDisposable
{
    private ObjectPool<GameObject> m_itemPool;

    public Vector2 ControlPointOffset = new Vector2(300, -300);

    public DiamondCollectEffect(GameObject prefab)
    {
        m_itemPool = new ObjectPool<GameObject>(
        createFunc: ()=> GameObject.Instantiate(prefab),
        actionOnGet: g => g.SetActive(true),
        actionOnRelease: g => g.SetActive(false),
        collectionCheck: false,
        defaultCapacity: 20,
        actionOnDestroy: g =>
        {
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(g);
            }
            else
            {
                GameObject.Destroy(g);
            }
        });
    }

    public void Dispose()
    {
        m_itemPool?.Dispose();
        m_itemPool=null;
    }

    public void ShowEffect(
        Transform parent, 
        Vector3 startPos, 
        Vector3 endPos, 
        int siblingIndex = -1, 
        int num = 20, 
        Action onItemFinish=null,
        Action onAllFinish = null)
    {
        var seq = DOTween.Sequence();
        for(var i = 0; i < num; ++i)
        {
            var item = m_itemPool.Get();
            item.transform.SetParent(parent, false);
            if (siblingIndex!=-1)
                item.transform.SetSiblingIndex(siblingIndex);
            var randomTime = UnityEngine.Random.Range(0.4f, 0.6f);
            item.transform.position = startPos;
            var rPos = startPos + UnityEngine.Random.insideUnitSphere * 200;
            var move = item.transform.DOMove(rPos, randomTime);
            seq.Insert(0, move).SetEase(Ease.OutSine);
            var pos = startPos;
            var ctrolPos = new Vector3(pos.x + ControlPointOffset.x, pos.y + ControlPointOffset.y, pos.z);
            Action claimRes= () =>
            {
                m_itemPool.Release(item);
                onItemFinish?.Invoke();
            };
            var bezier = item.transform.DOBezier(startPos, ctrolPos, endPos, UnityEngine.Random.Range(.8f, 1.2f), callback: claimRes);
            seq.Insert(randomTime, bezier);
            //完全有可能非正常Complete，需要处理异常
            DoTweenUtil.NormalCompletedListener(normal => {
                if (!normal)
                {
                    claimRes();
                }
            }, move, bezier);
        }
        seq.SetUpdate(true);
        seq.AppendCallback(() => { onAllFinish?.Invoke(); });
    }
}
