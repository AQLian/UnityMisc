using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ValueTaskDemo
{
    // Custom IValueTaskSource implementation
    public class CustomValueTaskSource : IValueTaskSource<int>
    {
        private ManualResetEventSlim _signal = new ManualResetEventSlim();
        private Action<object> _continuation;
        private object _continuationState;
        private short _token;
        private int _result;
        private bool _completed;

        // Reset the state for a new operation
        public void Reset()
        {
            _signal.Reset();
            _continuation = null;
            _continuationState = null;
            _token = unchecked((short)(_token + 1)); // Increment token for uniqueness
            _result = 0;
            _completed = false;
        }

        // Simulate an async operation completion
        public void Complete(int result)
        {
            _result = result;
            _completed = true;
            _signal.Set();

            // Invoke continuation if registered
            if (_continuation != null)
            {
                _continuation(_continuationState);
            }
        }

        // IValueTaskSource implementation
        public int GetResult(short token)
        {
            // Validate token
            if (token != _token)
                throw new InvalidOperationException("Invalid token");

            // Wait for completion if not yet completed
            _signal.Wait();

            return _result;
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (token != _token)
                throw new InvalidOperationException("Invalid token");

            if (_completed)
                return ValueTaskSourceStatus.Succeeded;
            return ValueTaskSourceStatus.Pending;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (token != _token)
                throw new InvalidOperationException("Invalid token");

            // Store continuation and state
            _continuation = continuation;
            _continuationState = state;

            // If already completed, invoke continuation immediately
            if (_completed)
            {
                continuation(state);
            }
        }

        // Helper to create a ValueTask
        public ValueTask<int> GetValueTask()
        {
            return new ValueTask<int>(this, _token);
        }
    }

    class Program
    {
        static async Task Main()
        {
            var source = new CustomValueTaskSource();

            // Create a ValueTask
            ValueTask<int> task = source.GetValueTask();

            // Simulate an async operation that completes after 2 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Simulate work
                source.Complete(42); // Complete with result 42
            });

            // Await the ValueTask
            int result = await task;
            Console.WriteLine($"Result: {result}"); // Output: Result: 42

            // Reuse the same source (reset for new operation)
            source.Reset();
            task = source.GetValueTask();

            // Simulate another async operation
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                source.Complete(100);
            });

            result = await task;
            Console.WriteLine($"Result: {result}"); // Output: Result: 100
        }
    }
}