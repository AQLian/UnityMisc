using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TimerWheel;

/// ==========================================================================
/// WheelEntry — A single timer entry stored in the wheel.
/// ==========================================================================
public class WheelEntry
{
    /// <summary>Absolute expiration tick (millisecond).</summary>
    public long When;

    /// <summary>Callback to invoke when this timer fires.</summary>
    public Action<object?>? Callback;

    /// <summary>User state passed to the callback.</summary>
    public object? State;

    /// <summary>Next entry in the slot's singly-linked list.</summary>
    public WheelEntry? Next;

    /// <summary>
    /// Set to true when the entry has been consumed (fired, cancelled, or
    /// removed).  The driver skips consumed entries.
    /// </summary>
    public volatile bool Consumed;
}

/// ==========================================================================
/// Expiration — Describes which slot must be processed next and its deadline.
/// ==========================================================================
internal readonly struct Expiration
{
    public readonly int Level;
    public readonly int Slot;
    public readonly long Deadline;

    public Expiration(int level, int slot, long deadline)
    {
        Level = level;
        Slot = slot;
        Deadline = deadline;
    }
}

/// ==========================================================================
/// TimerLevel — A single level of the hierarchical wheel (64 slots).
///
/// Level 0:  1 ms/slot   →  64 ms  range
/// Level 1: 64 ms/slot   →  ~4 s   range
/// Level 2: ~4 s/slot    →  ~4 min range
/// Level 3: ~4 min/slot  →  ~4 hr  range
/// Level 4: ~4 hr/slot   →  ~12 d  range
/// Level 5: ~12 d/slot   →  ~2 yr  range
/// ==========================================================================
internal sealed class TimerLevel
{
    public const int SlotCount = 64; // LEVEL_MULT — must be a power of two

    public readonly int Index;
    public readonly long SlotRange;   // ms per slot  = 64^index
    public readonly long LevelRange;  // ms per level = 64^(index+1)

    /// <summary>Bitmask: bit i is set when slot i is non-empty.</summary>
    public ulong Occupied;

    /// <summary>The 64 slots.  Each slot is the head of a singly-linked list.</summary>
    public readonly WheelEntry?[] Slots;

    public TimerLevel(int index)
    {
        Index = index;
        SlotRange = Pow64(index);
        LevelRange = SlotRange * SlotCount;
        Slots = new WheelEntry?[SlotCount];
    }

    // ---- helpers ---------------------------------------------------------

    /// <summary>64 ^ n  (n is small, result fits in long for n=0..5)</summary>
    private static long Pow64(int n)
    {
        long v = 1;
        for (int i = 0; i < n; i++) v *= 64;
        return v;
    }

    /// <summary>
    /// Slot index for a raw millisecond value in this level.
    /// Equivalent to Tokio's <c>slot_for(duration, level)</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SlotFor(long when) => (int)(((ulong)when >> (Index * 6)) % SlotCount);

    /// <summary>
    /// Computes the next slot that contains entries, along with its deadline.
    /// Returns <c>null</c> when <see cref="Occupied"/> is 0.
    ///
    /// Matches <c>Level::next_expiration</c> in Tokio.
    /// </summary>
    public Expiration? NextExpiration(long now)
    {
        if (Occupied == 0)
            return null;

        // Current slot position within this level
        int nowSlot = (int)((now / SlotRange) % SlotCount);

        // Rotate the occupancy mask so that bit 0 maps to nowSlot
        ulong rotated = BitOperations.RotateRight(Occupied, nowSlot);
        int zeros = BitOperations.TrailingZeroCount(rotated);
        int slot = (zeros + nowSlot) % SlotCount;

        // Compute the deadline from the slot index
        long levelStart = now & ~(LevelRange - 1);
        long deadline = levelStart + slot * SlotRange;

        // On the top level we may wrap around (pseudo ring-buffer behaviour)
        if (deadline <= now && Index == TimerWheel.NumLevels - 1)
            deadline += LevelRange;

        Debug.Assert(deadline > now,
            $"deadline={deadline}, now={now}, level={Index}, slot={slot}");

        return new Expiration(Index, slot, deadline);
    }

    /// <summary>Add an entry to the correct slot and mark it occupied.</summary>
    public void Add(WheelEntry entry)
    {
        int slot = SlotFor(entry.When);
        entry.Next = Slots[slot];
        Slots[slot] = entry;
        Occupied |= 1UL << slot;
    }

    /// <summary>Remove a specific entry from its slot.</summary>
    public bool Remove(WheelEntry entry)
    {
        int slot = SlotFor(entry.When);
        WheelEntry? prev = null;
        var cur = Slots[slot];

        while (cur is not null)
        {
            if (cur == entry)
            {
                if (prev is null)
                    Slots[slot] = cur.Next;
                else
                    prev.Next = cur.Next;

                if (Slots[slot] is null)
                    Occupied &= ~(1UL << slot);

                return true;
            }
            prev = cur;
            cur = cur.Next;
        }

        return false;
    }

    /// <summary>
    /// Take ALL entries out of the given slot, clear the occupied bit,
    /// and return them as a list (pop order ≈ LIFO for efficiency).
    /// </summary>
    public List<WheelEntry> TakeSlot(int slot)
    {
        Occupied &= ~(1UL << slot);

        var entries = new List<WheelEntry>();
        var cur = Slots[slot];
        while (cur is not null)
        {
            var next = cur.Next;
            cur.Next = null;
            entries.Add(cur);
            cur = next;
        }
        Slots[slot] = null;
        return entries;
    }
}

/// ==========================================================================
/// TimerWheel — Hierarchical hashed timer wheel (Tokio-style).
///
/// 6 levels × 64 slots.  Tracks timers with 1 ms precision up to ~2.2 years.
///
/// Thread safety: the wheel itself is NOT thread-safe.  The caller must
/// serialise access (the included <see cref="TimerDriver"/> does this via a
/// simple lock).
/// ==========================================================================
public sealed class TimerWheel
{
    public const int NumLevels = 6;

    /// <summary>Maximum duration representable by the wheel (ms).</summary>
    public static readonly long MaxDuration = (1L << 36) - 1; // (1 << (6*6)) - 1

    private long _elapsed;
    private readonly TimerLevel[] _levels;

    /// <summary>Entries whose deadline has been reached, waiting to be fired.</summary>
    private readonly Queue<WheelEntry> _pending = new();

    /// <summary>Milliseconds elapsed since the wheel was created.</summary>
    public long Elapsed => Volatile.Read(ref _elapsed);

    public TimerWheel()
    {
        _levels = new TimerLevel[NumLevels];
        for (int i = 0; i < NumLevels; i++)
            _levels[i] = new TimerLevel(i);
    }

    // ======================================================================
    //  level_for  —  Port of Tokio's `level_for(elapsed, when)`
    //
    //  XOR the two values, find the most-significant differing bit, and
    //  divide its position by 6 to get the level index.
    // ======================================================================
    public static int LevelForStatic(long elapsed, long when)
    {
        const long SlotMask = TimerLevel.SlotCount - 1; // 63

        long masked = (elapsed ^ when) | SlotMask;

        if (masked >= MaxDuration)
            masked = MaxDuration - 1;

        int leadingZeros = BitOperations.LeadingZeroCount((ulong)masked);
        int significant = 63 - leadingZeros;
        return significant / 6;
    }

    /// <summary>Instance-level shortcut using current <see cref="Elapsed"/>.</summary>
    public int LevelFor(long when) => LevelForStatic(Elapsed, when);

    // ======================================================================
    //  Insert / Remove
    // ======================================================================

    /// <summary>
    /// Insert a timer.  Returns <c>false</c> if the deadline has already
    /// passed (the caller should fire the callback immediately).
    /// </summary>
    public bool Insert(WheelEntry entry)
    {
        long now = Elapsed;

        if (entry.When <= now)
            return false;

        int level = LevelFor(entry.When);
        _levels[level].Add(entry);
        return true;
    }

    /// <summary>
    /// Remove a timer.  Returns <c>true</c> if successfully removed.
    /// If the entry has already been consumed or its deadline passed,
    /// returns <c>false</c>.
    /// </summary>
    public bool Remove(WheelEntry entry)
    {
        if (entry.Consumed)
            return false;

        long now = Elapsed;
        if (entry.When <= now)
            return false;

        int level = LevelFor(entry.When);
        if (_levels[level].Remove(entry))
        {
            entry.Consumed = true;
            return true;
        }

        return false;
    }

    // ======================================================================
    //  Poll  —  Advance time and collect expired entries
    // ======================================================================
    /// <summary>
    /// Poll the wheel up to <paramref name="now"/> (ms).
    /// Returns the next expired entry, or <c>null</c> if none are ready.
    ///
    /// Call repeatedly until <c>null</c> to drain all expired entries.
    /// </summary>
    public WheelEntry? Poll(long now)
    {
        while (true)
        {
            // 1.  Return a pending entry immediately (checked each iteration).
            if (_pending.TryDequeue(out var ready))
                return ready;

            // 2.  Find the next expiration slot.
            var expiration = FindNextExpiration();

            if (expiration is not null && expiration.Value.Deadline <= now)
            {
                ProcessExpiration(expiration.Value);
                SetElapsed(expiration.Value.Deadline);
                // Loop again — if ProcessExpiration added to pending, step 1
                // will pick it up on the next iteration.
            }
            else
            {
                SetElapsed(now);
                break;
            }
        }

        // 3.  Try to return a freshly-expired entry
        _pending.TryDequeue(out var result);
        return result;
    }

    // ======================================================================
    //  NextExpirationTime  —  when the next timer fires (or null)
    // ======================================================================
    /// <summary>
    /// Absolute instant (ms) of the next expiration, or <c>null</c>
    /// if no timers are registered.
    /// </summary>
    public long? NextExpirationTime()
    {
        if (_pending.Count > 0)
            return Elapsed;

        for (int i = 0; i < NumLevels; i++)
        {
            var exp = _levels[i].NextExpiration(Elapsed);
            if (exp is not null)
                return exp.Value.Deadline;
        }

        return null;
    }

    /// <summary>True when the wheel has no timers (including pending).</summary>
    public bool IsEmpty
    {
        get
        {
            if (_pending.Count > 0) return false;
            for (int i = 0; i < NumLevels; i++)
                if (_levels[i].Occupied != 0) return false;
            return true;
        }
    }

    // ======================================================================
    //  Internal helpers
    // ======================================================================

    private Expiration? FindNextExpiration()
    {
        // Pending entries take priority
        if (_pending.Count > 0)
            return new Expiration(0, 0, Elapsed);

        for (int i = 0; i < NumLevels; i++)
        {
            var exp = _levels[i].NextExpiration(Elapsed);
            if (exp is not null)
                return exp;
        }

        return null;
    }

    /// <summary>
    /// Process an expiration slot:
    ///  - Level 0 → entries are ready to fire (move to pending).
    ///  - Level N > 0 → re-insert entries at the correct lower level
    ///    (this is the cascade / tier-down mechanism).
    /// </summary>
    private void ProcessExpiration(Expiration exp)
    {
        var entries = _levels[exp.Level].TakeSlot(exp.Slot);

        // Iterate in reverse so we behave like pop_back (unimportant
        // for correctness, matching Tokio's iterator order).
        for (int idx = entries.Count - 1; idx >= 0; idx--)
        {
            var entry = entries[idx];
            if (entry.Consumed)
                continue;

            if (exp.Level == 0)
            {
                // This timer's deadline has arrived — queue it for firing.
                _pending.Enqueue(entry);
            }
            else
            {
                // Re-insert into the wheel.
                // Use the expiration deadline as the base for level_for
                // (the same as Tokio: level_for(expiration.deadline, when)).
                // This cascades the entry down to the correct lower level.
                int newLevel = LevelForStatic(exp.Deadline, entry.When);
                _levels[newLevel].Add(entry);
            }
        }
    }

    private void SetElapsed(long when)
    {
        Debug.Assert(Elapsed <= when,
            $"elapsed={Elapsed} when={when}");
        if (when > Elapsed)
            Volatile.Write(ref _elapsed, when);
    }
}
