# Tokio-style Hierarchical Timer Wheel — Deep Dive

This document explains the timer wheel data structure that powers Tokio's (and now
this C# port's) high-performance timer management.  It assumes you are reading the
source at `TimerWheel.cs` side by side.

---

## Table of Contents

1. [The Problem — Why a Timer Wheel?](#1-the-problem--why-a-timer-wheel)
2. [Single-Level Timer Wheel](#2-single-level-timer-wheel)
3. [The Hierarchy — Six Levels](#3-the-hierarchy--six-levels)
4. [Data Structures](#4-data-structures)
5. [Slot Calculation (`slot_for`)](#5-slot-calculation-slot_for)
6. [Level Determination (`level_for`)](#6-level-determination-level_for)
7. [Finding the Next Expiration](#7-finding-the-next-expiration)
8. [Inserting a Timer](#8-inserting-a-timer)
9. [Polling & Cascading](#9-polling--cascading)
10. [Worked Example — Step by Step](#10-worked-example--step-by-step)
11. [Edge Cases](#11-edge-cases)
12. [Time Complexity](#12-time-complexity)
13. [Comparison with Alternatives](#13-comparison-with-alternatives)

---

## 1. The Problem — Why a Timer Wheel?

Imagine you are building an async runtime.  Tasks call `sleep(50ms)`, `sleep(3s)`,
`sleep(2h)`.  You need to track thousands — or millions — of timers and wake them
up exactly when their deadlines arrive.

**Naive approaches:**

| Approach | Insert | Find next | Total (N timers) |
|----------|--------|-----------|-------------------|
| Sorted list / binary heap | O(log N) | O(1) | Good, but heap bubble for every insert |
| Linear scan | O(1) | O(N) | Terrible at scale |
| Per-timer OS timer | O(1) | O(1) | OS limit, context-switch heavy |

**Timer wheel:** O(1) insert, O(1) find-next-expiration, O(1) amortised per-tick.
It achieves this by exploiting the fact that time is linear and bounded.

> Reference: George Varghese and Tony Lauck, *"Hashed and Hierarchical Timing
> Wheels: Efficient Data Structures for Implementing a Timer Facility"*, 1997.

---

## 2. Single-Level Timer Wheel

A single-level wheel is a circular array of **N slots**.  Each slot covers a fixed
time interval (the **slot range** or **granularity**).  A "clock hand" advances one
slot per tick.

```
             ┌───┐
        ┌───▶│ 0 │◀── tick 0, 64, 128...
        │    ├───┤
        │    │ 1 │◀── tick 1, 65, 129...
        │    ├───┤
  clock │    │ 2 │
  hand ─┤    ├───┤
        │    │...│
        │    ├───┤
        │    │63 │◀── tick 63, 127, 191...
        └────┤   │
             └───┘
```

**Example:** 64 slots, 1 ms each → covers 0..63 ms.  A timer for *t=50* is stored
in slot 50.  When the hand reaches slot 50, all timers in that slot are fired.

**Problem:** A single level only covers `N × granularity`.  With 64 slots at 1 ms,
you can only schedule up to 64 ms ahead.  For a 2-hour timer you would need
`7 200 000` slots — impractical.

---

## 3. The Hierarchy — Six Levels

Tokio (and this C# port) uses **six hierarchical levels**, each with 64 slots.
Higher levels have coarser granularity:

| Level | Slots per level | Slot range (granularity) | Level range (total coverage) | Notes |
|:-----:|:---------------:|:------------------------:|:----------------------------:|-------|
| 0 | 64 | 1 ms | 64 ms | Sub-millisecond precision (rounded) |
| 1 | 64 | 64 ms | ~4.1 s | |
| 2 | 64 | ~4.1 s | ~4.4 min | |
| 3 | 64 | ~4.4 min | ~4.7 hr | |
| 4 | 64 | ~4.7 hr | ~12.4 days | |
| 5 | 64 | ~12.4 days | ~2.2 years | |

**Why exactly 64?**  64 = 2⁶, a power of two.  All slot and level calculations
become bitwise shifts and masks — no expensive division.

**Formulas:**

```
slot_range(level)  = 64 ^ level                    (C#: TimerLevel.SlotRange)
level_range(level) = 64 × slot_range(level)        (C#: TimerLevel.LevelRange)
max_duration       = 64⁶ − 1 ≈ 68.7 billion ms    (C#: TimerWheel.MaxDuration)
```

The key insight: **each level is equivalent to one rotation of the level below**.
When level 0 wraps around (64 ms elapsed), one slot of level 1 is consumed.
When level 1 wraps around, one slot of level 2 is consumed.  And so on.

---

## 4. Data Structures

### `WheelEntry`

```csharp
public class WheelEntry
{
    public long When;            // Absolute expiration tick (ms)
    public Action<object?>? Callback;
    public object? State;
    public WheelEntry? Next;     // Singly-linked list node
    public volatile bool Consumed;
}
```

Entries form a **singly-linked list** per slot (not `LinkedList<T>` — we avoid
heap allocations for list nodes by embedding the `Next` pointer in the entry).

### `TimerLevel`

```csharp
internal class TimerLevel
{
    public ulong Occupied;          // 64-bit bitmask, one bit per slot
    public readonly WheelEntry?[] Slots;  // 64 slot heads
    public readonly long SlotRange;       // ms per slot
    public readonly long LevelRange;      // ms per level
}
```

- **`Occupied`** is a bitmask — bit *i* is 1 when slot *i* has entries.
  This allows O(1) "is there ANY timer in this level?" and O(1) "which is the
  first non-empty slot after the current position?"

- **`Slots`** is an array of 64 linked-list heads.  `null` means empty.

### `TimerWheel`

```csharp
public class TimerWheel
{
    private long _elapsed;                    // Current time (ms)
    private readonly TimerLevel[] _levels;    // 6 levels
    private readonly Queue<WheelEntry> _pending;  // Ready-to-fire entries
}
```

---

## 5. Slot Calculation (`slot_for`)

Which slot within a level does a given absolute deadline belong to?

```csharp
public int SlotFor(long when) => (int)(((ulong)when >> (Index * 6)) % 64);
```

**Intuition:** divide `when` by the level's granularity, then take modulo 64.

Since `slot_range(level) = 64^level = 2^(6×level)`, dividing by it is equivalent
to a **right shift by `6 × level`** bits.  Modulo 64 is `& 63`.

| Level | Shift | Equivalent to |
|:-----:|:-----:|---------------|
| 0 | `>> 0` | `when % 64` |
| 1 | `>> 6` | `(when / 64) % 64` |
| 2 | `>> 12` | `(when / 4096) % 64` |
| n | `>> 6n` | `(when / 64ⁿ) % 64` |

**Example:** `when = 5000`, level = 2.

```
5000 >> 12   =   5000 / 4096   = 1
1 % 64       =   1
→ slot 1 of level 2
```

Slot 1 of level 2 covers absolute time range `[1×4096, 2×4096)` = `[4096, 8192)` ms.
5000 falls in this range.  ✓

---

## 6. Level Determination (`level_for`)

Which level should a new timer be inserted into?  This is the **most elegant**
part of the algorithm.

```csharp
public static int LevelForStatic(long elapsed, long when)
{
    const long SlotMask = 63;
    long masked = (elapsed ^ when) | SlotMask;
    if (masked >= MaxDuration)
        masked = MaxDuration - 1;
    int leadingZeros = BitOperations.LeadingZeroCount((ulong)masked);
    int significant = 63 - leadingZeros;
    return significant / 6;
}
```

### Step-by-step logic

**Step 1:** `elapsed ^ when` — XOR finds the **most significant bit that
differs** between current time and the deadline.

**Step 2:** `| SlotMask` (| 63) — ensures the bottom 6 bits are always 1.
This "caps" the precision to 6-bit granularity, preventing a timer 1 ms away
from being placed in level 2+ just because the 6th bit differs.

**Step 3:** `LeadingZeroCount` — finds how many leading zeros the masked
value has.  In a 64-bit unsigned integer, `63 - leading_zeros` gives the position
of the most significant set bit (0-indexed from LSB).

**Step 4:** `significant / 6` — divide by 6 to convert bit position to level index.

### Why this works

A timer belongs in level *k* when its remaining duration `(when − elapsed)` is in
the range `[64ᵏ, 64ᵏ⁺¹)`.  This means the most significant bit that differs
between `elapsed` and `when` sits at position `6k` through `6k+5`.

Dividing by 6 maps bit positions to levels:

| Remaining duration | MSB position | MSB/6 = level |
|--------------------|:------------:|:-------------:|
| 1 .. 63 ms | 0 .. 5 | 0 |
| 64 .. 4095 ms | 6 .. 11 | 1 |
| 4096 .. 262143 ms | 12 .. 17 | 2 |
| ... | ... | ... |

### Worked example

```
elapsed = 0, when = 5000

0 ^ 5000      = 5000  (binary: 1001110001000, 13 bits, MSB at position 12)
5000 | 63     = 5055  (binary: 1001110111111, bottom 6 bits forced to 1)
leading_zeros = 64 - 13 = 51
significant   = 63 - 51 = 12
level         = 12 / 6 = 2
```

Indeed, 5000 falls in level 2's range `[4096, 262144)`.  ✓

```
elapsed = 0, when = 50

0 ^ 50        = 50    (binary: 110010, 6 bits, MSB at position 5)
50 | 63       = 63    (binary: 111111, bottom 6 bits all 1)
leading_zeros = 58
significant   = 5
level         = 5 / 6 = 0
```

50 falls in level 0's range `[1, 64)`.  ✓

---

## 7. Finding the Next Expiration

The `TimerLevel.NextExpiration(now)` method answers: *"Which slot will expire next,
and at what absolute time?"* — in **O(1)**.

### The algorithm

```csharp
public Expiration? NextExpiration(long now)
{
    if (Occupied == 0) return null;

    // 1. Current position of the clock hand within this level
    int nowSlot = (int)((now / SlotRange) % 64);

    // 2. Rotate the occupancy bitmask so bit 0 aligns with nowSlot
    ulong rotated = BitOperations.RotateRight(Occupied, nowSlot);

    // 3. Count trailing zeros → first occupied slot >= nowSlot
    int zeros = BitOperations.TrailingZeroCount(rotated);
    int slot = (zeros + nowSlot) % 64;

    // 4. Compute the absolute deadline
    long levelStart = now & ~(LevelRange - 1);  // Round down to level boundary
    long deadline = levelStart + slot * SlotRange;

    // 5. Handle top-level wrap-around (pseudo ring-buffer)
    if (deadline <= now && Index == NumLevels - 1)
        deadline += LevelRange;

    return new Expiration(Index, slot, deadline);
}
```

### Visualisation

```
Occupied bitmask (64 bits, bit 0 = LSB, bit 63 = MSB):

  bit:  63  ...  nowSlot+3  nowSlot+2  nowSlot+1  nowSlot  nowSlot-1  ...  0
  value: 0        1          0          0          1         0             0

  After RotateRight by nowSlot:

  bit:  63  ...  3  2  1  0  63  ...  nowSlot+1
  value: 0       1  0  0  1  0         0
                    ↑
              TrailingZeroCount = 1 → first occupied slot after nowSlot is at offset 1
              Actual slot = (1 + nowSlot) % 64
```

### Why `RotateRight` + `TrailingZeroCount`?

A naive approach would loop from `nowSlot` to `nowSlot+63`, checking each slot.
That is O(64) per lookup — acceptable but not optimal.

With the bitmask rotation:
1. `RotateRight` puts `nowSlot` at bit position 0.
2. `TrailingZeroCount` is a **single CPU instruction** (TZCNT / BSF on x86) that
   directly gives the distance to the first set bit.

This is **O(1)** with extremely low constant cost.

### Deadline calculation

```
levelStart = now & ~(LevelRange - 1)
```

This rounds `now` **down** to the nearest level boundary.  For level 0 (range 64):
`now = 50` → `50 & ~63 = 0`.  For level 2 (range 262144): `now = 100000` →
`100000 & ~262143 = 0`.

Then `deadline = levelStart + slot × slot_range`.  For level 0 slot 10:
`0 + 10 × 1 = 10`.

---

## 8. Inserting a Timer

```csharp
public bool Insert(WheelEntry entry)
{
    long now = Elapsed;
    if (entry.When <= now)
        return false;           // Already elapsed — caller fires immediately

    int level = LevelFor(entry.When);
    _levels[level].Add(entry);  // Push onto the slot's linked list + set Occupied bit
    return true;
}
```

1. Check if deadline already passed.
2. Compute the appropriate level via `LevelFor`.
3. Compute the slot via `SlotFor`.
4. Prepend to the slot's singly-linked list.
5. Set the corresponding bit in `Occupied`.

**Cost: O(1)** — no heap bubbling, no tree rebalancing.

---

## 9. Polling & Cascading

This is the heart of the wheel.  `Poll(now)` advances time and collects expired timers.

```csharp
public WheelEntry? Poll(long now)
{
    while (true)
    {
        // ① Return a pending entry immediately (checked every iteration)
        if (_pending.TryDequeue(out var ready))
            return ready;

        // ② Find the next expiration across all levels
        var expiration = FindNextExpiration();

        if (expiration != null && expiration.Deadline <= now)
        {
            // ③ Process that expiration slot
            ProcessExpiration(expiration);
            SetElapsed(expiration.Deadline);
            // Loop back to ① — ProcessExpiration may have added to _pending
        }
        else
        {
            // ④ No expirations <= now — advance to now and stop
            SetElapsed(now);
            break;
        }
    }

    _pending.TryDequeue(out var result);
    return result;
}
```

### Key behaviour

**Time jumps directly to deadlines.**  If the next expiration is at t=5000 and
`now` is 6000, `_elapsed` jumps from (say) 100 to 5000 (processes that slot),
then potentially to 6000 — it does NOT tick 1 ms at a time.

**Pending queue acts as a buffer.**  When `ProcessExpiration` finds level-0
entries, it enqueues them into `_pending` rather than returning them directly.
The next `while` iteration's step ① immediately dequeues and returns.

### `ProcessExpiration` — the cascade

```csharp
private void ProcessExpiration(Expiration exp)
{
    var entries = _levels[exp.Level].TakeSlot(exp.Slot);

    for (int idx = entries.Count - 1; idx >= 0; idx--)
    {
        var entry = entries[idx];
        if (entry.Consumed) continue;

        if (exp.Level == 0)
        {
            _pending.Enqueue(entry);    // Ready to fire!
        }
        else
        {
            // Re-insert at a LOWER level (cascade)
            int newLevel = LevelForStatic(exp.Deadline, entry.When);
            _levels[newLevel].Add(entry);
        }
    }
}
```

### Why this is called "cascading"

Timers are initially placed into the coarsest level that can hold them.
A 5-second timer goes into level 2.  When time advances to level 2's slot
boundary, we **don't fire the timer yet** — we move it to level 1.
Later, it moves to level 0.  Only when it reaches level 0 is it actually fired.

```
   Level 2 (coarse)          Level 1                Level 0 (fine)
  ┌────┬────┬────┐         ┌────┬────┐            ┌────┬────┬────┐
  │    │███ │    │  ───▶   │    │███ │   ───▶    │    │    │███ │  ───▶  FIRE!
  └────┴────┴────┘         └────┴────┘            └────┴────┴────┘
   t=4096                t=4992                 t=5000

   Timer inserted at level 2 (when=5000, slot 1, deadline 4096)
   → cascaded to level 1 (deadline 4992)
   → cascaded to level 0 (deadline 5000)
   → fired
```

This is analogous to the gears of a mechanical clock — the hour hand drives the
minute hand, which drives the second hand.

### TakeSlot — atomic removal

```csharp
public List<WheelEntry> TakeSlot(int slot)
{
    Occupied &= ~(1UL << slot);    // Clear the occupied bit
    var entries = new List<WheelEntry>();
    var cur = Slots[slot];
    while (cur != null)
    {
        var next = cur.Next;
        cur.Next = null;
        entries.Add(cur);
        cur = next;
    }
    Slots[slot] = null;
    return entries;
}
```

We take **all** entries from the slot atomically.  This is important: some
entries might be re-inserted into the same slot (in the top-level wrap-around
case), and we must not re-process the newly inserted entries in the same
expiration cycle.

---

## 10. Worked Example — Step by Step

Let's trace a complete scenario:

```
Insert: when=30   (L0), when=2000 (L1), when=10000 (L2)
Initial state: elapsed=0
```

### State after insertion

| Level | Occupied bits | Slot → entry |
|:-----:|:-------------:|--------------|
| 0 | bit 30 | slot 30 → L0(when=30) |
| 1 | bit 31 | slot 31 → L1(when=2000) |
| 2 | bit 2 | slot 2 → L2(when=10000) |
| 3..5 | 0 | empty |

### `Poll(now=50)`

```
while true:
    ① pending empty
    ② FindNextExpiration:
       Level 0: Occupied≠0 → NextExpiration(now=0):
         nowSlot=0, rotated=Occupied, TZC=30, slot=30
         levelStart=0 & ~63 = 0, deadline=0+30=30
       → expiration (level=0, slot=30, deadline=30)
    ③ 30 <= 50 → ProcessExpiration:
       TakeSlot(level=0, slot=30) → [L0]
       exp.Level==0 → pending.Enqueue(L0)
       SetElapsed(30)
    Loop back to ①
    ① pending has L0 → return L0 ✓
```

`Poll(50)` called again → pending empty →
FindNextExpiration:
- Level 0: Occupied=0
- Level 1: NextExpiration(now=30):
  nowSlot=0, rotated=bit31, TZC=31, slot=31
  levelStart=30&~4095=0, deadline=0+31×64=1984
  1984 > 50 → stop

Poll returns null.  `PollAll(wheel, 50)` → [L0].

### `Poll(now=3000)`

```
while true:
    ① pending empty
    ② FindNextExpiration → Level 1, deadline=1984
    ③ 1984 <= 3000 → ProcessExpiration:
       TakeSlot(level=1, slot=31) → [L1]
       exp.Level≠0 → re-insert:
         LevelForStatic(1984, 10000):  Wait, entry.When=2000!
         LevelForStatic(1984, 2000):
           (1984^2000)|63 = 16|63 = 63, level=0
         Re-insert level 0, slot = 2000%64 = 16
       SetElapsed(1984)
    Loop back:
    ① pending empty
    ② FindNextExpiration:
       Level 0: Occupied bit 16
       NextExpiration(now=1984):
         nowSlot=0, TZC=16, slot=16
         levelStart=1984&~63=1984, deadline=1984+16=2000
    ③ 2000 <= 3000 → ProcessExpiration:
       Level 0, slot 16 → pending.Enqueue(L1)
       SetElapsed(2000)
    Loop back:
    ① pending has L1 → return L1 ✓
```

`Poll(3000)` again → pending empty →
FindNextExpiration → Level 2, deadline=8192 > 3000 → stop.
`PollAll(wheel, 3000)` → [L1].

### `Poll(now=60000)`

Similar cascade: Level 2, slot 2 → Level 1 → Level 0 → fire L2.

`PollAll(wheel, 60000)` → [L2].

---

## 11. Edge Cases

### Already-elapsed timer

`when <= elapsed` → `Insert` returns `false`.  Caller fires the callback immediately.

### Timer in the distant future (near MAX_DURATION)

If `when − elapsed` approaches `64⁶ − 1 ≈ 68.7 billion ms`, the timer is placed in
level 5 (the top level).  The top level acts as a **ring buffer** — when the
clock hand wraps around, timers that appear to be "in the past" are actually
one full rotation in the future.

```csharp
// In NextExpiration:
if (deadline <= now && Index == NumLevels - 1)
    deadline += LevelRange;
```

### Cancellation

A timer can be cancelled before it fires:

```csharp
public bool Remove(WheelEntry entry)
{
    if (entry.Consumed) return false;
    if (entry.When <= Elapsed) return false;  // Already passed
    int level = LevelFor(entry.When);
    if (_levels[level].Remove(entry))
    {
        entry.Consumed = true;
        return true;
    }
    return false;
}
```

`Remove` walks the singly-linked list of the entry's slot to unlink it.  If the
slot becomes empty, the `Occupied` bit is cleared.

**Race condition note:** if the entry has already been moved to the `_pending`
queue, `Remove` cannot find it in any level's slot and returns `false`.  The
caller should treat this as "timer already expired" (it will fire momentarily).

### Empty wheel

When all timers are consumed/cancelled, `IsEmpty` returns `true` and
`NextExpirationTime()` returns `null`.  The driver can sleep or wait for new
timers.

---

## 12. Time Complexity

| Operation | Complexity | Notes |
|-----------|:----------:|-------|
| `Insert` | **O(1)** | Level calc: XOR + LZCNT; slot calc: shift + mask; linked-list prepend |
| `Poll` (per entry) | **O(1)** amortised | `TakeSlot` drains all entries in one slot; re-insert is O(1) per entry |
| `NextExpirationTime` | **O(levels)** = O(1) | 6 levels, each O(1) via bitmask rotation |
| `Remove` | **O(L)** | L = entries in the same slot (practically O(1)) |
| `IsEmpty` | **O(levels)** = O(1) | Check 6 Occupied bitmasks |
| Space | **O(N)** | N = number of active timers; slots are fixed-size arrays |

**Key insight:** insert and find-next-expiration are both **truly O(1)**, not
amortised.  This is what makes the timer wheel superior to binary heaps for
async runtimes.

---

## 13. Comparison with Alternatives

| Data structure | Insert | Extract-min | Notes |
|----------------|:------:|:-----------:|-------|
| **Timer Wheel** (this) | O(1) | O(1) | Fixed precision (1 ms); bounded max duration (~2 yr) |
| Binary heap | O(log N) | O(log N) | Unbounded range; per-operation heap bubbling |
| Fibonacci heap | O(1) amortised | O(log N) amortised | Complex; high constant factors |
| Calendar queue | O(1) average | O(1) average | Probabilistic; harder to reason about |
| `SortedDictionary` | O(log N) | O(log N) | Tree rebalancing overhead |

For its target use case (async I/O runtime with thousands to millions of timers),
the timer wheel is nearly optimal.  The 1 ms precision and ~2 year maximum
duration are more than sufficient for all practical networking and scheduling
scenarios.

---

## Further Reading

- [Tokio source: `tokio/src/runtime/time/wheel/`](https://github.com/tokio-rs/tokio/tree/master/tokio/src/runtime/time/wheel)
- Varghese & Lauck, *"Hashed and Hierarchical Timing Wheels"*, IEEE/ACM ToN, 1997
- [Linux kernel timer wheel](https://www.kernel.org/doc/html/latest/core-api/timekeeping.html) — similar hierarchical design with different constants
- [Netty `HashedWheelTimer`](https://netty.io/wiki/hashed-wheel-timer.html) — single-level wheel used in Java networking
