# Unity Transport (`com.unity.transport`) — API & Feature Reference

> Version 2.2.1 | Unity 2022.2+ | Low-level multiplayer networking library

---

## Overview

Unity Transport is the low-level networking layer used by both **Netcode for GameObjects** and **Netcode for Entities**. It provides a connection-based abstraction over UDP and WebSocket with optional encryption, pipelines for reliability/ordering/fragmentation, and Burst-compatible job scheduling.

```
                   ┌──────────────────────────────┐
   Your Code  ────│     NetworkDriver             │
                   │  ┌──────────┐ ┌────────────┐ │
                   │  │Pipeline A│ │Pipeline B  │ │── reliability, ordering
                   │  └──────────┘ └────────────┘ │
                   │  ┌──────────────────────┐    │
                   │  │  UDP / WebSocket / IPC│    │── network interfaces
                   │  └──────────────────────┘    │
                   └──────────────────────────────┘
```

---

## Core API Types

### `NetworkDriver` (struct, `IDisposable`)

The main entry point. Think of it as a socket with extra features.

| Method | Description |
|---|---|
| `Create()` / `Create(NetworkSettings)` | Factory — constructs a new driver |
| `Create<N>(N interface, ...)` | Factory with a custom `INetworkInterface` |
| `Bind(NetworkEndpoint)` | Bind to a local address:port |
| `Listen()` | Start accepting incoming connections (server) |
| `Connect(NetworkEndpoint, payload?)` | Connect to a remote endpoint (client). Returns `NetworkConnection` |
| `Accept()` / `Accept(out payload)` | Accept an incoming connection (server) |
| `Disconnect(NetworkConnection)` | Close a connection |
| `ScheduleUpdate(JobHandle)` | Schedule the update job — processes incoming packets, checks timeouts, flushes sends. Must call `.Complete()` before reading events |
| `ScheduleFlushSend(JobHandle)` | Lighter variant — only flushes queued sends. Can be called multiple times per tick |
| `PopEvent(out conn, out reader)` | Pop next event from the event queue |
| `PopEventForConnection(conn, out reader)` | Pop next event for a specific connection |
| `BeginSend(conn/pipe, conn, out writer, size?)` | Start composing a message — returns a `DataStreamWriter` |
| `EndSend(writer)` | Enqueue the composed message for sending |
| `AbortSend(writer)` | Cancel a message started with `BeginSend` |
| `CreatePipeline(Type[] stages)` | Create a custom pipeline from stage types |
| `RegisterPipelineStage<T>(T stage)` | Register a custom pipeline stage (before bind) |
| `GetLocalEndpoint()` | Local address this driver is bound to |
| `GetRemoteEndpoint(conn)` | Remote address of a connection |
| `GetConnectionState(conn)` | Current state of a connection |
| `ToConcurrent()` | Get a concurrent-safe copy for use in jobs |
| `MaxHeaderSize(pipe)` | Header overhead for a pipeline |
| `Dispose()` | Release unmanaged memory |

**Properties:** `Bound`, `Listening`, `IsCreated`, `ReceiveErrorCode`, `CurrentSettings`

### `NetworkDriver.Concurrent` (struct)

Job-safe mirror of `NetworkDriver`. Obtained via `driver.ToConcurrent()`. Only supports `BeginSend`/`EndSend`/`AbortSend`.

---

### `NetworkConnection` (struct)

Handle to a single communication session with a remote peer.

| Method | Description |
|---|---|
| `PopEvent(driver, out reader)` | Pop event for this connection |
| `Disconnect(driver)` | Close this connection |
| `GetState(driver)` | Current connection state |
| `IsCreated` | Whether the handle is valid |

**States:** `Unknown`, `Connecting`, `Connected`, `Disconnecting`, `Disconnected`

---

### `NetworkEndpoint` (struct)

IP address + port + family. Analogous to `sockaddr`.

| Static Member | Description |
|---|---|
| `AnyIpv4` / `AnyIpv6` | Wildcard addresses |
| `LoopbackIpv4` / `LoopbackIpv6` | Localhost addresses |

| Instance Method | Description |
|---|---|
| `WithPort(ushort port)` | Return a copy with a different port |
| `Address` | IP address string |
| `Port` | Port number |
| `Family` | `NetworkFamily` (Ipv4, Ipv6, Custom) |
| `IsValid` | Whether the endpoint is valid |

---

### `NetworkEvent.Type` (enum)

Returned by `PopEvent` / `PopEventForConnection`.

| Value | Description |
|---|---|
| `Empty` | No more events in the queue |
| `Data` | Data received — read payload from `DataStreamReader` |
| `Connect` | Connection established (client-side notification) |
| `Disconnect` | Connection closed — `reader` contains disconnect reason byte |

---

### `DataStreamReader` / `DataStreamWriter` (Unity Collections)

Used to read/write messages.

**Reader:** `ReadByte()`, `ReadInt()`, `ReadUInt()`, `ReadFloat()`, `ReadLong()`, `ReadULong()`, `ReadFixedString64()`, `ReadBytes(byte[])`

**Writer:** `WriteByte()`, `WriteInt()`, `WriteUInt()`, `WriteFloat()`, `WriteLong()`, `WriteULong()`, `WriteFixedString64()`, `WriteBytes(byte[])`

---

## Pipelines

Pipelines add optional processing to a packet stream. You compose them with `CreatePipeline()` and pass the resulting `NetworkPipeline` handle to `BeginSend`.

### Built-in Pipeline Stages

| Stage | Description |
|---|---|
| `NullPipelineStage` | No processing. Use to create different "channels" for different message types on the same connection |
| `ReliableSequencedPipelineStage` | Guaranteed delivery + in-order. Uses ACKs and retransmission. Window size: 32 (configurable to 64). Susceptible to head-of-line blocking |
| `UnreliableSequencedPipelineStage` | In-order delivery only (drops out-of-order packets). No reliability guarantees |
| `FragmentationPipelineStage` | Splits large packets (> ~1400 bytes) into MTU-sized chunks and reassembles them |
| `SimulatorPipelineStage` | Artificial network conditions (packet loss, latency, jitter) for testing. Can be configured per-pipeline |

### Creating a Pipeline

```csharp
// Reliable ordered channel
var reliable = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

// Reliable + fragmentation for large messages
var reliableLarge = m_Driver.CreatePipeline(
    typeof(FragmentationPipelineStage),
    typeof(ReliableSequencedPipelineStage)
);

// Unreliable (default — NetworkPipeline.Null)
NetworkPipeline.Null
```

### Pipeline Stage Order

Stages process packets **in order** when sending, and **in reverse order** when receiving:

```
Send:    [Fragmentation] → [Reliable] → network
Receive: network → [Reliable] → [Fragmentation]
```

---

## Network Interfaces (Transport Layer)

| Interface | Description |
|---|---|
| `UDPNetworkInterface` | Default. Sends/receives over UDP. Not available on WebGL |
| `WebSocketNetworkInterface` | WebSocket connections. Only interface available on WebGL (client only, server requires Relay) |
| `IPCNetworkInterface` | Intra-process communication. Instant, no real network. Useful for testing or single-player with local multiplayer code paths |
| `INetworkInterface` (custom) | Implement for custom transports. Must be `unmanaged`. Use `WrapToUnmanaged<T>()` for managed implementations |

---

### `MultiNetworkDriver` (struct)

Drives multiple `NetworkDriver` instances at once (e.g. UDP + WebSocket for cross-play). Has its own `ToConcurrent()` for job usage.

---

## NetworkSettings & Parameters

### `NetworkSettings` (struct)

Aggregate of parameter structures passed to `NetworkDriver.Create()`. Built via chaining:

```csharp
var settings = new NetworkSettings()
    .WithNetworkConfigParameters(
        connectTimeoutMS: 1000,
        maxConnectAttempts: 60,
        disconnectTimeoutMS: 30000,
        sendQueueCapacity: 512,
        receiveQueueCapacity: 512,
        maxFrameTimeMS: 16
    );
var driver = NetworkDriver.Create(settings);
```

### Key Parameters

| Parameter | Description |
|---|---|
| `NetworkConfigParameter` | Timeouts, queue capacities, max frame time |
| `NetworkSimulatorParameter` | Global packet loss/latency for all traffic |
| `WebSocketParameter` | WebSocket-specific settings |
| `BaselibNetworkParameter` | *(Obsolete)* |

### `NetworkParameterConstants` (struct)

Default values for all configurable parameters.

---

## SimulatorPipelineStage API

For per-pipeline network simulation (packet loss, latency, jitter):

```csharp
// Add simulator to pipeline
var simPipeline = m_Driver.CreatePipeline(
    typeof(SimulatorPipelineStage),
    typeof(ReliableSequencedPipelineStage)
);

// Modify parameters at runtime
SimulatorUtility.Parameters simParams = default;
simParams.PacketLossPercent = 10;
simParams.MaxLatency = 200;
simParams.MinLatency = 50;
m_Driver.ModifySimulatorStageParameters(simParams);
```

---

## Extensibility

| Interface | Use |
|---|---|
| `INetworkInterface` | Custom transport layer (raw socket, Steam, ENet, etc.) |
| `INetworkPipelineStage` | Custom pipeline stage (encryption, compression, custom reliability) |
| `INetworkParameter` | Custom configuration parameters for the above |

Custom pipeline stages require:
- Implementing `INetworkPipelineStage`
- Providing a `NetworkPipelineStage` static initializer
- Providing `InitializeConnectionDelegate`, `SendDelegate`, `ReceiveDelegate`

---

## Connection Lifecycle

```
Client                          Server
  │                                │
  │  Connect(endpoint) ──────────► │
  │                                │  Listen() active
  │  ┌──────────┐                  │  Accept() → conn
  │  │Connecting│                  │
  │  └──────────┘                  │
  │         │                      │
  │  PopEvent → Connect ◄───────── │
  │  ┌─────────┐                   │
  │  │Connected│                   │
  │  └─────────┘                   │
  │         │                      │
  │  BeginSend/EndSend ──────────► │
  │         │      ◄────────────── │
  │  PopEvent → Data               │
  │         │                      │
  │  Disconnect() ───────────────► │
  │  ┌─────────────┐               │
  │  │Disconnecting│               │
  │  └─────────────┘               │
  │  PopEvent → Disconnect         │
```

---

## Quick Start

### Server

```csharp
using Unity.Collections;
using Unity.Networking.Transport;

NetworkDriver m_Driver;
NativeList<NetworkConnection> m_Connections;

void Start()
{
    m_Driver = NetworkDriver.Create();
    m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    m_Driver.Bind(NetworkEndpoint.AnyIpv4.WithPort(7777));
    m_Driver.Listen();
}

void Update()
{
    m_Driver.ScheduleUpdate().Complete();

    // Accept new connections
    NetworkConnection c;
    while ((c = m_Driver.Accept()) != default)
        m_Connections.Add(c);

    // Process events
    for (int i = 0; i < m_Connections.Length; i++)
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream))
               != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                uint value = stream.ReadUInt();
                m_Driver.BeginSend(m_Connections[i], out var writer);
                writer.WriteUInt(value + 2);
                m_Driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                m_Connections[i] = default;
            }
        }
    }
}

void OnDestroy()
{
    if (m_Driver.IsCreated) { m_Driver.Dispose(); m_Connections.Dispose(); }
}
```

### Client

```csharp
using Unity.Networking.Transport;

NetworkDriver m_Driver;
NetworkConnection m_Connection;

void Start()
{
    m_Driver = NetworkDriver.Create();
    m_Connection = m_Driver.Connect(NetworkEndpoint.LoopbackIpv4.WithPort(7777));
}

void Update()
{
    m_Driver.ScheduleUpdate().Complete();
    if (!m_Connection.IsCreated) return;

    DataStreamReader stream;
    NetworkEvent.Type cmd;
    while ((cmd = m_Connection.PopEvent(m_Driver, out stream))
           != NetworkEvent.Type.Empty)
    {
        if (cmd == NetworkEvent.Type.Connect)
        {
            m_Driver.BeginSend(m_Connection, out var writer);
            writer.WriteUInt(1);
            m_Driver.EndSend(writer);
        }
        else if (cmd == NetworkEvent.Type.Data)
        {
            Debug.Log($"Got: {stream.ReadUInt()}");
            m_Connection.Disconnect(m_Driver);
            m_Connection = default;
        }
        else if (cmd == NetworkEvent.Type.Disconnect)
        {
            m_Connection = default;
        }
    }
}

void OnDestroy() { if (m_Driver.IsCreated) m_Driver.Dispose(); }
```

---

## Platform Support

| Platform | UDP | WebSocket | Notes |
|---|---|---|---|
| Windows/Mac/Linux | ✓ | ✓ | |
| iOS/Android | ✓ | ✓ | |
| WebGL | ✗ | ✓ client only | Server requires Unity Relay |
| Console | ✓ | ✓ | |

---

## Key Features Summary

| Feature | Details |
|---|---|
| **Transport protocols** | UDP, WebSocket, IPC (in-process) |
| **Encryption** | Built-in DTLS over UDP, TLS over WebSocket |
| **Reliability** | ACK-based with configurable window size (32-64) |
| **Ordering** | Unreliable sequenced (drop out-of-order) or reliable sequenced |
| **Fragmentation** | Automatic for packets > MTU (~1400 bytes) |
| **Network simulation** | Per-pipeline packet loss, latency, jitter |
| **Job system** | `ScheduleUpdate`/`ScheduleFlushSend` run on worker threads |
| **Burst-compatible** | Core API compiles with Burst |
| **Concurrent access** | `ToConcurrent()` for sending from jobs |
| **Multi-driver** | `MultiNetworkDriver` for UDP+WebSocket cross-play |
| **Custom transports** | `INetworkInterface` extensibility |
| **Custom pipelines** | `INetworkPipelineStage` extensibility |
| **Unity Relay** | First-class integration via `NetworkDriverRelayExtensions` |
| **Connection payload** | Attach arbitrary bytes to `Connect()` / `Accept()` |
