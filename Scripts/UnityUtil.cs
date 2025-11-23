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
}