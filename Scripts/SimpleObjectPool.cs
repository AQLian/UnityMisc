using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.UI;

public static class ListExtensions
{
    public static T RemoveLast<T>(this List<T> list) where T : new()
    {
        var listCount = list.Count;
        if (listCount > 0)
        {
            var last = list[listCount -1];
            list.RemoveAt(listCount - 1);
            return last;
        }

        return default(T);
    }

    public static void SwapRemove<T>(this List<T> list, int toRemove) where T : new()
    {
        var listCount = list.Count;
        if (listCount > 0 && toRemove < listCount)
        {
            var last = list[listCount - 1];
            var item = list[toRemove];
            list[listCount - 1] = item;
            list[toRemove] = last;
            RemoveLast(list);
        }
    }
}

public class SimplePool<T> where T : new()
{
    private List<T> m_PoolItems = new List<T>();
    // item -> index map
    private Dictionary<T, int> m_ItemIndexMap = new Dictionary<T, int>();

    public int AllCount { get; private set; }

    public int InActiveCount { get {  return m_PoolItems.Count; } }

    public int ActiveCount { get { return AllCount - m_PoolItems.Count; } }

    public int MaxInActiveCount { get; }

    private Action<T> m_ActionOnGet;
    private Action<T> m_ActionOnRelease;


    public SimplePool(Action<T> actionOnGet, Action<T> actionOnRelease, int maxPoolItemCount = -1)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
        MaxInActiveCount = maxPoolItemCount;
    }

    public T Get()
    {
        T item = default(T);
        if (m_PoolItems.Count > 0)
        {
            item = m_PoolItems.RemoveLast();
            m_ItemIndexMap.Remove(item);

            if (item is UnityEngine.Object u && u == null)
            {
                AllCount--;
                while (m_PoolItems.Count > 0)
                {
                    item = m_PoolItems.RemoveLast();
                    m_ItemIndexMap.Remove(item);
                    if (item is UnityEngine.Object next && next == null)
                    {
                        AllCount--;
                    }
                    else
                    {
                        m_ActionOnGet?.Invoke(item);
                        return item;
                    }
                }
            }
            else
            {
                m_ActionOnGet?.Invoke(item);
                return item;
            }
        }


        AllCount++;
        item = new T();
        m_ActionOnGet?.Invoke(item);
        return item;
    }

    private void RemovePoolItem(T item)
    {
        if (m_ItemIndexMap.TryGetValue(item, out int index))
        {
            var lastCount = m_PoolItems.Count - 1;
            var lastItem = m_PoolItems[lastCount];
        
            m_PoolItems.SwapRemove(index);
            m_ItemIndexMap.Remove(item);

            // reset index!!!
            if (index != lastCount)
            {
                m_ItemIndexMap[lastItem] = index;
            }

            return true;
        }
        return false;
    }

    public void Release(T item)
    {
        if (item == null)
        {
            Debug.Log("Can not poo null item!");
            return;
        }

        if (m_ItemIndexMap.ContainsKey(item))
        {
            Debug.Log($"item {item} already pooled!");
            return;
        }

        if (item is UnityEngine.Object o && o == null)
        {
            Debug.Log("try to relase unityobject has destroyed!");
            return;
        }

        if (m_PoolItems.Count >= MaxCount && MaxCount > 0)
        {
            RemovePoolItem(m_PoolItems[m_PoolItems.Count - 1]);
            AllCount--;
        }

        m_ActionOnRelease?.Invoke(item);
        m_PoolItems.Add(item);
        m_ItemIndexMap[item] = m_PoolItems.Count - 1;
    }
}
