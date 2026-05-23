using System;
using System.Collections.Generic;
public class MyCancellationToken
{
    private readonly object _gate = new();
    private Dictionary<long, Action> _callbacks;
    private long _nextId;
    private bool _cancelled;
    public bool IsCancellationRequested
    {
        get { lock (_gate) return _cancelled; }
    }
    public void Cancel()
    {
        List<Action> pending = null;
        lock (_gate)
        {
            if (_cancelled) return;
            _cancelled = true;
            if (_callbacks != null)
            {
                pending = new List<Action>(_callbacks.Values);
                _callbacks = null;
            }
        }
        if (pending != null)
        {
            List<Exception> errors = null;
            foreach (Action cb in pending)
            {
                try { cb(); }
                catch (Exception ex) { (errors ??= new List<Exception>()).Add(ex); }
            }
            if (errors != null)
                throw new AggregateException(errors);
        }
    }
    public long Register(Action callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        Action fireNow = null;
        long id = 0;
        lock (_gate)
        {
            if (_cancelled)
            {
                fireNow = callback;
            }
            else
            {
                id = ++_nextId;
                (_callbacks ??= new Dictionary<long, Action>())[id] = callback;
            }
        }
        fireNow?.Invoke();
        return id;
    }
    public bool Unregister(long id)
    {
        if (id == 0) return false;
        lock (_gate)
        {
            return _callbacks != null && _callbacks.Remove(id);
        }
    }
}