using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Collections.Concurrent;

namespace ValueTaskSourcePoolDemo
{
    // Custom IValueTaskSource implementation for the pool
    public class PooledValueTaskSource : IValueTaskSource<bool>
    {
        private ManualResetValueTaskSourceCore<bool> _core = new ManualResetValueTaskSourceCore<bool>();
        private short _currentToken; // Current token for this instance
        private bool _isInUse; // Tracks if this instance is in use

        public bool IsInUse => _isInUse;

        // Initialize or reset the instance for a new operation
        public void Initialize(short token)
        {
            _currentToken = token;
            _isInUse = true;
            _core.Reset();
        }

        // Mark the instance as free for reuse
        public void Release()
        {
            _isInUse = false;
        }

        // Complete the operation
        public void SetResult(bool result)
        {
            _core.SetResult(result);
        }

        // IValueTaskSource<bool> implementation
        public bool GetResult(short token)
        {
            if (token != _currentToken)
                throw new InvalidOperationException("Invalid token");
            return _core.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (token != _currentToken)
                throw new InvalidOperationException("Invalid token");
            return _core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (token != _currentToken)
                throw new InvalidOperationException("Invalid token");
            _core.OnCompleted(continuation, state, token, flags);
        }
    }

    // Pool for managing reusable PooledValueTaskSource instances
    public class ValueTaskSourcePool
    {
        private readonly ConcurrentBag<PooledValueTaskSource> _pool;
        private readonly int _maxPoolSize;
        private short _nextToken; // Monotonic token counter

        public ValueTaskSourcePool(int maxPoolSize)
        {
            _maxPoolSize = maxPoolSize;
            _pool = new ConcurrentBag<PooledValueTaskSource>();
            _nextToken = 0;
        }

        // Rent a PooledValueTaskSource from the pool
        public (PooledValueTaskSource Source, short Token, ValueTask<bool> Task) Rent()
        {
            if (!_pool.TryTake(out var source))
            {
                if (_pool.Count < _maxPoolSize)
                {
                    source = new PooledValueTaskSource();
                }
                else
                {
                    throw new InvalidOperationException("Pool exhausted");
                }
            }

            // Generate a new token (thread-safe increment)
            short token = Interlocked.Increment(ref _nextToken);
            if (token == short.MaxValue)
            {
                // Handle token overflow (rare in practice)
                throw new InvalidOperationException("Token overflow");
            }

            // Initialize the source with the token
            source.Initialize(token);

            // Create a ValueTask for the operation
            var task = new ValueTask<bool>(source, token);

            return (source, token, task);
        }

        // Return a PooledValueTaskSource to the pool
        public void Return(PooledValueTaskSource source)
        {
            source.Release();
            _pool.Add(source);
        }
    }

    // Demo program
    class Program
    {
        static async Task Main()
        {
            // Create a pool with a maximum of 10 sources
            var pool = new ValueTaskSourcePool(maxPoolSize: 10);

            // Simulate multiple async operations
            Task[] tasks = new Task[5];
            PooledValueTaskSource[] sources = new PooledValueTaskSource[5];

            for (int i = 0; i < 5; i++)
            {
                // Rent a source from the pool
                var (source, token, task) = pool.Rent();
                sources[i] = source;

                // Start a task that completes the operation after a delay
                tasks[i] = Task.Run(async () =>
                {
                    await Task.Delay(1000); // Simulate work
                    source.SetResult(true); // Complete the operation
                    pool.Return(source); // Return the source to the pool
                });

                // Await the ValueTask
                bool result = await task;
                Console.WriteLine($"Operation {i + 1} completed with token {token}, result: {result}");
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            Console.WriteLine("All operations completed.");
        }
    }
}