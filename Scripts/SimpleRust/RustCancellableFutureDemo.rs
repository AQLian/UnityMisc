use std::future::Future;
use std::pin::Pin;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::task::{Context, Poll, Waker};
use std::thread;
use std::time::Duration;

/* ---------- 返回类型 ---------- */
#[derive(Debug)]
enum MyResult<T> {
    Completed(T),
    Cancelled(String),
}

/* ---------- 共享状态 ---------- */
struct Inner<T> {
    value: Option<MyResult<T>>,
    waker: Option<Waker>,
}
type Shared<T> = Arc<Mutex<Inner<T>>>;

/* ---------- 取消句柄 + Future ---------- */
struct CancellableFuture<T> {
    state: Option<Shared<T>>,
    cancel: Option<Arc<AtomicBool>>,
    handle: Option<thread::JoinHandle<()>>,
    _phantom: std::marker::PhantomData<T>,
}

impl CancellableFuture<i32> {
    /// 创建 *未启动* 的 Future；线程在第一次 poll 才 spawn。
    fn new() -> (Self, CancelToken) {
        let token = CancelToken {
            flag: Arc::new(AtomicBool::new(false)),
        };
        let fut = Self {
            state: None,
            cancel: Some(Arc::clone(&token.flag)),
            handle: None,
            _phantom: std::marker::PhantomData,
        };
        (fut, token)
    }
}

/* ---------- 取消句柄（可 Clone，随处发取消） ---------- */
#[derive(Clone)]
struct CancelToken {
    flag: Arc<AtomicBool>,
}

impl CancelToken {
    fn cancel(&self) {
        self.flag.store(true, Ordering::Relaxed);
    }
}

/* ---------- Future 实现 ---------- */
impl Future for CancellableFuture<i32> {
    type Output = MyResult<i32>;

    fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        // 懒启动：第一次 poll 才创建线程
        if self.state.is_none() {
            let state: Shared<i32> = Arc::new(Mutex::new(Inner {
                value: None,
                waker: None,
            }));
            let cancel = Arc::clone(self.cancel.as_ref().unwrap());

            let state_clone = Arc::clone(&state);
            let handle = thread::spawn(move || {
                for i in 0..10 {
                    if cancel.load(Ordering::Relaxed) {
                        let mut locked = state_clone.lock().unwrap();
                        locked.value = Some(MyResult::Cancelled(
                            "cancelled by token".into(),
                        ));
                        if let Some(w) = locked.waker.take() {
                            w.wake()
                        }
                        return;
                    }
                    thread::sleep(Duration::from_millis(5000));
                }
                // 正常完成
                let mut locked = state_clone.lock().unwrap();
                locked.value = Some(MyResult::Completed(42));
                if let Some(w) = locked.waker.take() {
                    w.wake()
                }
            });

            self.state = Some(state);
            self.handle = Some(handle);
        }

        let state = self.state.as_ref().unwrap();
        let mut locked = state.lock().unwrap();

        if let Some(res) = locked.value.take() {
            return Poll::Ready(res);
        }

        // 注册 waker，让线程完成后唤醒
        locked.waker = Some(cx.waker().clone());
        Poll::Pending
    }
}

impl<T> Drop for CancellableFuture<T> {
    fn drop(&mut self) {
        // 1. 发取消信号
        if let Some(c) = self.cancel.as_ref() {
            c.store(true, Ordering::Relaxed);
        }

        // 2. 等线程看到信号后退出（协作） 完全看实现细节，如果需要知道await后是内部token触发的
        // cancel,需要这个join操作来等待future done，如果是tokio select这种直接drop了，其实
        // 都不关心结果了，因为我已经知道处于tokio timeout了，可以不加这个join逻辑
        // if let Some(h) = self.handle.take() {
        //     h.join().ok();
        // }
    }
}

/* ---------- 演示三种取消场景 ---------- */
#[tokio::main]
async fn main() {
    // 1. 提前 drop（模拟用户不要了）
    // {
    //     let (fut, _token) = CancellableFuture::new();
    //     drop(fut);
    //     println!("1. dropped before first poll → no thread spawned");
    // }

    // 2. 用 select! 外部取消
    // {
    //     println!("spawn a cancellable future {:#?}", std::time::Instant::now());
    //     let (fut, token) = CancellableFuture::new();
    //     tokio::spawn(async move {
    //         tokio::time::sleep(Duration::from_millis(2500)).await;
    //         token.cancel();
    //     });
    //
    //     match fut.await {
    //         MyResult::Completed(v) => println!("2. completed: {}", v),
    //         MyResult::Cancelled(r) => println!("2. cancelled: {}", r),
    //     }
    //     println!("spawn a cancellable future run done {:#?}", std::time::Instant::now());
    // }

    // 3. 用 tokio::time::timeout 取消
    {
        println!("spawn a cancellable future {:#?}", std::time::Instant::now());
        let (fut, _token) = CancellableFuture::new();
        match tokio::time::timeout(Duration::from_millis(1000), fut).await {
            Ok(res) => println!("3. finished: {:?}", res),
            Err(_) => println!("3. timeout → future dropped & cancelled"),
        }
        println!("spawn a cancellable future run done {:#?}", std::time::Instant::now());
    }
}
