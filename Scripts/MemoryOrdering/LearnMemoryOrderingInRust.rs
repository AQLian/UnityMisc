// Rust exposes the full C++11 memory model:
// Ordering	Guarantee
// Relaxed	No ordering, atomicity only. Single counters, stats.
// Acquire	All subsequent reads/writes stay after this load. Pairs with Release.
// Release	All prior reads/writes complete before this store. Pairs with Acquire.
// AcqRel	Combined Acquire + Release. For RMW ops that both read and write.
// SeqCst	Total sequential consistency (strongest, default).



use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering};
use std::thread;

// SeqCst
fn SeqCstDemo() {
    let x = AtomicBool::new(false);
    let y = AtomicBool::new(false);
    let z = AtomicUsize::new(0);
    thread::scope(|s| {
        s.spawn(|| {
            x.store(true, Ordering::SeqCst);           // A
        });
        s.spawn(|| {
            y.store(true, Ordering::SeqCst);           // B
        });
        s.spawn(|| {
            while !x.load(Ordering::SeqCst) {}         // C
            if y.load(Ordering::SeqCst) {              // D
                z.fetch_add(1, Ordering::SeqCst);
            }
        });
        s.spawn(|| {
            while !y.load(Ordering::SeqCst) {}         // E
            if x.load(Ordering::SeqCst) {              // F
                z.fetch_add(1, Ordering::SeqCst);
            }
        });
    });
    // SeqCst guarantees at least one of the threads sees both stores.
    // Under weaker orderings both threads could see 0.
    assert!(z.load(Ordering::SeqCst) >= 1);
}


//Key insight: Acquire on swap(true) makes all subsequent data accesses happen-after.
//Release on store(false) makes all prior data accesses happen-before the next Acquire.
//---
//Demo: Dekker-style flag coordination (Acquire/Release pair)
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread;
fn main() {
    let flag = AtomicBool::new(false);
    let mut data: u64 = 0;
    thread::scope(|s| {
        // Writer
        s.spawn(|| {
            data = 42;                              // (1) write payload
            flag.store(true, Ordering::Release);    // (2) publish
        });
        // Reader
        s.spawn(|| {
            while !flag.load(Ordering::Acquire) {   // (3) observe flag
                std::hint::spin_loop();
            }
            assert_eq!(data, 42);                   // (4) guaranteed to see (1)
        });
    });
}
//---

//Demo: Spinlock (Acquire/Release — synchronize data access)
use std::cell::UnsafeCell;
use std::sync::atomic::{AtomicBool, Ordering};
use std::ops::{Deref, DerefMut};
pub struct SpinLock<T> {
    locked: AtomicBool,
    data: UnsafeCell<T>,
}
unsafe impl<T: Send> Send for SpinLock<T> {}
unsafe impl<T: Send> Sync for SpinLock<T> {}
pub struct SpinLockGuard<'a, T> {
    lock: &'a SpinLock<T>,
}
impl<T> SpinLock<T> {
    pub fn new(data: T) -> Self {
        SpinLock {
            locked: AtomicBool::new(false),
            data: UnsafeCell::new(data),
        }
    }
    pub fn lock(&self) -> SpinLockGuard<'_, T> {
        while self.locked.swap(true, Ordering::Acquire) {
            std::hint::spin_loop();
        }
        SpinLockGuard { lock: self }
    }
}
impl<T> Deref for SpinLockGuard<'_, T> {
    type Target = T;
    fn deref(&self) -> &T {
        unsafe { &*self.lock.data.get() }
    }
}
impl<T> DerefMut for SpinLockGuard<'_, T> {
    fn deref_mut(&mut self) -> &mut T {
        unsafe { &mut *self.lock.data.get() }
    }
}
impl<T> Drop for SpinLockGuard<'_, T> {
    fn drop(&mut self) {
        self.lock.locked.store(false, Ordering::Release);
    }
}



// Demo: CAS-based lock-free LIFO stack
use std::sync::atomic::{AtomicPtr, Ordering};
use std::ptr;
struct Node<T> {
    value: T,
    next: *mut Node<T>,
}
pub struct AtomicStack<T> {
    head: AtomicPtr<Node<T>>,
}
impl<T> AtomicStack<T> {
    pub fn new() -> Self {
        AtomicStack { head: AtomicPtr::new(ptr::null_mut()) }
    }

    pub fn push(&self, value: T) {
        let node = Box::into_raw(Box::new(Node { value, next: ptr::null_mut() }));
        loop {
            let head = self.head.load(Ordering::Relaxed);
            unsafe { (*node).next = head; }
            match self.head.compare_exchange_weak(
                head,
                node,
                Ordering::Release,  // success: publish node
                Ordering::Relaxed,  // failure: just retry
            ) {
                Ok(_) => break,
                Err(_) => continue,
            }
        }
    }

    pub fn pop(&self) -> Option<T> {
        loop {
            let head = self.head.load(Ordering::Acquire);
            if head.is_null() {
                return None;
            }
            let next = unsafe { (*head).next };
            match self.head.compare_exchange_weak(
                head,
                next,
                Ordering::Acquire,  // success: take ownership
                Ordering::Acquire,  // failure: retry, still observing
            ) {
                Ok(_) => {
                    let boxed = unsafe { Box::from_raw(head) };
                    return Some(boxed.value);
                }
                Err(_) => continue,
            }
        }
    }
}

impl<T> Drop for AtomicStack<T> {
    fn drop(&mut self) {
        while self.pop().is_some() {}
    }
}
// ---


//Demo: Counter (Relaxed — single field, no cross-thread ordering needed)
use std::sync::atomic::{AtomicUsize, Ordering};
use std::thread;
fn main() {
    let counter = AtomicUsize::new(0);
    thread::scope(|s| {
        for _ in 0..4 {
            s.spawn(|| {
                for _ in 0..10_000 {
                    counter.fetch_add(1, Ordering::Relaxed);
                }
            });
        }
    });
    assert_eq!(counter.load(Ordering::Relaxed), 40_000);
}
//---