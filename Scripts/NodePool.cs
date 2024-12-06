#nullable enable
using System;
using System.Linq;
using System.Threading;

namespace PoolNode
{
    public class Node<T> : IDisposable
    {
        public T value { get; set; } = default!;
        internal Node<T>? next { get; set; }

        internal NodePool<T>? Pool;

        public void Dispose()
        {
            Pool?.Return(this);
        }
    }

    /// <summary>
    /// thread safe pool of nodes with value T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NodePool<T> 
    {
        private Node<T>? _head; 
        private readonly Func<T>? _valueFactory;
        private readonly object _lock = new();
        
        public NodePool(Func<T>? valueFactory = null)
        {
            _valueFactory = valueFactory;
        }

        public Node<T> Get()
        {
            Node<T>? node;

            lock(_lock)
            {
                node = _head;

                if (node != null)
                {
                    _head = node.next;
                    node.Pool = this;
                    countPooled--;
                }
            }

            if (node == null)
            {
                node = new Node<T>
                {
                    value = _valueFactory != null ? _valueFactory() : default!,
                    Pool = this
                };
                Interlocked.Increment(ref _countAll);
            }

            node.next = null;
            return node;
        }

        public void Return(Node<T> node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            node.value = default!;
            lock(_lock)
            {
                node.next = _head;
                _head = node;
                countPooled++;
            }
        }

        private int _countAll;
        public int countAll => _countAll;

        public int countPooled { get; private set; }
    }
}