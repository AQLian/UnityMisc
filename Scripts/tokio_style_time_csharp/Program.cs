using System;
using System.Collections.Generic;
using System.Threading;

namespace TimerWheel;

class Program
{
    static int _passed;
    static int _failed;

    static void Main(string[] args)
    {
        Console.WriteLine("=== Tokio-style TimerWheel Tests ===\n");

        TestLevelFor();
        TestNextExpiration();
        TestBasicInsertPoll();
        TestMultipleLevels();
        TestCascade();
        TestCancel();
        TestEdgeCases();
        TestPollIncremental();
        TestElapsedJumpToDeadline();
        TestDriverSmoke();

        Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===");
        if (_failed > 0) Environment.Exit(1);
    }

    // ── helpers ─────────────────────────────────────────────────────────

    static void Assert(bool condition, string msg)
    {
        if (condition) { _passed++; }
        else { _failed++; Console.WriteLine($"  FAIL: {msg}"); }
    }

    static void AssertEq<T>(T expected, T actual, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual)) { _passed++; }
        else { _failed++; Console.WriteLine($"  FAIL: {msg} — expected {expected}, got {actual}"); }
    }

    // ── test: level_for matches Tokio ───────────────────────────────────

    static void TestLevelFor()
    {
        Console.WriteLine("[LevelFor]");
        long elapsed = 0;

        // Level 0: 1..63 ms
        for (long pos = 1; pos < 64; pos++)
            AssertEq(0, TimerWheel.LevelForStatic(elapsed, pos), $"level_for(0,{pos})");

        // Level 1: 64..4095 ms
        for (int level = 1; level <= 4; level++)
        {
            long step = (long)Math.Pow(64, level);
            for (int pos = level; pos < 64; pos++)
            {
                long a = pos * step;
                AssertEq(level, TimerWheel.LevelForStatic(elapsed, a), $"level_for(0,{a})");

                if (pos > level)
                {
                    long a_1 = a - 1;
                    AssertEq(level, TimerWheel.LevelForStatic(elapsed, a_1), $"level_for(0,{a_1})");
                }
                if (pos < 63)
                {
                    long a_p1 = a + 1;
                    AssertEq(level, TimerWheel.LevelForStatic(elapsed, a_p1), $"level_for(0,{a_p1})");
                }
            }
        }

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: next_expiration on an empty wheel ─────────────────────────

    static void TestNextExpiration()
    {
        Console.WriteLine("[NextExpiration]");
        var wheel = new TimerWheel();

        AssertEq((long?)null, wheel.NextExpirationTime(), "empty wheel -> null");

        // Insert one timer at 50 ms
        var e1 = new WheelEntry { When = 50 };
        Assert(wheel.Insert(e1), "insert 50ms");

        // next_expiration should be ~50 (depending on elapsed=0)
        long? next = wheel.NextExpirationTime();
        Assert(next is not null, "next_exp != null");
        Assert(next!.Value >= 50 && next.Value < 64, $"next_exp in [50,64): {next.Value}");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: basic insert + poll ───────────────────────────────────────

    static void TestBasicInsertPoll()
    {
        Console.WriteLine("[BasicInsertPoll]");
        var wheel = new TimerWheel();

        // Insert 3 timers
        wheel.Insert(new WheelEntry { When = 10, State = "A" });
        wheel.Insert(new WheelEntry { When = 20, State = "B" });
        wheel.Insert(new WheelEntry { When = 5,  State = "C" });

        // Poll at t=8 — only C should fire
        var fired1 = PollAll(wheel, 8);
        AssertEq(1, fired1.Count, "t=8 fires 1 entry");
        AssertEq("C", (string)fired1[0].State!, "t=8 fires C");

        // Poll at t=15 — A should fire
        var fired2 = PollAll(wheel, 15);
        AssertEq(1, fired2.Count, "t=15 fires 1 entry");
        AssertEq("A", (string)fired2[0].State!, "t=15 fires A");

        // Poll at t=30 — B should fire
        var fired3 = PollAll(wheel, 30);
        AssertEq(1, fired3.Count, "t=30 fires 1 entry");
        AssertEq("B", (string)fired3[0].State!, "t=30 fires B");

        Assert(wheel.IsEmpty, "wheel empty after all expired");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: timers across multiple levels ─────────────────────────────

    static void TestMultipleLevels()
    {
        Console.WriteLine("[MultipleLevels]");
        var wheel = new TimerWheel();

        // Level 0: < 64 ms
        // Level 1: 64 .. 4095 ms
        // Level 2: 4096 .. 262143 ms
        wheel.Insert(new WheelEntry { When = 30,    State = "L0" });
        wheel.Insert(new WheelEntry { When = 2000,  State = "L1" });
        wheel.Insert(new WheelEntry { When = 10000, State = "L2" });
        wheel.Insert(new WheelEntry { When = 50000, State = "L2b" });
        wheel.Insert(new WheelEntry { When = 100,   State = "L1b" });

        var fired = PollAll(wheel, 50);
        AssertEq(1, fired.Count, "t=50: only L0");
        AssertEq("L0", (string)fired[0].State!, "t=50: L0");

        // Around t=100..2000 the L1 entries cascade down
        fired = PollAll(wheel, 150);
        AssertEq(1, fired.Count, "t=150: only L1b");
        AssertEq("L1b", (string)fired[0].State!, "t=150: L1b");

        fired = PollAll(wheel, 3000);
        AssertEq(1, fired.Count, "t=3000: L1");
        AssertEq("L1", (string)fired[0].State!, "t=3000: L1");

        fired = PollAll(wheel, 60000);
        AssertEq(2, fired.Count, "t=60000: L2 + L2b");
        var states = new HashSet<string>();
        foreach (var e in fired) states.Add((string)e.State!);
        Assert(states.Contains("L2") && states.Contains("L2b"), "t=60000: L2 + L2b");

        Assert(wheel.IsEmpty, "wheel empty after all");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: cascade (tier-down) behaviour ─────────────────────────────

    static void TestCascade()
    {
        Console.WriteLine("[Cascade]");
        var wheel = new TimerWheel();

        // Insert a timer at 200ms.  It goes into level 2 (64^2=4096 range, slot = 200/64 ≈ 3 in level 1... 
        // Actually: level_for(0, 200) should be level 1 (since 64 <= 200 < 4096)
        wheel.Insert(new WheelEntry { When = 200, State = "Cascade" });

        // At t=64, level 0 wraps around → cascade from level 1 to level 0
        // At t=128, level 0 wraps again → cascade
        // At t=192, level 0 wraps again → cascade (and the entry should now be in level 0)
        // The entry at 200 should fire after 200 is reached.

        var fired = PollAll(wheel, 150);
        AssertEq(0, fired.Count, "t=150: not yet fired");

        fired = PollAll(wheel, 210);
        AssertEq(1, fired.Count, "t=210: fired");
        AssertEq("Cascade", (string)fired[0].State!, "t=210: Cascade");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: cancel / remove ───────────────────────────────────────────

    static void TestCancel()
    {
        Console.WriteLine("[Cancel]");
        var wheel = new TimerWheel();

        var keep = new WheelEntry { When = 100, State = "keep" };
        var kill = new WheelEntry { When = 100, State = "kill" };
        wheel.Insert(keep);
        wheel.Insert(kill);

        Assert(wheel.Remove(kill), "cancel kill");
        Assert(!wheel.Remove(kill), "double cancel -> false");

        var fired = PollAll(wheel, 200);
        AssertEq(1, fired.Count, "only keep fires");
        AssertEq("keep", (string)fired[0].State!, "keep fires");

        // Cancel after deadline (no-op)
        var late = new WheelEntry { When = 300 };
        wheel.Insert(late);
        fired = PollAll(wheel, 400);
        AssertEq(1, fired.Count, "late already fired");
        Assert(!wheel.Remove(late), "cancel after fire -> false");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: edge cases ────────────────────────────────────────────────

    static void TestEdgeCases()
    {
        Console.WriteLine("[EdgeCases]");

        var wheel = new TimerWheel();

        // Already-elapsed timer
        var past = new WheelEntry { When = 0, State = "past" };
        Assert(!wheel.Insert(past), "insert elapsed -> false");

        // Empty polls
        AssertEq(0, PollAll(wheel, 0).Count, "poll empty -> 0");
        Assert(wheel.IsEmpty, "wheel empty");

        // Very far future (near max)
        long far = TimerWheel.MaxDuration - 1;
        var distant = new WheelEntry { When = far, State = "distant" };
        Assert(wheel.Insert(distant), "insert distant");

        long? next = wheel.NextExpirationTime();
        Assert(next is not null, "next_exp not null for distant");
        Assert(next!.Value <= far, "next_exp <= when");

        // Should not fire at t=1000
        var fired = PollAll(wheel, 1000);
        AssertEq(0, fired.Count, "distant not fired early");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: incremental polling ───────────────────────────────────────

    static void TestPollIncremental()
    {
        Console.WriteLine("[PollIncremental]");
        var wheel = new TimerWheel();

        wheel.Insert(new WheelEntry { When = 1, State = "t1" });
        wheel.Insert(new WheelEntry { When = 2, State = "t2" });
        wheel.Insert(new WheelEntry { When = 3, State = "t3" });

        var w1 = wheel.Poll(1);
        Assert(w1 is not null && "t1" == (string)w1!.State!, "poll(1) -> t1");
        Assert(null == wheel.Poll(1), "poll(1) again -> null");

        var w2 = wheel.Poll(2);
        Assert(w2 is not null && "t2" == (string)w2!.State!, "poll(2) -> t2");

        var w3 = wheel.Poll(3);
        Assert(w3 is not null && "t3" == (string)w3!.State!, "poll(3) -> t3");

        Assert(null == wheel.Poll(100), "poll(100) -> null all drained");
        Assert(wheel.IsEmpty, "empty after");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: elapsed jumps directly to the next deadline ───────────────

    static void TestElapsedJumpToDeadline()
    {
        Console.WriteLine("[ElapsedJumpToDeadline]");
        var wheel = new TimerWheel();

        // Insert with a gap — no timers between 10 and 5000
        wheel.Insert(new WheelEntry { When = 10, State = "early" });
        wheel.Insert(new WheelEntry { When = 5000, State = "late" });

        // Poll to t=20: should fire "early" and set elapsed to 20 (past 10)
        var fired = PollAll(wheel, 20);

        // Now poll to t=100: no timers, elapsed should jump to 100
        // But since there are still timers (late at 5000), next_expiration should be ~5000
        long? next = wheel.NextExpirationTime();
        Assert(next is not null && next.Value > 50, "next_exp after early fired is > 50");

        var none = wheel.Poll(100);
        Assert(none is null, "poll(100) -> null (no timer at 100)");

        // Verify Elapsed advanced to 100
        AssertEq(100L, wheel.Elapsed, "elapsed jumped to 100");

        // Poll all the way past "late"
        fired = PollAll(wheel, 6000);
        AssertEq(1, fired.Count, "late fires at 6000");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── test: smoke test with the actual driver ─────────────────────────

    static void TestDriverSmoke()
    {
        Console.WriteLine("[DriverSmoke]");

        var done = new ManualResetEventSlim();
        int fired = 0;
        var firedStates = new List<string>();
        var lockObj = new object();

        using var driver = new TimerDriver();

        driver.Schedule(150, _ => { lock (lockObj) { Interlocked.Increment(ref fired); firedStates.Add("A"); } });
        driver.Schedule(50,  _ => { lock (lockObj) { Interlocked.Increment(ref fired); firedStates.Add("B"); } });
        driver.Schedule(100, _ => { lock (lockObj) { Interlocked.Increment(ref fired); firedStates.Add("C"); } });
        driver.Schedule(300, _ =>
        {
            lock (lockObj) { Interlocked.Increment(ref fired); firedStates.Add("D"); }
            done.Set();
        });

        // Also test cancel (cancel immediately to avoid race)
        var cancelled = driver.Schedule(400, _ => { Assert(false, "should not fire"); });
        driver.Cancel(cancelled);

        // Wait for D to fire
        Assert(done.Wait(TimeSpan.FromSeconds(10)), "driver finished in time");
        Thread.Sleep(50);

        // A(150), B(50), C(100), D(300) — 4 callbacks total, cancelled excluded
        AssertEq(4, fired, "4 callbacks fired (cancelled excluded)");

        // Order: B(50) < C(100) < A(150) < D(300)
        AssertEq("B", firedStates[0], "B fires first (50ms)");
        AssertEq("C", firedStates[1], "C fires second (100ms)");
        AssertEq("A", firedStates[2], "A fires third (150ms)");
        AssertEq("D", firedStates[3], "D fires last (300ms)");

        Console.WriteLine($"  OK ({_passed})");
    }

    // ── helper: poll until no more entries, return them all ─────────────

    static List<WheelEntry> PollAll(TimerWheel wheel, long now)
    {
        var list = new List<WheelEntry>();
        while (true)
        {
            var entry = wheel.Poll(now);
            if (entry is null) break;
            list.Add(entry);
        }
        return list;
    }
}
