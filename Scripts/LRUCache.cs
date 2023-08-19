using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class LRUCache<TKey, TValue> : IEnumerable<(TKey, TValue)>  
{
    private Dictionary<TKey, LinkedListNode<(TKey, TValue)>> m_Map;
    private LinkedList<(TKey, TValue)> m_LinkedList;

    public int Capacity
    {
        get; private set;
    }

    public LRUCache(int capacity)
    {
        m_Map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
        m_LinkedList = new LinkedList<(TKey, TValue)>();
        Capacity = capacity;
    }

    public TValue Get(TKey key)
    {
        if (m_Map.TryGetValue(key, out LinkedListNode<(TKey, TValue)> node)) 
        {
            m_LinkedList.Remove(node);
            m_LinkedList.AddFirst(node);
            return node.Value.Item2; 
        }
        return default;
    }

    public void Put(TKey key, TValue value)
    {
        if (m_Map.Count >= Capacity)
        {
            var firstItem = m_LinkedList.Last;
            m_Map.Remove(firstItem.Value.Item1);
            m_LinkedList.RemoveLast();
        }

        var newItem = (key, value);
        var newNode = new LinkedListNode<(TKey, TValue)>(newItem);
        if (!m_Map.ContainsKey(key))
        {
            m_Map.Add(key, newNode);
            m_LinkedList.AddFirst(newNode);
        }
        else
        {
            var old = m_Map[key];
            m_Map[key] = newNode;
            m_LinkedList.Remove(old);
            m_LinkedList.AddFirst(newNode);
        }
    }

    public IEnumerator<(TKey, TValue)> GetEnumerator()
    {
        return m_LinkedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
