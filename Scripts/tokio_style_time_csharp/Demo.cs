using System;
using System.Threading;

namespace TimerWheel;

public class Demo
{
    public static void Run()
    {
        // ── Step 1: Create the driver ───────────────────────────────────
        // The driver starts a background thread that owns the timer wheel.
        // It will poll, fire callbacks, and sleep until the next deadline.
        using var driver = new TimerDriver();

        Console.WriteLine("Driver started.\n");

        // ── Step 2: Schedule a simple one-shot timer ─────────────────────
        // Schedule(delayMs, callback, optionalState)
        // Returns a WheelEntry you can use to cancel later.
        WheelEntry t1 = driver.Schedule(200, state =>
        {
            Console.WriteLine("[200ms] Simple one-shot fired.");
        });
        Console.WriteLine("Scheduled: 200ms one-shot");

        // ── Step 3: Schedule with custom state ───────────────────────────
        WheelEntry t2 = driver.Schedule(300, state =>
        {
            string msg = (string)state!;
            Console.WriteLine($"[300ms] Custom state: {msg}");
        }, "Hello from state!");
        Console.WriteLine("Scheduled: 300ms with state");

        // ── Step 4: Cancel a timer before it fires ──────────────────────
        WheelEntry t3 = driver.Schedule(400, _ =>
        {
            Console.WriteLine("[400ms] This should NEVER appear.");
        });
        Console.WriteLine("Scheduled: 400ms (will be cancelled)");
        driver.Cancel(t3);
        Console.WriteLine("Cancelled: 400ms timer\n");

        // ── Step 5: Schedule a repeating timer (manual re-schedule) ─────
        // The wheel is one-shot; re-schedule inside the callback for repeats.
        driver.Schedule(100, state =>
        {
            int count = state is int n ? n : 0;
            count++;
            Console.WriteLine($"[repeating] Tick #{count}");

            if (count < 3)
            {
                // Re-schedule with updated state
                driver.Schedule(100, state2 =>
                {
                    int c2 = (int)state2!;
                    c2++;
                    Console.WriteLine($"[repeating] Tick #{c2}");

                    if (c2 < 3)
                    {
                        driver.Schedule(100, state3 =>
                        {
                            int c3 = (int)state3!;
                            c3++;
                            Console.WriteLine($"[repeating] Tick #{c3} (last)");
                        }, c2);
                    }
                }, count);
            }
        }, 0);

        // ── Step 6: Many timers at once ─────────────────────────────────
        Console.WriteLine("Scheduling 5 quick timers...");
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            driver.Schedule(50 + idx * 30, _ =>
            {
                Console.WriteLine($"[~{50 + idx * 30}ms] Timer #{idx}");
            });
        }

        // ── Step 7: A long timer to keep the driver alive ───────────────
        // The driver thread exits when disposed, but we want to see all
        // callbacks fire, so we schedule a final timer and wait.
        var done = new ManualResetEventSlim();
        driver.Schedule(600, _ =>
        {
            Console.WriteLine("\n[600ms] Final timer — demo done.");
            done.Set();
        });

        done.Wait();

        // ── Step 8: Dispose stops the background thread ─────────────────
        // The `using` block calls Dispose(), which joins the driver thread.
        Console.WriteLine("Driver stopped.");
    }
}
