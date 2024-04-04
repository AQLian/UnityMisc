using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;

public class BinarySearchTree<T>  where T : IComparable<T>
{
    private BSTNode<T> m_Root;

    public BSTNode<T> Root
    {
        get { return m_Root; }
    }

    public int Count
    {
        get; private set;
    }

    public void Add(IEnumerable<T> values)
    {
        foreach(var v in values)
        {
            Add(v);
        }
    }

    public void Add(T value)
    {
        if (m_Root == null)
        {
            m_Root = new BSTNode<T>();
            m_Root.value = value;
            m_Root.count = 1;
        }
        else
        {
            void insertNewNode(ref BSTNode<T> newNode, T v)
            {
                newNode = new BSTNode<T>
                {
                    value = v,
                    count = 1
                };
            }

            var node = m_Root;
            while (node != null)
            {
                if (node.value.CompareTo(value) == 0)
                {
                    node.count++;
                    break;
                }
                else if(node.value.CompareTo(value) > 0)
                {
                    if (node.left != null)
                    {
                        node = node.left;
                    }
                    else
                    {
                        insertNewNode(ref node.left, value);
                        break;
                    }
                }
                else
                {
                    if (node.right != null)
                    {
                        node = node.right;
                    }
                    else
                    {
                        insertNewNode(ref node.right, value);
                        break;
                    }
                }
            }
        }
        Count++;
    }

    public void Remove(T value)
    {
        InternalFindAndDeleteNode(ref m_Root, value);
    }

    public BSTNode<T> Find(T value)
    {
        return FindInternal(m_Root, value);
    }

    public bool Contains(T value)
    {
        return Find(value) != default(BSTNode<T>);
    }

    private BSTNode<T> FindInternal(BSTNode<T> node, T value)
    {
        if (node == null)
        {
            return default;
        }

        var compareResult = node.value.CompareTo(value);
        if ( compareResult == 0)
        {
            return node;
        }

        if (compareResult > 0)
        {
            return FindInternal(node.left, value);
        }
        else 
        {
            return FindInternal(node.right, value);
        }
    }

    private void InternalFindAndDeleteNode(ref BSTNode<T> node, T value)
    {
        if (node == null)
        {
            return;
        }

        if (node.value.CompareTo(value) == 0)
        {
            InternalDeleteNode(ref node);
            Count--;
        }
        else if (node.value.CompareTo(value) < 0)
        {
            InternalFindAndDeleteNode(ref node.right, value);
        }
        else
        {
            InternalFindAndDeleteNode(ref node.left, value);
        }
    }


    private void InternalDeleteNode(ref BSTNode<T> toDelete)
    {
        toDelete.count--;
        if (toDelete.left == null && toDelete.right == null)
        {
            if (toDelete.count == 0)
            {
                toDelete = null;
            }
        }
        else if (toDelete.left == null && toDelete.ChildCount() == 1)
        {
            if (toDelete.count == 0)
            {
                toDelete.value = toDelete.right.value;
                toDelete.count = toDelete.right.count;
                toDelete.right = null;
            }
        }
        else if (toDelete.right == null && toDelete.ChildCount() == 1)
        {
            if (toDelete.count == 0)
            {
                toDelete.value = toDelete.left.value;
                toDelete.count = toDelete.left.count;
                toDelete.left = null;
            }
        }
        else
        {
            // delete right in-order first element
            // or left in-order last element ?
            if (toDelete.count == 0)
            {
                var parent = toDelete;
                var target = toDelete.right;
                while(target.left != null)
                {
                    parent = target;
                    target = target.left;
                }

                var targetValue = target.value;
                if (target.ChildCount() == 0)
                {
                    if (parent.left == target)
                    {
                        parent.left = null;
                    }    
                    else if(parent.right == target )
                    {
                        parent.right = null;
                    }
                }

                var targetCount = target.count;
                target.count = 1;
                InternalDeleteNode(ref target);
                toDelete.value = targetValue;
                toDelete.count = targetCount;
            }
        }
    }

    public void PreOrderTraverse(Action<BSTNode<T>> action)
    {
        InternalPreOrderTraverse(m_Root, action);
    }

    private void InternalPreOrderTraverse(BSTNode<T> node, Action<BSTNode<T>> action)
    {
        if (node != null)
        {
            action?.Invoke(node);
            InternalPreOrderTraverse(node.left, action);
            InternalPreOrderTraverse(node.right, action);
        }
    }

    public void InOrderTraverse(Action<BSTNode<T>> action)
    {
        InternalInOrderTraverse(m_Root, action);
    }

    private void InternalInOrderTraverse(BSTNode<T> node, Action<BSTNode<T>> action)
    {
        if (node != null)
        {
            InternalInOrderTraverse(node.left, action);
            action?.Invoke(node);
            InternalInOrderTraverse(node.right, action);
        }
    }

    public void PostOrderTraverse(Action<BSTNode<T>> action)
    {
        InternalPostTraverse(m_Root, action);
    }

    private void InternalPostTraverse(BSTNode<T> node, Action<BSTNode<T>> action)
    {
        if (node != null)
        {
            InternalPostTraverse(node.left, action);
            InternalPostTraverse(node.right, action);
            action?.Invoke(node);
        }
    }
}

public class BSTNode<T>
{
    public T value;
    public BSTNode<T> left;
    public BSTNode<T> right;
    public int count;    // same value just increase count, not need to add new node

    public override string ToString()
    {
        return $"node value: {value} count: {count}";
    }
}

public static class NodeExtension
{
    public static int ChildCount<T>(this BSTNode<T> node) where T : IComparable<T>
    {
        int count = 0;
        InternalChildCount<T>(node, ref count);
        return count;
    }

    private static void InternalChildCount<T>(this BSTNode<T> node, ref int count) where T : IComparable<T>
    {
        if (node == null)
        {
            return;
        }

        if (node.left != null && node.right == null)
        {
            count++;
            InternalChildCount(node.left, ref count);
        }
        else if (node.left == null && node.right != null)
        {
            count++;
            InternalChildCount(node.right, ref count);
        }
        else if (node.left != null && node.right != null)
        {
            count += 2;
            InternalChildCount(node.left, ref count);
            InternalChildCount(node.right, ref count);
        }
    }
}
