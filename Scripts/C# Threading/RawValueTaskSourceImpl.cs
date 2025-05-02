using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class CustomValueTaskSource : IValueTaskSource<int>
{
    private ValueTaskSourceStatus _state = ValueTaskSourceStatus.Pending;
    private int _result;
    private Exception _exception;
    private Action<object> _continuation;
    private object _stateObject;
    private short _token;
    private CancellationTokenRegistration _cancellationRegistration;

    public CustomValueTaskSource()
    {
        _token = 0;
    }

    public ValueTask<int> GetAsyncValueTask(CancellationToken cancellationToken = default)
    {
        if (_state != ValueTaskSourceStatus.Pending)
        {
            throw new InvalidOperationException("ValueTaskSource is not in a pending state");
        }

        if (cancellationToken.CanBeCanceled)
        {
            _cancellationRegistration = cancellationToken.Register(
            state => ((CustomValueTaskSource)state).CancelInternal(), this);
        }

        return new ValueTask<int>(this, _token);
    }

    public void Complete(int result)
    {
        if (_state != ValueTaskSourceStatus.Pending)
        {
            throw new InvalidOperationException("Operation already completed, faulted, or canceled");
        }

        _result = result;
        _state = ValueTaskSourceStatus.Succeeded;
        CleanupCancellation();
        TriggerContinuation();
    }

    public void Complete(Exception error)
    {
        if (_state != ValueTaskSourceStatus.Pending)
        {
            throw new InvalidOperationException("Operation already completed, faulted, or canceled");
        }

        _exception = error;
        _state = ValueTaskSourceStatus.Faulted;
        CleanupCancellation();
        TriggerContinuation();
    }

    public void Cancel()
    {
        CancelInternal();
    }

    private void CancelInternal()
    {
        if (_state != ValueTaskSourceStatus.Pending)
        {
            return;
        }

        _exception = new OperationCanceledException();
        _state = ValueTaskSourceStatus.Canceled;
        CleanupCancellation();
        TriggerContinuation();
    }

    public void Reset()
    {
        _state = ValueTaskSourceStatus.Pending;
        _result = 0;
        _exception = null;
        _continuation = null;
        _stateObject = null;
        CleanupCancellation();
        _token = unchecked((short)(_token + 1));
    }

    private void CleanupCancellation()
    {
        _cancellationRegistration.Dispose();
        _cancellationRegistration = default;
    }

    private void TriggerContinuation()
    {
        var continuation = _continuation;
        var state = _stateObject;
        _continuation = null;
        _stateObject = null;
        continuation?.Invoke(state);
    }

    public int GetResult(short token)
    {
        if (_token != token)
        {
            throw new InvalidOperationException("Token mismatch");
        }

        if (_state == ValueTaskSourceStatus.Pending)
        {
            throw new InvalidOperationException("Operation is still pending");
        }

        if (_state == ValueTaskSourceStatus.Faulted)
        {
            ExceptionDispatchInfo.Capture(_exception).Throw();
        }

        if (_state == ValueTaskSourceStatus.Canceled)
        {
            ExceptionDispatchInfo.Capture(_exception ?? new OperationCanceledException()).Throw();
        }

        return _result;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        if (_token != token)
        {
            throw new InvalidOperationException("Token mismatch");
        }

        return _state;
    }

    public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (_token != token)
        {
            throw new InvalidOperationException("Token mismatch");
        }

        if (_state != ValueTaskSourceStatus.Pending)
        {
            Task.Run(() => continuation(state));
            return;
        }

        _continuation = continuation;
        _stateObject = state;
    }
}

public class Program
{
    public static async Task Main()
    {
        var pool = new Queue<CustomValueTaskSource>();
        pool.Enqueue(new CustomValueTaskSource());

        // Test 1: Cancellation
        async Task TestCancellation()
        {
            var source = pool.Dequeue();
            try
            {
                using var cts = new CancellationTokenSource(100);
                ValueTask<int> task = source.GetAsyncValueTask(cts.Token);

                try
                {
                    int result = await task;
                    Console.WriteLine("This line should not be reached");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Test 1: Operation was canceled as expected");
                }

                source.Reset();
                pool.Enqueue(source);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test 1: Unexpected error - {ex.Message}");
            }
        }

        // Test 2: Sequential access
        async Task TestSequentialAccess(int id)
        {
            var source = pool.Dequeue();
            try
            {
                ValueTask<int> task = source.GetAsyncValueTask();
                await Task.Delay(50);
                source.Complete(id * 100);
                int result = await task;
                Console.WriteLine($"Task {id}: Completed with result {result}");

                source.Reset();
                pool.Enqueue(source);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task {id}: Error - {ex.Message}");
            }
        }

        // Test 3: Token mismatch and reuse
        async Task TestTokenMismatchAndReuse()
        {
            var source = pool.Dequeue();
            try
            {
                ValueTask<int> task = source.GetAsyncValueTask();
                source.Complete(42);
                int result = await task;
                Console.WriteLine($"Test 3: First result: {result}");

                source.Reset();

                try
                {
                    result = await task;
                    Console.WriteLine("This line should not be reached");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Test 3: Expected token mismatch: {ex.Message}");
                }

                var newTask = source.GetAsyncValueTask();
                source.Complete(999);
                result = await newTask;
                Console.WriteLine($"Test 3: Second result: {result}");

                pool.Enqueue(source);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test 3: Unexpected error - {ex.Message}");
            }
        }

        await TestCancellation();
        await TestSequentialAccess(1);
        await TestSequentialAccess(2);
        await TestSequentialAccess(3);
        await TestTokenMismatchAndReuse();
    }
}