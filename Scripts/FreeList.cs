using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEditor;
using UnityEngine;
using System;


/// <summary>
/// free list implemented in C# List<typeparamref name="T"/>
/// feature:
/// 1. index 0 item just using for reusing pooling head, so return handle starting from 1
/// 2. removed item just back to pool for reusing
/// todo: reimplemented using LinkedList<typeparamref name="T"/> as data backup
/// </summary>
/// <typeparam name="T"></typeparam>
public class FreeList<T>
{
    public class ListNode<U>
    {
        public int index;
        public U data;
        public bool isFree;

        public ListNode(int index, U value)
        {
            this.index = index;
            this.data = value;
            isFree = false;
        }

        public override string ToString()
        {
            return $"node index {index} data: {data} isFree {isFree}";
        }
    }

    private List<ListNode<T>> m_List;
    private ListNode<T> m_Head;
    private int m_Count;
    private Action<T> m_ActionOnGet;
    private Action<T> m_ActionOnRelease;

    public int FreeCount
    {
        get {
            var count = 0;
            var node = m_Head;
            while(node.index != 0)
            {
                node = m_List[node.index];
                count++;
            }
            return count;
        }
    }

    public int AllCount
    {

        get
        {
            return m_Count - 1;
        }
    }

    public int ActiveCount
    {
        get
        {
            return AllCount - FreeCount;
        }
    }


    public FreeList(int initCapacity, Action<T> actionOnGet = null, Action<T> actionOnRelease = null)
    {
        m_List = new List<ListNode<T>>(initCapacity);
        m_Head = new ListNode<T>(0, default);
        m_List.Add(m_Head);
        m_Count = m_List.Count;
        m_ActionOnGet = actionOnGet;
        m_ActionOnRelease = actionOnRelease;
    }

    /// <summary>
    /// return index as handle to 
    /// retrive data later
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public int Add(T data)
    {
        var node = m_Head;
        if (node.index != 0)
        {
            var freeIndex = node.index;
            var reusing = m_List[freeIndex];
            node.index = reusing.index;
            reusing.data = data;
            reusing.isFree = false;
            return freeIndex;
        }
        else
        {
            var pos = m_List.Count;
            m_ActionOnGet?.Invoke(data);
            m_List.Add(new ListNode<T>(pos, data));
            m_Count ++;
            return pos;
        }
    }

    public bool TryGet(int index, out T data)
    {
        if (index > 0 && index < m_Count)
        {
            data = m_List[index].data;
            return true;
        }

        data = default;
        return false;
    }

    public bool TryRelease(int index, out T data)
    {
        data = default;
        if (index > 0 && index < m_Count)
        {
            var node = m_List[index];
            if (!node.isFree)
            {
                data = node.data;
                m_ActionOnRelease?.Invoke(data);
                node.isFree = true;
                node.index = m_Head.index;
                m_Head.index = index;
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// make data at index to default
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public object Destroy(int index)
    {
        if (index > 0 && index < m_Count)
        {
            var node = m_List[index];
            var data = node.data;
            m_List[index].data = default;
            return data;
        }

        return null;
    }

    public IEnumerable<ListNode<T>> EnumerateFreeNode()
    {
        var node = m_Head;

        while(node.index != 0)
        {
            node = m_List[node.index];
            yield return node;
        }
    }
}
