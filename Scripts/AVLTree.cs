using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

/*
some basic concept:
1.balance factor: height of left sub tree minus height of right sub tree
2.if tree is not balance, rotation applied
3.basic rotation type
rotateleft
          1                      1
        1   1       balanced     
                     =>       1     1
          1    1            1   1     1  
                 1

rotateright
           1                       1
         1  1      balanced      1   1 
        1 1        =>           1   1  1  
       1
 */
public class AVLTree<T> where T : IComparable<T>
{
    public AVLTreeNode<T> m_Root;
    public int Count { get; private set; }


    public void AddRange(IEnumerable<T> keys)
    {
        foreach (var item in keys)
        {
            Add(item);
        }
    }

    public void Add(T key)
    {
        if (m_Root is null)
        {
            m_Root = new AVLTreeNode<T>(key);
        }
        else
        {
            m_Root = Add(m_Root, key);
        }
    }

    private static AVLTreeNode<T> Add(AVLTreeNode<T> node, T key)
    {
        var compare = key.CompareTo(node.key);

        if (compare > 0)
        {
            if (node.right is null)
            {
                node.right = new AVLTreeNode<T>(key);
            }
            else 
            {
                node.right = Add(node.right, key);
            }
        }
        else if (compare < 0)
        {
            if (node.left is null)
            {
                node.left = new AVLTreeNode<T>(key);
            }
            else
            {
                node.left = Add(node.left, key);
            }
        }
        else
        {
            throw new ArgumentException($"Already has key {key} in tree!");
        }

        node.UpdateBalanceFactor();

        return Rebalance(node);
    }

    public bool Contains(T key)
    {
        if (key is null)
        {
            throw new ArgumentNullException("key is null");
        }
        var node = m_Root;
        while(node is not null)
        {
            var compare = key.CompareTo(node.key);
            if (compare > 0)
            {
                node = node.right;
            }
            else if (compare < 0)
            {
                node = node.left;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    private static AVLTreeNode<T> Rebalance(AVLTreeNode<T> node)
    {
        if (node.factor > 1)
        {
            // two steps: rotate left and rotate right
            if (node.left.factor == -1)
            {
                node.left = RotateLeft(node.left);
            }

            node = RotateRight(node);
        }

        if (node.factor < -1)
        {
            // two steps: rotate right and rotate left
            if (node.right.factor == 1)
            {
                node.right = RotateRight(node.right);
            }

            node = RotateLeft(node);
        }

        return node;
    }

    private static AVLTreeNode<T> RotateLeft(AVLTreeNode<T> node)
    {
        var temp = node;
        var right = node.right;
        var rightLeft = node.right.left;
        node = right;
        node.left = temp;
        node.left.right = rightLeft;

        node.left.UpdateBalanceFactor();
        node.UpdateBalanceFactor();

        return node;
    }

    private static AVLTreeNode<T> RotateRight(AVLTreeNode<T> node)
    {
        var temp = node;
        var left = node.left;
        var leftRight = node.left.right;
        node = left;
        node.right = temp;
        node.right.left = leftRight;

        node.right.UpdateBalanceFactor();
        node.UpdateBalanceFactor();

        return node;
    }

    public T GetMin()
    {
        return GetMin(m_Root).key;
    }

    public T GetMax()
    {
        return GetMax(m_Root).key;
    }

    public List<T> InOrderTraverse()
    {
        var ret = new List<T>();
        Action(m_Root);
        return ret;

        void Action(AVLTreeNode<T> node)
        {
            if (node is null)
            {
                return;
            }
            Action(node.left);
            ret.Add(node.key);
            Action(node.right);
        }
    }

    private static AVLTreeNode<T> GetMin(AVLTreeNode<T> node)
    {
        while(node.left is not null)
        {
            node = node.left;
        }
        return node;
    }

    private static AVLTreeNode<T> GetMax(AVLTreeNode<T> node)
    {
        while(node.right is not null)
        {
            node = node.right;
        }
        return node;
    }
}

public class AVLTreeNode<T>
{
    public T key;
    public int factor;
    public int height;
    public AVLTreeNode<T> left;
    public AVLTreeNode<T> right;

    public AVLTreeNode(T key)
    {
        this.key = key;
    }

    public void UpdateBalanceFactor()
    {
        if (left is null && right is null)
        {
            height = 0;
            factor = 0;
        }
        else if (left is null)
        {
            height = right.height + 1;
            factor = -height;
        }
        else if (right is null)
        {
            height = left.height + 1;
            factor = height;
        }
        else
        {
            height = Math.Max(left.height, right.height) + 1;
            factor = left.height - right.height;
        }
    }
}