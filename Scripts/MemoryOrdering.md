// Thread A
x = 1;                              // plain store
y = 2;                              // plain store
guard.store(1, memory_order_release); // ← only prevents
                                      //    x=1, y=2 from sinking
                                      //    below this line.
                                      //    Loads or stores that
                                      //    appear *after* it may
                                      //    still be reordered
                                      //    *above* it (unless
                                      //    they touch the same
                                      //    atomic variable).







// Thread B
while (guard.load(memory_order_acquire) != 1)
    ;
// ← only prevents loads/stores that come *after* this line
//   from being hoisted *above* it.
z = x;  // guaranteed to see x == 1

