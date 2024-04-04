using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// downside: using more memory to store pooled object
/// upside: double pool check more efficient
/// </summary>
/// <typeparam name="T"></typeparam>
public class UniqueObjectPool<T> where T : new()
{
    private Stack<T> m_PoolItems = new Stack<T>();
    private HashSet<T> m_UniquePoolItems = new HashSet<T>();

    public int AllCount { get; private set; }

    public int InActiveCount { get {  return m_PoolItems.Count; } }

    public int ActiveCount { get { return AllCount - m_PoolItems.Count; } }

    public int MaxPooledCount { get; }

    private Action<T> m_ActionOnGet;
    private Action<T> m_ActionOnRelease;


    public UniqueObjectPool(Action<T> actionOnGet, Action<T> actionOnRelease, int maxPoolItemCount = -1)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
        MaxPooledCount = maxPoolItemCount;
    }

    public T Get()
    {
        T item;
        if (m_PoolItems.Count > 0)
        {
            item = m_PoolItems.Pop();
            m_ActionOnGet?.Invoke(item);
            return item;
        }

        AllCount++;
        item = new T();
        m_ActionOnGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null)
        {
            Debug.Log("Can not poo null item!");
            return;
        }

        if (m_UniquePoolItems.Contains(item))
        {
            Debug.Log($"item {item} already pooled!");
            return;
        }

        if (m_PoolItems.Count >= MaxPooledCount && MaxPooledCount > 0)
        {
            var rm = m_PoolItems.Pop();
            m_UniquePoolItems.Remove(rm);
            AllCount--;
        }

        m_ActionOnRelease?.Invoke(item);
        m_PoolItems.Push(item);
        m_UniquePoolItems.Add(item);
    }
}
