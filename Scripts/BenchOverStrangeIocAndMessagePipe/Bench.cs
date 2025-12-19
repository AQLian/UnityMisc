using System;
using System.Diagnostics;
using UnityEngine;
using strange.extensions.dispatcher.eventdispatcher.impl;
using strange.extensions.dispatcher.eventdispatcher.api;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Debug = UnityEngine.Debug;

public class Bench : MonoBehaviour
{
    // Constants
    private const int TEST_ITERATIONS = 100000;
    private const int WARMUP_ITERATIONS = 10000;
    private const int LISTENER_COUNT = 5;

    // StrangeIoC EventDispatcher
    private IEventDispatcher strangeDispatcher;
    private StrangeTestEvent strangeTestEvent = new StrangeTestEvent();

    // MessagePipe
    private IPublisher<MessagePipeTestEvent> mpPublisher;
    private ISubscriber<MessagePipeTestEvent> mpSubscriber;
    private MessagePipeTestEvent mpTestEvent = new MessagePipeTestEvent();
    private IDisposable[] mpSubscriptions;

    // Test data
    public struct SmallPayload
    {
        public int id;
        public float value;
        public bool flag;
    }

    private SmallPayload testPayload = new SmallPayload { id = 123, value = 45.6f, flag = true };

    void Start()
    {
        // Initialize StrangeIoC EventDispatcher
        strangeDispatcher = new EventDispatcher();
        strangeTestEvent.type = "TEST_EVENT";

        // Initialize MessagePipe
        var builtinContainer = new BuiltinContainerBuilder();
        builtinContainer.AddMessagePipe(c => { });
        builtinContainer.AddMessageBroker<MessagePipeTestEvent>();
        
        var provider = builtinContainer.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        mpPublisher = GlobalMessagePipe.GetPublisher<MessagePipeTestEvent>();
        mpSubscriber = GlobalMessagePipe.GetSubscriber<MessagePipeTestEvent>();
        mpSubscriptions = new IDisposable[LISTENER_COUNT];

        // Run benchmark
        RunBenchmarks().Forget();
    }

    private async UniTask RunBenchmarks()
    {
        await UniTask.Yield(); // Wait for frame to stabilize

        Debug.Log("=== Starting Event System Benchmark ===");
        Debug.Log($"Iterations: {TEST_ITERATIONS}, Listeners: {LISTENER_COUNT}");

        // Warmup
        RunStrangeWarmup();
        RunMessagePipeWarmup();

        // Benchmark 1: Empty payload (no data)
        RunEmptyPayloadTest();

        // Benchmark 2: Small payload (struct data)
        RunSmallPayloadTest();

        // Cleanup
        foreach (var sub in mpSubscriptions)
        {
            sub?.Dispose();
        }

        Debug.Log("=== Benchmark Complete ===");
    }

    #region Warmup Methods
    private void RunStrangeWarmup()
    {
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.AddListener(strangeTestEvent.type, StrangeEmptyHandler);
        }

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            strangeDispatcher.Dispatch(strangeTestEvent.type);
        }

        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.RemoveListener(strangeTestEvent.type, StrangeEmptyHandler);
        }
    }

    private void RunMessagePipeWarmup()
    {
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            mpSubscriptions[i] = mpSubscriber.Subscribe(MessagePipeEmptyHandler);
        }

        for (int i = 0; i < WARMUP_ITERATIONS; i++)
        {
            mpPublisher.Publish(mpTestEvent);
        }

        foreach (var sub in mpSubscriptions)
        {
            sub.Dispose();
        }
    }
    #endregion

    #region Empty Payload Test
    private void RunEmptyPayloadTest()
    {
        Debug.Log("\n--- Empty Payload Test ---");

        // StrangeIoC
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.AddListener(strangeTestEvent.type, StrangeEmptyHandler);
        }

        var strangeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            strangeDispatcher.Dispatch(strangeTestEvent.type);
        }
        strangeStopwatch.Stop();

        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.RemoveListener(strangeTestEvent.type, StrangeEmptyHandler);
        }

        // MessagePipe
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            mpSubscriptions[i] = mpSubscriber.Subscribe(MessagePipeEmptyHandler);
        }

        var mpStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            mpPublisher.Publish(mpTestEvent);
        }
        mpStopwatch.Stop();

        foreach (var sub in mpSubscriptions)
        {
            sub.Dispose();
        }

        // Results
        Debug.Log($"StrangeIoC EventDispatcher: {strangeStopwatch.ElapsedMilliseconds}ms");
        Debug.Log($"MessagePipe Pub/Sub: {mpStopwatch.ElapsedMilliseconds}ms");
    }
    #endregion

    #region Small Payload Test
    private void RunSmallPayloadTest()
    {
        Debug.Log("\n--- Small Payload Test ---");

        // StrangeIoC
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.AddListener(strangeTestEvent.type, StrangePayloadHandler);
        }

        var strangeStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            strangeDispatcher.Dispatch(strangeTestEvent.type, testPayload);
        }
        strangeStopwatch.Stop();

        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            strangeDispatcher.RemoveListener(strangeTestEvent.type, StrangePayloadHandler);
        }

        // MessagePipe
        mpTestEvent.payload = testPayload;
        for (int i = 0; i < LISTENER_COUNT; i++)
        {
            mpSubscriptions[i] = mpSubscriber.Subscribe(MessagePipePayloadHandler);
        }

        var mpStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < TEST_ITERATIONS; i++)
        {
            mpPublisher.Publish(mpTestEvent);
        }
        mpStopwatch.Stop();

        foreach (var sub in mpSubscriptions)
        {
            sub.Dispose();
        }

        // Results
        Debug.Log($"StrangeIoC EventDispatcher: {strangeStopwatch.ElapsedMilliseconds}ms");
        Debug.Log($"MessagePipe Pub/Sub: {mpStopwatch.ElapsedMilliseconds}ms");
    }
    #endregion

    #region Handlers
    private void StrangeEmptyHandler() { }
    //private void StrangeEmptyHandler(IEvent evt) { }

    private void StrangePayloadHandler(IEvent evt)
    {
        var payload = (SmallPayload)evt.data;
        // Simulate work with payload
        var _ = payload.id + payload.value;
    }

    private void MessagePipeEmptyHandler(MessagePipeTestEvent evt) { }

    private void MessagePipePayloadHandler(MessagePipeTestEvent evt)
    {
        var payload = evt.payload;
        // Simulate work with payload
        var _ = payload.id + payload.value;
    }
    #endregion

    // Test event classes
    public class StrangeTestEvent : IEvent
    {
        public object type { get; set; }
        public object data { get; set; }
        public IEventDispatcher target { get; set; }
    }

    public class MessagePipeTestEvent
    {
        public SmallPayload payload;
    }
}