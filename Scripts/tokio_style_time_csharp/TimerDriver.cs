using System;
using System.Collections.Generic;
using System.Threading;

namespace TimerWheel;

/// ==========================================================================
/// TimerDriver — Simple background thread that drives a <see cref="TimerWheel"/>.
///
/// Polls the wheel, fires expired callbacks, and sleeps until the next
/// expiration.  This is intentionally kept minimal — the focus is on the
/// wheel algorithm itself.
/// ==========================================================================
public sealed class TimerDriver : IDisposable
{
    private readonly TimerWheel _wheel;
    private readonly object _lock = new();
    private readonly Thread _thread;
    private volatile bool _shutdown;

    /// <summary>The underlying timer wheel.</summary>
    public TimerWheel Wheel => _wheel;

    /// <summary>Fired when the driver thread stops.</summary>
    public event Action? OnStopped;

    public TimerDriver()
    {
        _wheel = new TimerWheel();
        _thread = new Thread(Run)
        {
            Name = "TimerDriver",
            IsBackground = true
        };
        _thread.Start();
    }

    /// <summary>
    /// Schedule a callback after <paramref name="delay"/> milliseconds.
    /// Returns a <see cref="WheelEntry"/> that can be used to cancel.
    /// </summary>
    public WheelEntry Schedule(long delayMs, Action<object?> callback, object? state = null)
    {
        var entry = new WheelEntry
        {
            When = Environment.TickCount64 + delayMs,
            Callback = callback,
            State = state
        };

        lock (_lock)
        {
            if (!_wheel.Insert(entry))
            {
                // Already elapsed — fire inline
                ThreadPool.UnsafeQueueUserWorkItem(_ => callback(state), null);
                entry.Consumed = true;
            }
        }

        return entry;
    }

    /// <summary>Cancel a previously scheduled timer.</summary>
    public void Cancel(WheelEntry entry)
    {
        lock (_lock)
        {
            _wheel.Remove(entry);
        }
    }

    /// <summary>Shut down the driver and wait for the thread to exit.</summary>
    public void Dispose()
    {
        _shutdown = true;
        _thread.Join(TimeSpan.FromSeconds(5));
        OnStopped?.Invoke();
    }

    // ---- driver loop -----------------------------------------------------

    private void Run()
    {
        while (!_shutdown)
        {
            WheelEntry? entry;

            lock (_lock)
            {
                long now = Environment.TickCount64;
                entry = _wheel.Poll(now);
            }

            // Fire entries outside the lock to avoid deadlocks
            if (entry is not null)
            {
                if (!entry.Consumed)
                {
                    entry.Consumed = true;
                    entry.Callback?.Invoke(entry.State);
                }
                continue; // keep draining without sleeping
            }

            // Determine how long to sleep
            long? nextWake;
            lock (_lock)
            {
                nextWake = _wheel.NextExpirationTime();
            }

            if (nextWake is not null)
            {
                long delay = nextWake.Value - Environment.TickCount64;
                if (delay > 0)
                    SleepMs(delay);
            }
            else
            {
                // No timers — sleep a bit then check again
                SleepMs(100);
            }
        }

        // Drain remaining entries on shutdown
        lock (_lock)
        {
            while (true)
            {
                long now = Environment.TickCount64;
                var entry = _wheel.Poll(now);
                if (entry is null) break;
                if (!entry.Consumed)
                {
                    entry.Consumed = true;
                    entry.Callback?.Invoke(entry.State);
                }
            }
        }
    }

    private static void SleepMs(long ms)
    {
        if (ms <= 0) return;
        // Clamp to reasonable range
        if (ms > 10_000) ms = 10_000;
        try { Thread.Sleep((int)ms); } catch (ThreadInterruptedException) { }
    }
}
