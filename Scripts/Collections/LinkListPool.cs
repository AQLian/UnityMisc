public interface IPoolListNode<T> where T : class
{
    ref T NextNode { get; }
}

public class LinkListPool<T> where T : class, IPoolListNode<T>, new()
{
    private static T _sentinel = new();
    private T _top = _sentinel;

    public int CountAll { get; private set; }
    public int InactiveCount { get; private set; }
    public int ActiveCount { get{ return CountAll - InactiveCount; } }
    
    public T Acquire()
    {
        if (_top == _sentinel)
        {
            CountAll++;
            return new T();
        }

        var item = _top;
        _top = _top.NextNode;
        if (_top == null)
        {
            _top = _sentinel;
        }
        ref var nextItem = ref item.NextNode;
        nextItem = null;
        InactiveCount--;
        return item;
    }

    public bool Release(T node)
    {
        ref var refNode = ref node.NextNode;
        if (refNode != null)
        {
            return false;
        }

        refNode = _top;
        _top = node;
        InactiveCount++;
        return true;
    }
}