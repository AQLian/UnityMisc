using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace ReliableUdp;

// ================================================================
// Selective ACK block — sent in ACK/DATA payloads when SACK flag is set.
// Each block describes a contiguous range [Start, End) that has been received.
// ================================================================

public readonly struct SackBlock
{
    public readonly uint Start; // inclusive
    public readonly uint End;   // exclusive

    public SackBlock(uint start, uint end) { Start = start; End = end; }

    public byte[] Serialize()
    {
        var buf = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(0), Start);
        BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(4), End);
        return buf;
    }

    public static SackBlock Deserialize(byte[] data, int offset)
    {
        uint start = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
        uint end = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 4));
        return new SackBlock(start, end);
    }

    public const int SerializedSize = 8;
}

// ================================================================
// RTT ESTIMATOR
//
// Uses TCP-like smoothed RTT with Jacobson/Karels algorithm (RFC 6298):
//   SRTT   = (1 - α)·SRTT + α·RTT_sample      (α = 1/8)
//   RTTVAR = (1 - β)·RTTVAR + β·|SRTT - RTT_sample|  (β = 1/4)
//   RTO    = SRTT + 4·RTTVAR
//
// On first measurement: SRTT = RTT, RTTVAR = RTT/2
// On RTO expiry: RTO *= 2 (exponential backoff), capped at MAX_RTO
// ================================================================

public class RudpRttEstimator
{
    private double _srtt;      // smoothed round-trip time (ms)
    private double _rttvar;    // round-trip time variation (ms)
    private int _currentRto;
    private bool _hasMeasurement;

    public int CurrentRto => _currentRto;
    public double SmoothedRtt => _srtt;

    public RudpRttEstimator()
    {
        _currentRto = RudpConstants.INITIAL_RTO_MS;
        _srtt = 0;
        _rttvar = 0;
        _hasMeasurement = false;
    }

    public void OnRttMeasurement(long rttMs)
    {
        if (!_hasMeasurement)
        {
            _srtt = rttMs;
            _rttvar = rttMs / 2.0;
            _hasMeasurement = true;
        }
        else
        {
            double alpha = 1.0 / 8.0;
            double beta = 1.0 / 4.0;
            _rttvar = (1 - beta) * _rttvar + beta * Math.Abs(_srtt - rttMs);
            _srtt = (1 - alpha) * _srtt + alpha * rttMs;
        }
        _currentRto = Math.Max(RudpConstants.MIN_RTO_MS,
                        Math.Min(RudpConstants.MAX_RTO_MS,
                            (int)(_srtt + 4 * _rttvar)));
    }

    /// <summary>Called when a retransmission timer expires. Applies exponential backoff.</summary>
    public void OnTimeout()
    {
        _currentRto = Math.Min(RudpConstants.MAX_RTO_MS, _currentRto * 2);
    }
}

// ================================================================
// CONGESTION CONTROL — AIMD (Additive Increase, Multiplicative Decrease)
//
// Congestion window (cwnd) limits packets in flight, measured in packets.
// Two phases:
//   Slow start:        cwnd += 1 per ACK of new data → exponential growth per RTT
//   Congestion avoidance: cwnd += 1 per RTT → linear growth
// On loss (timeout):   ssthresh = max(cwnd/2, 2), cwnd = 1, enter slow start
//
// Actual flight limit = min(cwnd, receiver_advertised_window)
// ================================================================

public class RudpCongestionControl
{
    private int _cwnd;        // congestion window (packets)
    private int _ssthresh;    // slow start threshold
    private int _acksInRound; // ACKs received in current RTT round

    public int Cwnd => _cwnd;
    public int Ssthresh => _ssthresh;

    public RudpCongestionControl()
    {
        _cwnd = RudpConstants.INITIAL_CWND;
        _ssthresh = int.MaxValue; // effectively no limit, start in slow start
        _acksInRound = 0;
    }

    /// <summary>Called for every new ACK (cumulative ACK advances).</summary>
    public void OnAckReceived()
    {
        _acksInRound++;
        if (_cwnd < _ssthresh)
        {
            // Slow start: exponential growth
            _cwnd++;
        }
        else if (_acksInRound >= _cwnd)
        {
            // Congestion avoidance: add 1 packet per full RTT
            _cwnd++;
            _acksInRound = 0;
        }
    }

    /// <summary>Called on packet loss (timeout). Multiplicative decrease.</summary>
    public void OnLoss()
    {
        _ssthresh = Math.Max(RudpConstants.MIN_CWND, _cwnd / 2);
        _cwnd = RudpConstants.MIN_CWND;
        _acksInRound = 0;
    }
}

// ================================================================
// CONNECTION — complete reliable UDP transport connection
//
// Ties together the FSM, sliding windows, RTT estimation, congestion
// control, and flow control into a single per-connection object.
//
// All public methods are thread-safe (locked internally).
// ================================================================

public class RudpConnection
{
    private readonly object _lock = new();
    private readonly int _connectionId;
    private IPEndPoint _peer;
    private RudpConnState _state;
    private readonly bool _isServer; // server-side connection IDs differ from client

    // --- Sequence numbers ---
    private uint _sendNext;     // next sequence number for new data
    private uint _sendBase;     // oldest unacknowledged seq
    private uint _recvNext;     // next expected in-order seq

    // --- Send window (selective repeat) ---
    // Packets we've sent but haven't been ACKed. Keyed by sequence number.
    private readonly Dictionary<uint, RudpPacket> _sendWindow = new();
    // Sequence numbers explicitly ACKed via SACK (beyond _sendBase)
    private readonly HashSet<uint> _sackedSeqs = new();

    // --- Receive buffer (for out-of-order packets) ---
    private readonly SortedDictionary<uint, byte[]> _recvBuffer = new();

    // --- Congestion + flow control ---
    private readonly RudpRttEstimator _rtt = new();
    private readonly RudpCongestionControl _congestion = new();
    private int _recvWindow;     // our receive window we advertise
    private int _peerWindow;     // last advertised window from peer

    // --- Last packet send time (for idle timer) ---
    private long _lastSendTimeMs;
    private DateTime _stateDeadline; // timeout for TIME_WAIT, etc.

    // --- Callbacks ---
    public event Action<byte[]>? OnDataReceived;
    public event Action? OnClosed;
    public event Action? OnPeerClosing; // peer initiated FIN — app should call Close()

    public int ConnectionId => _connectionId;
    public IPEndPoint PeerEndPoint => _peer;
    public RudpConnState State
    {
        get { lock (_lock) return _state; }
    }

    /// <summary>Outgoing send queue (application calls Send, we queue here).</summary>
    private readonly Queue<byte[]> _sendQueue = new();

    /// <summary>Packets ready to be written to the socket (filled by Process/Tick).</summary>
    public readonly Queue<(byte[] Data, IPEndPoint Dest)> OutgoingPackets = new();

    /// <summary>Delivered data waiting to be consumed by the application.</summary>
    public readonly Queue<byte[]> DeliveredData = new();

    public RudpConnection(int connectionId, IPEndPoint peer, bool isServer)
    {
        _connectionId = connectionId;
        _peer = peer;
        _isServer = isServer;
        _state = RudpConnState.Closed;
        _recvWindow = RudpConstants.DEFAULT_WINDOW;
    }

    // ================================================================
    // PUBLIC API
    // ================================================================

    /// <summary>Start connection initiation (client side). Sends SYN.</summary>
    public void Connect()
    {
        lock (_lock)
        {
            if (_state != RudpConnState.Closed) return;
            _sendNext = (uint)Random.Shared.Next();
            _sendBase = _sendNext;
            _recvNext = 0;
            _state = RudpConnState.SynSent;
            _stateDeadline = DateTime.UtcNow.AddSeconds(30);

        SendPacket(RudpPacketType.SYN, null);
    }
    }

    /// <summary>Start listening for a SYN (server side).</summary>
    public void Listen()
    {
        lock (_lock)
        {
            if (_state != RudpConnState.Closed) return;
            _state = RudpConnState.Listen;
        }
    }

    /// <summary>Queue data for reliable delivery. Returns true if queued.</summary>
    public bool Send(byte[] data)
    {
        lock (_lock)
        {
            if (_state != RudpConnState.Established || data.Length > RudpConstants.MAX_PAYLOAD)
                return false;
            _sendQueue.Enqueue(data);
            TryFlushSendQueue();
            return true;
        }
    }

    /// <summary>Initiate graceful close. Returns true if close started.</summary>
    public bool Close()
    {
        lock (_lock)
        {
            if (_state == RudpConnState.Closed || _state == RudpConnState.TimeWait) return false;
            if (_state == RudpConnState.Established)
            {
                _state = RudpConnState.FinWait1;
                SendPacket(RudpPacketType.FIN, null);
                return true;
            }
            if (_state == RudpConnState.CloseWait)
            {
                _state = RudpConnState.LastAck;
                SendPacket(RudpPacketType.FIN, null);
                return true;
            }
            // In other states, just force close
            _state = RudpConnState.Closed;
            OnClosed?.Invoke();
            return true;
        }
    }

    /// <summary>
    /// Process an incoming packet. Called by the socket receive loop.
    /// </summary>
    public void HandlePacket(RudpPacket packet, long nowMs)
    {
        lock (_lock)
        {
            // Update peer advertised window
            if (packet.Header.Window > 0)
                _peerWindow = packet.Header.Window;

            switch (packet.Header.Type)
            {
                case RudpPacketType.SYN:
                    HandleSyn(packet, nowMs);
                    break;
                case RudpPacketType.SYN_ACK:
                    HandleSynAck(packet, nowMs);
                    break;
                case RudpPacketType.ACK:
                    HandleAck(packet, nowMs);
                    break;
                case RudpPacketType.DATA:
                    HandleData(packet, nowMs);
                    break;
                case RudpPacketType.FIN:
                    HandleFin(packet, nowMs);
                    break;
                case RudpPacketType.FIN_ACK:
                    HandleFinAck(packet, nowMs);
                    break;
                case RudpPacketType.RST:
                    HandleRst();
                    break;
                case RudpPacketType.PING:
                    SendPacket(RudpPacketType.PONG, null);
                    break;
                case RudpPacketType.PONG:
                    // keepalive response received — nothing more to do
                    break;
            }
        }
    }

    /// <summary>
    /// Periodic tick. Checks retransmission timeouts and flushes send queue.
    /// Called from a timer or polling loop.
    /// </summary>
    public void Tick(long nowMs)
    {
        lock (_lock)
        {
            // Check state deadlines (TIME_WAIT, connection timeout)
            if (_state != RudpConnState.Established && _state != RudpConnState.Closed)
            {
                if (DateTime.UtcNow > _stateDeadline)
                {
                    _state = RudpConnState.Closed;
                    OnClosed?.Invoke();
                    return;
                }
            }

            // Check retransmission timeouts in send window
            var toRetransmit = new List<RudpPacket>();
            foreach (var kvp in _sendWindow)
            {
                if (_sackedSeqs.Contains(kvp.Key)) continue; // already SACKed

                long rtoForPacket = kvp.Value.RetransmitCount == 0
                    ? _rtt.CurrentRto
                    : _rtt.CurrentRto * (1 << (kvp.Value.RetransmitCount - 1)); // exponential backoff per packet

                if (nowMs - kvp.Value.SendTimestampMs >= rtoForPacket)
                {
                    toRetransmit.Add(kvp.Value);
                }
            }

            foreach (var pkt in toRetransmit)
            {
                if (pkt.RetransmitCount >= RudpConstants.MAX_RETRANSMITS)
                {
                    // Give up — close connection
                    _state = RudpConnState.Closed;
                    OnClosed?.Invoke();
                    return;
                }

                pkt.RetransmitCount++;
                pkt.SendTimestampMs = nowMs;
                pkt.Header.Flags |= (byte)RudpFlags.Retransmit;
                pkt.Header.AckNumber = _recvNext;
                pkt.Header.Window = (ushort)AvailableRecvWindow();
                pkt.Header.Checksum = 0;
                byte[] buf = new byte[RudpConstants.MAX_PACKET_SIZE];
                int len = RudpSerializer.Serialize(pkt, buf);
                OutgoingPackets.Enqueue((buf[..len], _peer));

                _rtt.OnTimeout();
                _congestion.OnLoss();
            }

            // Try to flush send queue
            TryFlushSendQueue();
        }
    }

    // ================================================================
    // PACKET HANDLERS (called under lock)
    // ================================================================

    private void HandleSyn(RudpPacket packet, long nowMs)
    {
        if (_state == RudpConnState.Listen)
        {
            _recvNext = packet.Header.SeqNumber + 1;
            _sendNext = (uint)Random.Shared.Next();
            _sendBase = _sendNext;
            _state = RudpConnState.SynReceived;
            _stateDeadline = DateTime.UtcNow.AddSeconds(30);

            SendPacket(RudpPacketType.SYN_ACK, null);
        }
        else if (_state == RudpConnState.SynSent)
        {
            // Simultaneous open — both sides sent SYN
            _recvNext = packet.Header.SeqNumber + 1;
            SendPacket(RudpPacketType.SYN_ACK, null);
        }
        // Else: duplicate SYN on established connection — re-ACK
        else if (_state == RudpConnState.Established)
        {
            SendAck();
        }
    }

    private void HandleSynAck(RudpPacket packet, long nowMs)
    {
        if (_state != RudpConnState.SynSent) return;

        _recvNext = packet.Header.SeqNumber + 1;
        _state = RudpConnState.Established;
        _lastSendTimeMs = nowMs;

        // SYN was acknowledged implicitly by SYN_ACK — clean up
        _sendWindow.Clear();
        _sackedSeqs.Clear();
        _sendBase = _sendNext;

        // Send final ACK of handshake
        SendAck();

        TryFlushSendQueue();
    }

    private void HandleAck(RudpPacket packet, long nowMs)
    {
        uint ackNum = packet.Header.AckNumber;

        // Handshake completion (server-side): SYN_RECEIVED → ESTABLISHED
        if (_state == RudpConnState.SynReceived)
        {
            _state = RudpConnState.Established;
            _sendWindow.Clear();
            _sackedSeqs.Clear();
            _lastSendTimeMs = nowMs;
            _sendBase = _sendNext;
            TryFlushSendQueue();
            return;
        }

        // Process SACK blocks if present
        if ((packet.Header.Flags & (byte)RudpFlags.SACK) != 0 && packet.Payload != null)
        {
            int offset = 0;
            while (offset + SackBlock.SerializedSize <= packet.Payload.Length)
            {
                var block = SackBlock.Deserialize(packet.Payload, offset);
                offset += SackBlock.SerializedSize;
                for (uint s = block.Start; s < block.End; s++)
                    _sackedSeqs.Add(s);
            }
        }

        // Process cumulative ACK: advance send base past ACKed packets
        // Everything before ackNum is acknowledged
        while (_sendWindow.Count > 0 && !SeqAfter(_sendBase, ackNum - 1) && _sendBase != _sendNext)
        {
            // If sendBase is before ackNum, it's ACKed
            if (_sendWindow.TryGetValue(_sendBase, out var pkt))
            {
                long rtt = nowMs - pkt.SendTimestampMs;
                if (rtt > 0 && (pkt.Header.Flags & (byte)RudpFlags.Retransmit) == 0)
                {
                    _rtt.OnRttMeasurement(rtt);
                }
                _congestion.OnAckReceived();
                _sendWindow.Remove(_sendBase);
            }
            _sackedSeqs.Remove(_sendBase);
            _sendBase++;
        }

        // If we're in FinWait2 and all data is acked, we're done
        if (_state == RudpConnState.FinWait2 && _sendWindow.Count == 0)
        {
            // Wait for peer's FIN — or just go to TIME_WAIT
        }
    }

    private void HandleData(RudpPacket packet, long nowMs)
    {
        if (_state != RudpConnState.Established) return;

        uint seq = packet.Header.SeqNumber;

        // If this is ahead of our window, drop
        if (SeqAfter(seq, _recvNext + (uint)_recvWindow))
            return;

        // If already received (duplicate), just ACK
        if (seq < _recvNext || _recvBuffer.ContainsKey(seq))
        {
            SendAck();
            return;
        }

        // Store the packet (copy payload)
        if (packet.Payload != null)
            _recvBuffer[seq] = packet.Payload.ToArray();

        // Deliver in-order data
        while (_recvBuffer.TryGetValue(_recvNext, out var data))
        {
            _recvBuffer.Remove(_recvNext);
            DeliveredData.Enqueue(data);
            OnDataReceived?.Invoke(data);
            _recvNext++;
        }

        // Build and send ACK (possibly with SACK)
        SendAckWithSack();
    }

    private void HandleFin(RudpPacket packet, long nowMs)
    {
        uint seq = packet.Header.SeqNumber;

        if (_state == RudpConnState.Established)
        {
            _recvNext = seq + 1;
            _state = RudpConnState.CloseWait;
            SendPacket(RudpPacketType.FIN_ACK, null); // acknowledge peer's FIN
            OnPeerClosing?.Invoke();
        }
        else if (_state == RudpConnState.FinWait1)
        {
            // Simultaneous close
            _recvNext = seq + 1;
            _state = RudpConnState.Closing;
            SendAck();
        }
        else if (_state == RudpConnState.FinWait2)
        {
            // Expected FIN — ACK and go to TIME_WAIT
            _recvNext = seq + 1;
            _state = RudpConnState.TimeWait;
            _stateDeadline = DateTime.UtcNow.AddMilliseconds(2 * RudpConstants.INITIAL_RTO_MS);
            SendPacket(RudpPacketType.FIN_ACK, null);
        }
        else if (_state == RudpConnState.TimeWait)
        {
            // Duplicate FIN — re-ACK
            SendPacket(RudpPacketType.FIN_ACK, null);
        }
    }

    private void HandleFinAck(RudpPacket packet, long nowMs)
    {
        if (_state == RudpConnState.FinWait1)
        {
            _state = RudpConnState.FinWait2;
            // Wait for peer's FIN
        }
        else if (_state == RudpConnState.LastAck)
        {
            _state = RudpConnState.Closed;
            OnClosed?.Invoke();
        }
        else if (_state == RudpConnState.Closing)
        {
            // Simultaneous close complete
            _state = RudpConnState.TimeWait;
            _stateDeadline = DateTime.UtcNow.AddMilliseconds(2 * RudpConstants.INITIAL_RTO_MS);
        }
    }

    private void HandleRst()
    {
        _state = RudpConnState.Closed;
        _sendWindow.Clear();
        _recvBuffer.Clear();
        _sackedSeqs.Clear();
        OnClosed?.Invoke();
    }

    // ================================================================
    // SENDING HELPERS
    // ================================================================

    private void TryFlushSendQueue()
    {
        while (_sendQueue.Count > 0 && CanSend())
        {
            byte[] data = _sendQueue.Dequeue();
            uint seq = _sendNext++;
            var pkt = new RudpPacket
            {
                Header = new RudpHeader
                {
                    Version = RudpConstants.PROTOCOL_VERSION,
                    Type = RudpPacketType.DATA,
                    ConnectionId = (uint)_connectionId,
                    SeqNumber = seq,
                    AckNumber = _recvNext,
                    Window = (ushort)AvailableRecvWindow(),
                },
                Payload = data,
                SendTimestampMs = 0, // will be set on actual send
            };
            _sendWindow[seq] = pkt;

            byte[] buf = new byte[RudpConstants.MAX_PACKET_SIZE];
            int len = RudpSerializer.Serialize(pkt, buf);
            pkt.SendTimestampMs = Environment.TickCount64;
            OutgoingPackets.Enqueue((buf[..len], _peer));
            _lastSendTimeMs = Environment.TickCount64;
        }
    }

    /// <summary>Can we send another new packet? Respects cwnd and peer window.</summary>
    private bool CanSend()
    {
        int flightSize = _sendWindow.Count;
        int limit = Math.Min(_congestion.Cwnd, _peerWindow > 0 ? _peerWindow : RudpConstants.DEFAULT_WINDOW);
        return flightSize < limit;
    }

    private void SendPacket(RudpPacketType type, byte[]? payload)
    {
        uint seq = 0;
        if (type == RudpPacketType.SYN || type == RudpPacketType.SYN_ACK || type == RudpPacketType.FIN)
        {
            seq = _sendNext++;
        }

        var pkt = new RudpPacket
        {
            Header = new RudpHeader
            {
                Version = RudpConstants.PROTOCOL_VERSION,
                Type = type,
                ConnectionId = (uint)_connectionId,
                SeqNumber = seq,
                AckNumber = _recvNext,
                Window = (ushort)AvailableRecvWindow(),
            },
            Payload = payload,
            SendTimestampMs = Environment.TickCount64,
        };

        // For SYN/FIN, track in send window for retransmission
        if (type == RudpPacketType.SYN || type == RudpPacketType.FIN)
            _sendWindow[seq] = pkt;

        byte[] buf = new byte[RudpConstants.MAX_PACKET_SIZE];
        int len = RudpSerializer.Serialize(pkt, buf);
        OutgoingPackets.Enqueue((buf[..len], _peer));
        _lastSendTimeMs = Environment.TickCount64;
    }

    private void SendAck()
    {
        var pkt = new RudpPacket
        {
            Header = new RudpHeader
            {
                Version = RudpConstants.PROTOCOL_VERSION,
                Type = RudpPacketType.ACK,
                ConnectionId = (uint)_connectionId,
                SeqNumber = _sendNext, // don't increment for pure ACK
                AckNumber = _recvNext,
                Window = (ushort)AvailableRecvWindow(),
            },
            SendTimestampMs = Environment.TickCount64,
        };
        byte[] buf = new byte[RudpConstants.MAX_PACKET_SIZE];
        int len = RudpSerializer.Serialize(pkt, buf);
        OutgoingPackets.Enqueue((buf[..len], _peer));
    }

    /// <summary>Send ACK with SACK blocks for out-of-order received packets.</summary>
    private void SendAckWithSack()
    {
        var sackBlocks = BuildSackBlocks();
        byte[]? sackPayload = null;
        if (sackBlocks.Count > 0)
        {
            sackPayload = new byte[sackBlocks.Count * SackBlock.SerializedSize];
            for (int i = 0; i < sackBlocks.Count; i++)
            {
                var blockBytes = sackBlocks[i].Serialize();
                Array.Copy(blockBytes, 0, sackPayload, i * SackBlock.SerializedSize, SackBlock.SerializedSize);
            }
        }

        var pkt = new RudpPacket
        {
            Header = new RudpHeader
            {
                Version = RudpConstants.PROTOCOL_VERSION,
                Type = sackBlocks.Count > 0 ? RudpPacketType.DATA : RudpPacketType.ACK,
                Flags = sackBlocks.Count > 0 ? (byte)RudpFlags.SACK : (byte)0,
                ConnectionId = (uint)_connectionId,
                SeqNumber = _sendNext,
                AckNumber = _recvNext,
                Window = (ushort)AvailableRecvWindow(),
            },
            Payload = sackPayload,
            SendTimestampMs = Environment.TickCount64,
        };
        // If we're piggybacking SACK on DATA but have no data, use pure ACK
        if (sackBlocks.Count > 0)
            pkt.Header.Type = RudpPacketType.ACK;

        byte[] buf = new byte[RudpConstants.MAX_PACKET_SIZE];
        int len = RudpSerializer.Serialize(pkt, buf);
        OutgoingPackets.Enqueue((buf[..len], _peer));
    }

    private List<SackBlock> BuildSackBlocks()
    {
        var blocks = new List<SackBlock>();
        if (_recvBuffer.Count == 0) return blocks;

        uint? blockStart = null;
        uint? blockEnd = null;

        foreach (uint seq in _recvBuffer.Keys)
        {
            if (blockStart == null)
            {
                blockStart = seq;
                blockEnd = seq + 1;
            }
            else if (seq == blockEnd)
            {
                blockEnd = seq + 1;
            }
            else
            {
                blocks.Add(new SackBlock(blockStart!.Value, blockEnd!.Value));
                blockStart = seq;
                blockEnd = seq + 1;
            }
        }
        if (blockStart != null)
            blocks.Add(new SackBlock(blockStart.Value, blockEnd!.Value));

        return blocks;
    }

    private int AvailableRecvWindow()
    {
        return Math.Max(0, RudpConstants.DEFAULT_WINDOW - _recvBuffer.Count);
    }

    // ================================================================
    // SEQUENCE NUMBER UTILITIES
    // ================================================================

    /// <summary>Returns true if s1 comes after s2 in the 32-bit sequence space (with wrap).</summary>
    private static bool SeqAfter(uint s1, uint s2)
    {
        // If the difference is less than half the sequence space and positive, s1 > s2
        long diff = (long)s1 - (long)s2;
        return diff > 0 && diff < (long)(uint.MaxValue / 2);
    }
}

// ================================================================
// SOCKET I/O LAYER — wraps .NET UDP socket, dispatches to connections
// ================================================================

public class RudpSocket : IDisposable
{
    private readonly Socket _socket;
    private readonly CancellationTokenSource _cts = new();
    // Key = "IP:Port:ConnId" — each (remote endpoint, connection_id) pair is a unique connection
    private readonly Dictionary<string, RudpConnection> _connections = new();
    private readonly Queue<(int ConnId, byte[] Data)> _pendingDelivers = new();

    public RudpSocket(int listenPort = 0)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
        _socket.Blocking = false;
    }

    public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint!;

    /// <summary>Fires when a new connection is accepted (server-side, after SYN received).</summary>
    public event Action<RudpConnection>? OnNewConnection;

    /// <summary>Look up an existing connection by remote endpoint and connection ID.</summary>
    public RudpConnection? GetConnection(IPEndPoint remote, int connId)
    {
        lock (_connections) { _connections.TryGetValue(MakeKey(remote, connId), out var c); return c; }
    }

    /// <summary>Create a client connection and connect to a remote endpoint.</summary>
    public RudpConnection Connect(IPEndPoint remote)
    {
        int connId = Random.Shared.Next(1, int.MaxValue);
        var conn = new RudpConnection(connId, remote, false);
        lock (_connections) { _connections[MakeKey(remote, connId)] = conn; }
        conn.Connect();
        return conn;
    }

    /// <summary>Start the receive + timer loop. Non-blocking; runs on background thread.</summary>
    public void Start()
    {
        Task.Run(() => ReceiveLoop(_cts.Token));
        Task.Run(() => TimerLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts.Cancel();
        _socket.Close();
    }

    public void Dispose()
    {
        Stop();
        _socket.Dispose();
        _cts.Dispose();
    }

    private static string MakeKey(IPEndPoint ep, int connId) =>
        $"{ep.Address}:{ep.Port}:{connId}";

    // ================================================================
    // RECEIVE LOOP
    // ================================================================

    private void ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[RudpConstants.MAX_PACKET_SIZE];
        EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_socket.Poll(1000, SelectMode.SelectRead)) continue;

                int received = _socket.ReceiveFrom(buffer, ref remoteEp);
                if (received <= 0) continue;

                var packet = RudpSerializer.Deserialize(buffer, 0, received);
                if (packet == null) continue; // checksum failed or malformed

                long nowMs = Environment.TickCount64;
                IPEndPoint remote = (IPEndPoint)remoteEp;
                Console.Error.WriteLine($"[RECV] Packet: type={packet.Header.Type} seq={packet.Header.SeqNumber} ack={packet.Header.AckNumber} from={remote}");

                int connId = (int)packet.Header.ConnectionId;
                string key = MakeKey(remote, connId);

                RudpConnection? conn;
                lock (_connections)
                {
                    if (!_connections.TryGetValue(key, out conn))
                    {
                        // Only create a new connection for SYN packets
                        if (packet.Header.Type == RudpPacketType.SYN)
                        {
                            conn = new RudpConnection(connId, remote, true);
                            conn.Listen();
                            _connections[key] = conn;
                            OnNewConnection?.Invoke(conn);
                        }
                        else continue;
                    }
                }

                conn.HandlePacket(packet, nowMs);

                // Drain outgoing packets
                while (conn.OutgoingPackets.TryDequeue(out var outPkt))
                    _socket.SendTo(outPkt.Data, outPkt.Dest);

                // Drain delivered data
                while (conn.DeliveredData.TryDequeue(out var data))
                    _pendingDelivers.Enqueue((conn.ConnectionId, data));
            }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { break; }
        }
    }

    // ================================================================
    // TIMER LOOP
    // ================================================================

    private void TimerLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Thread.Sleep(50); // 20Hz tick

            long nowMs = Environment.TickCount64;
            List<RudpConnection> conns;
            lock (_connections) { conns = _connections.Values.ToList(); }

            foreach (var conn in conns)
            {
                conn.Tick(nowMs);

                // Drain outgoing packets
                while (conn.OutgoingPackets.TryDequeue(out var outPkt))
                {
                    try { _socket.SendTo(outPkt.Data, outPkt.Dest); }
                    catch { /* UDP send failed — likely buffer full, will retry next tick */ }
                }

                // Drain delivered data
                while (conn.DeliveredData.TryDequeue(out var data))
                    _pendingDelivers.Enqueue((conn.ConnectionId, data));
            }
        }
    }

    /// <summary>Try to get a delivered data packet from any connection. Returns null if none available.</summary>
    public (int ConnId, byte[] Data)? TryReceive()
    {
        if (_pendingDelivers.TryDequeue(out var result))
            return result;
        return null;
    }

    /// <summary>Block until data arrives from any connection.</summary>
    public (int ConnId, byte[] Data) Receive(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = TryReceive();
            if (result.HasValue) return result.Value;
            Thread.Sleep(10);
        }
        throw new OperationCanceledException();
    }
}
