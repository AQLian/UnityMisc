using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TimerWheel
{
    public sealed class TimerHandle
    {
        internal Action CancelAction;
        internal WheelEntry CurrentWheelEntry;

        public void Cancel()
        {
            CancelAction?.Invoke();
            CancelAction = null;
        }

        public bool Cancelled { get; internal set; }
    }

    public sealed class TimerManager : MonoBehaviour
    {
        static TimerManager _instance;
        public static TimerManager Instance => _instance;

        TimerWheel _wheel;
        readonly Dictionary<WheelEntry, Action> _pending = new();

        readonly ObjectPool<WheelEntry> _entryPool = new(
            createFunc: () => new WheelEntry(),
            actionOnGet: e => { e.Consumed = false; e.Next = null; },
            actionOnRelease: e => { e.Callback = null; e.State = null; },
            collectionCheck: true
        );

        void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _wheel = new TimerWheel();
        }

        void Update()
        {
            long now = NowMs();

            while (true)
            {
                var wheelEntry = _wheel.Poll(now);
                if (wheelEntry is null) break;

                if (_pending.TryGetValue(wheelEntry, out var callback))
                {
                    _pending.Remove(wheelEntry);
                    callback();
                    _entryPool.Release(wheelEntry);
                }
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        static long NowMs()
        {
            return (long)(Time.realtimeSinceStartupAsDouble * 1000.0);
        }

        public TimerHandle Schedule(long delayMs, Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var handle = new TimerHandle();
            long when = NowMs() + delayMs;
            var wheelEntry = _entryPool.Get();
            wheelEntry.When = when;

            _pending[wheelEntry] = () =>
            {
                if (handle.Cancelled) return;
                callback();
            };

            handle.CancelAction = () =>
            {
                if (handle.Cancelled) return;
                handle.Cancelled = true;
                _wheel.Remove(wheelEntry);
                _pending.Remove(wheelEntry);
                _entryPool.Release(wheelEntry);
            };

            handle.CurrentWheelEntry = wheelEntry;
            InsertOrFire(wheelEntry);
            return handle;
        }

        public TimerHandle Interval(long intervalMs, Action callback, bool immediate = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (intervalMs <= 0) throw new ArgumentException("intervalMs must be > 0");

            var handle = new TimerHandle();

            Action tick = () =>
            {
                if (handle.Cancelled) return;
                callback();
                ScheduleNextTick(intervalMs, handle, tick);
            };

            handle.CancelAction = () =>
            {
                if (handle.Cancelled) return;
                handle.Cancelled = true;
                ReturnCurrentEntry(handle);
            };

            if (immediate)
                tick();
            else
                ScheduleNextTick(intervalMs, handle, tick);

            return handle;
        }

        void ScheduleNextTick(long intervalMs, TimerHandle handle, Action tick)
        {
            if (handle.Cancelled) return;

            long when = NowMs() + intervalMs;
            var wheelEntry = _entryPool.Get();
            wheelEntry.When = when;

            ReturnCurrentEntry(handle);
            handle.CurrentWheelEntry = wheelEntry;

            _pending[wheelEntry] = () =>
            {
                if (handle.Cancelled) return;
                tick();
            };

            InsertOrFire(wheelEntry);
        }

        void ReturnCurrentEntry(TimerHandle handle)
        {
            if (handle.CurrentWheelEntry == null) return;
            _pending.Remove(handle.CurrentWheelEntry);
            _entryPool.Release(handle.CurrentWheelEntry);
            handle.CurrentWheelEntry = null;
        }

        void InsertOrFire(WheelEntry wheelEntry)
        {
            if (!_wheel.Insert(wheelEntry))
            {
                if (_pending.TryGetValue(wheelEntry, out var cb))
                {
                    _pending.Remove(wheelEntry);
                    cb();
                    _entryPool.Release(wheelEntry);
                }
            }
        }
    }
}
