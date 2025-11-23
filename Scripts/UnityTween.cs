using System;
using System.Collections;
using UnityEngine;

public sealed class TweenHandle : IDisposable
{
    MonoBehaviour mb;
    Coroutine coroutine;
    TweenHandle inner;

    internal TweenHandle(MonoBehaviour mb, Coroutine coroutine)
    {
        this.mb = mb;
        this.coroutine = coroutine;
    }

    public bool IsRunning => coroutine != null || (inner != null && inner.IsRunning);

    internal void SetCoroutine(Coroutine c) => coroutine = c;
    internal void MarkCompleted() => coroutine = null;
    internal void SetInner(TweenHandle h) => inner = h;

    public void Stop()
    {
        if (inner != null)
        {
            try { inner.Stop(); } catch { }
            inner = null;
        }
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
    public static Func<float, float> EaseOutBounce = t =>
    {
        if (t < (1f / 2.75f))
            return 7.5625f * t * t;
        else if (t < (2f / 2.75f))
        {
            t -= (1.5f / 2.75f);
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < (2.5f / 2.75f))
        {
            t -= (2.25f / 2.75f);
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= (2.625f / 2.75f);
            return 7.5625f * t * t + 0.984375f;
        }
    };

    public static Func<float, float> EaseInBounce = t => 1f - EaseOutBounce(1f - t);
    public static Func<float, float> EaseInOutBounce = t => t < 0.5f ? (EaseInBounce(t * 2f) * 0.5f) : (EaseOutBounce(t * 2f - 1f) * 0.5f + 0.5f);

    public static Func<float, float> EaseOutElastic = t =>
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float p = 0.3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    };
    public static Func<float, float> EaseInElastic = t =>
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float p = 0.3f;
        t -= 1f;
        return -Mathf.Pow(2f, 10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p);
    };

    public static Func<float, float> EaseOutBack = t =>
    {
        float s = 1.70158f;
        t = t - 1f;
        return (t * t * ((s + 1f) * t + s) + 1f);
    };
    public static Func<float, float> EaseInBack = t =>
    {
        float s = 1.70158f;
        return t * t * ((s + 1f) * t - s);
    };

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

        var handle = new TweenHandle(mb, null);
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
            handle.MarkCompleted();
        }

        var c = mb.StartCoroutine(Routine());
        handle.SetCoroutine(c);
        return handle;
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

        var handle = new TweenHandle(mb, null);
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
            handle.MarkCompleted();
        }

        var c = mb.StartCoroutine(Routine());
        handle.SetCoroutine(c);
        return handle;
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

    // Sequence: run a set of tween factories in order. Each factory should start a tween and return its TweenHandle.
    public static TweenHandle Sequence(this MonoBehaviour mb, Func<TweenHandle>[] steps, Action onComplete = null)
    {
        if (mb == null) return null;
        if (steps == null || steps.Length == 0)
        {
            onComplete?.Invoke();
            return null;
        }
        var handle = new TweenHandle(mb, null);

        IEnumerator Routine()
        {
            foreach (var step in steps)
            {
                if (step == null) continue;
                var inner = step();
                handle.SetInner(inner);
                if (inner != null)
                {
                    while (inner.IsRunning)
                        yield return null;
                }
                else
                {
                    // nothing to wait for
                    yield return null;
                }
            }
            handle.MarkCompleted();
            try { onComplete?.Invoke(); } catch { }
        }

        var c = mb.StartCoroutine(Routine());
        handle.SetCoroutine(c);
        return handle;
    }

    // Parallel: run all factories simultaneously and wait until all complete
    public static TweenHandle Parallel(this MonoBehaviour mb, Func<TweenHandle>[] steps, Action onComplete = null)
    {
        if (mb == null) return null;
        if (steps == null || steps.Length == 0)
        {
            onComplete?.Invoke();
            return null;
        }
        var handle = new TweenHandle(mb, null);

        IEnumerator Routine()
        {
            var list = new System.Collections.Generic.List<TweenHandle>();
            foreach (var step in steps)
            {
                if (step == null) continue;
                var h = step();
                if (h != null) list.Add(h);
            }
            handle.SetInner(null);
            bool anyRunning = true;
            while (anyRunning)
            {
                anyRunning = false;
                foreach (var h in list)
                {
                    if (h != null && h.IsRunning) { anyRunning = true; break; }
                }
                yield return null;
            }
            handle.MarkCompleted();
            try { onComplete?.Invoke(); } catch { }
        }

        var c = mb.StartCoroutine(Routine());
        handle.SetCoroutine(c);
        return handle;
    }
}
