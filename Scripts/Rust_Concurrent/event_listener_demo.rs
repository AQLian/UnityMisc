use event_listener::Event;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::thread;
use std::time::Duration;

let flag = Arc::new(AtomicBool::new(false));
let event = Arc::new(Event::new());

// Spawned thread
let flag_clone = flag.clone();
let event_clone = event.clone();
thread::spawn(move || {
    flag_clone.store(true, Ordering::SeqCst);
    event_clone.notify(usize::MAX); // Notify all listeners
});

// Main thread
loop {
    let listener = event.listen(); // Register listener first
    if flag.load(Ordering::SeqCst) {
        break;
    }
    listener.wait(); // Only wait if flag is false
}