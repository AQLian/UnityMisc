using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public static class UnityUtil
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        if (go == null) return null;
        var comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
        }
        return comp;
    }

    public static string KeepOnlyChinese(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return Regex.Replace(input, @"[^\u4E00-\u9FFF]", "");
    }

    public static void DestroySafe(this Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Object.Destroy(obj);
        }
        else
        {
            Object.DestroyImmediate(obj);
        }
    }

    public static void DestroySafe(this GameObject go)
    {
        if (go == null) return;
        DestroySafe((Object)go);
    }

    public static void SetLayerRecursively(this GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i).gameObject;
            SetLayerRecursively(child, layer);
        }
    }

    public static Transform FindChildByName(this Transform parent, string name, bool includeInactive = false)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if ((includeInactive || child.gameObject.activeInHierarchy) && child.name == name)
                return child;
            var found = FindChildByName(child, name, includeInactive);
            if (found != null) return found;
        }
        return null;
    }

    public static T GetOrAddComponentInChildren<T>(this GameObject go) where T : Component
    {
        if (go == null) return null;
        var comp = go.GetComponentInChildren<T>(true);
        if (comp != null) return comp;
        return go.AddComponent<T>();
    }

    public static bool TryGetComponentInChildren<T>(this GameObject go, out T comp) where T : Component
    {
        comp = default;
        if (go == null) return false;
        comp = go.GetComponentInChildren<T>(true);
        return comp != null;
    }

    public static void SetActiveSafe(this GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf != active)
            go.SetActive(active);
    }

    public static T CloneComponent<T>(T original, GameObject destination) where T : Component
    {
        if (original == null || destination == null) return null;
        var type = original.GetType();
        var copy = destination.AddComponent(type) as T;
        // copy all fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            f.SetValue(copy, f.GetValue(original));
        }
        // copy writable properties
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite && p.CanRead);
        foreach (var p in props)
        {
            try
            {
                p.SetValue(copy, p.GetValue(original));
            }
            catch { }
        }
        return copy;
    }

    public static void SetAnchoredPosition(this RectTransform rt, Vector2 pos)
    {
        if (rt == null) return;
        rt.anchoredPosition = pos;
    }

    public static void ResetTransform(this Transform t)
    {
        if (t == null) return;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    // Vector helpers
    public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
    public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);
    public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);
    public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

    // Parent / transform helpers
    public static void SetParentKeepLocal(this GameObject child, Transform parent)
    {
        if (child == null) return;
        var t = child.transform;
        var localPos = t.localPosition;
        var localRot = t.localRotation;
        var localScale = t.localScale;
        t.SetParent(parent, worldPositionStays: false);
        t.localPosition = localPos;
        t.localRotation = localRot;
        t.localScale = localScale;
    }

    public static Transform GetTopmostParent(this Transform t)
    {
        if (t == null) return null;
        while (t.parent != null) t = t.parent;
        return t;
    }

    // Find helpers
    public static Transform FindInChildren(this GameObject go, System.Func<Transform, bool> predicate, bool includeInactive = false)
    {
        if (go == null || predicate == null) return null;
        var root = go.transform;
        return FindInChildrenInternal(root, predicate, includeInactive);
    }

    static Transform FindInChildrenInternal(Transform parent, System.Func<Transform, bool> predicate, bool includeInactive)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if ((includeInactive || child.gameObject.activeInHierarchy) && predicate(child))
                return child;
            var found = FindInChildrenInternal(child, predicate, includeInactive);
            if (found != null) return found;
        }
        return null;
    }

    // RectTransform helpers
    public static void SetSize(this RectTransform rt, Vector2 size)
    {
        if (rt == null) return;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    public static void SetPivotAndAnchors(this RectTransform rt, Vector2 pivot)
    {
        if (rt == null) return;
        rt.pivot = pivot;
        rt.anchorMin = pivot;
        rt.anchorMax = pivot;
    }

    // Color helpers
    public static Color ColorFromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length == 6)
        {
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                var r = ((val >> 16) & 0xFF) / 255f;
                var g = ((val >> 8) & 0xFF) / 255f;
                var b = (val & 0xFF) / 255f;
                return new Color(r, g, b, 1f);
            }
        }
        else if (hex.Length == 8)
        {
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val))
            {
                var a = ((val >> 24) & 0xFF) / 255f;
                var r = ((val >> 16) & 0xFF) / 255f;
                var g = ((val >> 8) & 0xFF) / 255f;
                var b = (val & 0xFF) / 255f;
                return new Color(r, g, b, a);
            }
        }
        return Color.white;
    }

    public static string ToHex(this Color c, bool includeAlpha = false)
    {
        int r = Mathf.RoundToInt(c.r * 255f);
        int g = Mathf.RoundToInt(c.g * 255f);
        int b = Mathf.RoundToInt(c.b * 255f);
        if (includeAlpha)
        {
            int a = Mathf.RoundToInt(c.a * 255f);
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", a, r, g, b);
        }
        return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
    }

    // LayerMask helpers
    public static bool Contains(this LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // Component/parent search
    public static T GetComponentInParents<T>(this GameObject go) where T : Component
    {
        if (go == null) return null;
        var t = go.transform;
        while (t != null)
        {
            var c = t.GetComponent<T>();
            if (c != null) return c;
            t = t.parent;
        }
        return null;
    }

    // Coroutine helper for MonoBehaviour to run a coroutine with a completion callback
    public static Coroutine StartCoroutineWithCallback(this MonoBehaviour mb, System.Collections.IEnumerator routine, System.Action onComplete)
    {
        if (mb == null) return null;
        return mb.StartCoroutine(RunRoutine());

        System.Collections.IEnumerator RunRoutine()
        {
            yield return mb.StartCoroutine(routine);
            try { onComplete?.Invoke(); } catch { }
        }
    }
}