using System;
using System.Collections;
using UnityEngine;

public sealed class TweenHandle : IDisposable
{
    MonoBehaviour mb;
    Coroutine coroutine;

    internal TweenHandle(MonoBehaviour mb, Coroutine coroutine)
    {
        this.mb = mb;
        this.coroutine = coroutine;
    }

    public bool IsRunning => coroutine != null;

    public void Stop()
    {
        if (mb != null && coroutine != null)
        {
            try { mb.StopCoroutine(coroutine); } catch { }
            coroutine = null;
        }
    }

    public void Dispose() => Stop();
}

public static class UnityTween
{
    // Common easing functions
    public static Func<float, float> Linear = t => t;
    public static Func<float, float> EaseInQuad = t => t * t;
    public static Func<float, float> EaseOutQuad = t => t * (2f - t);
    public static Func<float, float> EaseInOutQuad = t => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    public static Func<float, float> EaseInCubic = t => t * t * t;
    public static Func<float, float> EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

    public static TweenHandle TweenFloat(this MonoBehaviour mb, float from, float to, float duration, Action<float> onUpdate, Action onComplete = null, Func<float, float> easing = null)
    {
        if (mb == null) return null;
        if (onUpdate == null) return null;
        if (duration <= 0f)
        {
            onUpdate(to);
            onComplete?.Invoke();
            return new TweenHandle(mb, null);
        }
        easing ??= Linear;

        IEnumerator Routine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ev = easing(t);
                onUpdate(Mathf.LerpUnclamped(from, to, ev));
                yield return null;
            }
            onUpdate(to);
            try { onComplete?.Invoke(); } catch { }
        }

        var c = mb.StartCoroutine(Routine());
        return new TweenHandle(mb, c);
    }

    public static TweenHandle TweenVector3(this MonoBehaviour mb, Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate, Action onComplete = null, Func<float, float> easing = null)
    {
        if (mb == null) return null;
        if (onUpdate == null) return null;
        if (duration <= 0f)
        {
            onUpdate(to);
            onComplete?.Invoke();
            return new TweenHandle(mb, null);
        }
        easing ??= Linear;

        IEnumerator Routine()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ev = easing(t);
                onUpdate(Vector3.LerpUnclamped(from, to, ev));
                yield return null;
            }
            onUpdate(to);
            try { onComplete?.Invoke(); } catch { }
        }

        var c = mb.StartCoroutine(Routine());
        return new TweenHandle(mb, c);
    }

    // CanvasGroup fade helper
    public static TweenHandle FadeCanvasGroup(this MonoBehaviour mb, CanvasGroup cg, float from, float to, float duration, Action onComplete = null, Func<float, float> easing = null)
    {
        if (cg == null)
        {
            onComplete?.Invoke();
            return null;
        }
        return mb.TweenFloat(from, to, duration, v => cg.alpha = v, onComplete, easing);
    }

    // Transform scale helper
    public static TweenHandle Scale(this MonoBehaviour mb, Transform t, Vector3 from, Vector3 to, float duration, Action onComplete = null, Func<float, float> easing = null)
    {
        if (t == null)
        {
            onComplete?.Invoke();
            return null;
        }
        return mb.TweenVector3(from, to, duration, v => t.localScale = v, onComplete, easing);
    }
}
