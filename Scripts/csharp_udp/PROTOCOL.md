# Reliable UDP Protocol (RUDP)

A custom reliable UDP transport protocol implemented in C# (.NET 8.0). Provides TCP-like reliability (ACKs, retransmission, sequencing, congestion control, flow control) over raw UDP sockets.

## Architecture

```
┌─────────────────────────────────────────────────┐
│              Application (Program.cs)            │
│  Sends/receives byte[] via RudpConnection API    │
├─────────────────────────────────────────────────┤
│            RudpConnection (per-connection)       │
│  FSM, send window, recv buffer, SACK, RTT, CC   │
├─────────────────────────────────────────────────┤
│           RudpSocket (connection multiplexer)    │
│  Binds UDP socket, dispatches to connections     │
│  Runs ReceiveLoop + TimerLoop (background tasks) │
├─────────────────────────────────────────────────┤
│             RudpSerializer (wire format)         │
│  RudpPacket ↔ byte[] + Internet checksum        │
├─────────────────────────────────────────────────┤
│               .NET Socket (UDP)                  │
└─────────────────────────────────────────────────┘
```

## File Overview

| File | Lines | Purpose |
|------|-------|---------|
| `RudpCommon.cs` | 201 | Constants, enums, header struct, packet class |
| `RudpPacket.cs` | 120 | Serialization, deserialization, checksum |
| `RudpConnection.cs` | 952 | Connection FSM, reliability, congestion control, socket I/O |
| `Program.cs` | 150 | Demo echo server/client |

## Packet Format (Wire Protocol)

Fixed **20-byte header** + variable payload. Max total packet: **1500 bytes** (max payload: **1480 bytes**). All multi-byte fields are **big-endian**.

| Offset | Field | Size | Description |
|--------|-------|------|-------------|
| 0 | VersionType | 1 | High nibble: version (1). Low nibble: packet type |
| 1 | Flags | 1 | Bit flags: SACK (0x01), Retransmit (0x02), More (0x04) |
| 2-5 | ConnectionId | 4 | uint32, uniquely identifies this connection |
| 6-9 | SeqNumber | 4 | uint32, monotonic packet sequence number |
| 10-13 | AckNumber | 4 | uint32, cumulative ACK ("I have everything before this") |
| 14-15 | Window | 2 | uint16, receiver buffer capacity in packets |
| 16-17 | PayloadLength | 2 | uint16, payload bytes following header |
| 18-19 | Checksum | 2 | uint16, Internet checksum (RFC 1071) of header[0-17]+payload |
| 20+ | Payload | var | Application data or SACK blocks |

**Checksum**: Internet checksum (RFC 1071) over header bytes 0-17 + payload. Field zeroed during computation. Non-zero mismatch → packet silently dropped.

## Packet Types

| Type | Value | Purpose |
|------|-------|---------|
| `SYN` | 0x01 | Connection initiation |
| `SYN_ACK` | 0x02 | Accept connection |
| `ACK` | 0x03 | Cumulative acknowledgment (may carry SACK blocks) |
| `DATA` | 0x04 | Reliable data delivery |
| `FIN` | 0x05 | Graceful close request |
| `FIN_ACK` | 0x06 | Acknowledgment of FIN |
| `RST` | 0x07 | Force-reset connection |
| `PING` | 0x08 | Keepalive probe |
| `PONG` | 0x09 | Keepalive response |

## Connection Lifecycle (State Machine)

### Handshake (3-Way)

```
Client                              Server
Closed ──Connect()──> SynSent
  │  ──────────── SYN ─────────────>  Listen ──> SynReceived
  │  <───────── SYN_ACK ────────────
  │  ──────────── ACK ─────────────>  ──> Established
  └──> Established
```

1. **Client**: Sends SYN with initial SeqNumber (in `_sendWindow` for retransmission).
2. **Server**: Receives SYN → sets `_recvNext = SYN.SeqNumber + 1`, generates random `_sendNext`, sends SYN_ACK.
3. **Both**: On receiving final ACK → clear windows, deliver queued data, enter Established.

### Data Transfer

- Application calls `Send(byte[])` → enqueues to `_sendQueue`.
- `TryFlushSendQueue()` serializes into DATA packets with increasing SeqNumber.
- Packets stored in `_sendWindow` for retransmission.
- Receiver: DATA stored in `_recvBuffer` (`SortedDictionary<uint, byte[]>`) for out-of-order reassembly.
- In-order data delivered immediately via `DeliveredData` queue / `OnDataReceived` event.

### Teardown (4-Way Close)

```
Active Close                       Passive Close
Established ──Close()──> FinWait1
  │  ──────────── FIN ────────────>  Established ──> CloseWait (fires OnPeerClosing)
  │  <───────── FIN_ACK ────────────
  └──> FinWait2                      └──Close()──> LastAck
  │  <─────────── FIN ──────────────    │  ──────── FIN ────────────>
  │  ────────── FIN_ACK ───────────>    │  <────── FIN_ACK ──────────
  └──> TimeWait (wait 2×RTO) ──> Closed └──> Closed
```

**Simultaneous close**: FinWait1 + incoming FIN → Closing → TimeWait → Closed.  
**RST in any state** → immediate Closed, clears all buffers.

### State Enum (11 states)

`Closed → Listen → SynSent → SynReceived → Established → FinWait1 → FinWait2 → CloseWait → LastAck → Closing → TimeWait`

## Reliability Mechanisms

### Selective Repeat ARQ (Sliding Window)

- **Send window**: `_sendWindow` (Dictionary<uint, RudpPacket>) — unacknowledged packets.
- **Receive window**: `_recvBuffer` (SortedDictionary<uint, byte[]>) — out-of-order packets.
- **Default window size**: 64 packets.
- Sequence numbers: 32-bit unsigned, wrap handled via `SeqAfter(s1, s2)` → `(s1 - s2) > 0 && (s1 - s2) < 2^31`.

### Cumulative ACK

Every packet carries an `AckNumber`: "I have received all packets with seq < AckNumber". On receiving ACK, all `_sendWindow` entries with seq < AckNumber are removed.

### Selective ACK (SACK)

When SACK flag is set, the ACK/DATA payload contains zero or more `SackBlock` structs (8 bytes each: `[Start uint32, End uint32)`). The receiver builds SACK blocks from contiguous ranges in `_recvBuffer`. The sender marks those sequences as `_sackedSeqs` and skips retransmitting them.

### RTT Estimation (Jacobson/Karels, RFC 6298)

```
SRTT   = (1 - α) × SRTT + α × RTT_sample       (α = 1/8)
RTTVAR = (1 - β) × RTTVAR + β × |SRTT - RTT_sample|  (β = 1/4)
RTO    = SRTT + 4 × RTTVAR
```

- First measurement: SRTT = RTT, RTTVAR = RTT/2
- On timeout: RTO doubles (exponential backoff), capped at 60s
- Min RTO: 100ms, Max RTO: 60s, Initial RTO: 1s
- **Karn's algorithm**: RTT samples only from non-retransmitted packets
- **Per-packet backoff**: timeout = `RTO × 2^(RetransmitCount - 1)`
- **Max retransmits**: 10 before abandoning connection

### Keepalive

- Interval: **10 seconds** (0 = disabled)
- PING/PONG exchange confirms liveness

## Congestion Control (AIMD, TCP Reno-style)

### Slow Start
- Initial cwnd: **10 packets**
- Each ACK of new data: `cwnd += 1` (exponential per RTT)
- Continues until `cwnd >= ssthresh`

### Congestion Avoidance
- Each full RTT worth of ACKs: `cwnd += 1` (linear growth)
- `_acksInRound` reset on each increment

### On Loss (timeout)
- `ssthresh = max(2, cwnd / 2)`
- `cwnd = 2` (minimum)
- Enter slow start

### Flight Size Limit
```
in_flight ≤ min(cwnd, peerWindow)
```

## Flow Control

Receiver advertises available buffer space in the `Window` field of every packet:
```
AvailableRecvWindow = max(0, 64 - _recvBuffer.Count)
```
Sender caps in-flight packets at `min(cwnd, advertised_window)`.

## Threading Model

| Thread | Role |
|--------|------|
| Application | Calls `Connect()`, `Send()`, `Close()`, `Receive()` |
| Receive loop | Polls UDP socket every 1ms, deserializes, dispatches to connections |
| Timer loop | Runs at 20Hz (50ms), checks retransmission timeouts, flushes send queue |

**Locking**: Each `RudpConnection` has its own `_lock`. All public methods and packet handlers acquire it.

## Key Constants

| Constant | Value | Meaning |
|----------|-------|---------|
| `PROTOCOL_VERSION` | 1 | High nibble of byte 0 |
| `HEADER_SIZE` | 20 | Fixed header bytes |
| `MAX_PACKET_SIZE` | 1500 | Max UDP payload (avoids fragmentation) |
| `MAX_PAYLOAD` | 1480 | Max app data per packet |
| `DEFAULT_WINDOW` | 64 | Sliding window size in packets |
| `INITIAL_RTO_MS` | 1000 | Initial retransmission timeout |
| `MIN_RTO_MS` | 100 | Minimum RTO |
| `MAX_RTO_MS` | 60000 | Maximum RTO |
| `MAX_RETRANSMITS` | 10 | Max retransmit attempts |
| `KEEPALIVE_MS` | 10000 | Keepalive interval |
| `INITIAL_CWND` | 10 | Initial congestion window (packets) |
| `MIN_CWND` | 2 | Minimum congestion window |

## Public API

```csharp
// Server
var socket = new RudpSocket(port);
socket.OnNewConnection += conn => {
    conn.OnDataReceived += data => { /* handle */ };
    conn.OnClosed += () => { /* cleanup */ };
    conn.OnPeerClosing += () => conn.Close();
};
socket.Start();

// Client
var conn = socket.Connect(remoteEP);
conn.OnDataReceived += data => { /* handle */ };
conn.Send(data);          // max 1480 bytes per call

// Receive
var result = socket.TryReceive();  // non-blocking
var result = socket.Receive(ct);   // blocking

// Shutdown
conn.Close();
socket.Stop();
socket.Dispose();
```

## Connection Multiplexing

Multiple connections share one UDP port, differentiated by tuple `(RemoteAddress, RemotePort, ConnectionId)`. One `RudpSocket` handles many peers simultaneously.
