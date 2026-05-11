using System.Runtime.InteropServices;

namespace ReliableUdp;

// ================================================================
// CONSTANTS
// ================================================================

public static class RudpConstants
{
    public const byte PROTOCOL_VERSION = 1;

    /// <summary>Fixed 20-byte header on every packet.</summary>
    public const int HEADER_SIZE = 20;

    /// <summary>Maximum UDP payload: 65535 - IP(20) - UDP(8) = 65507. We cap at 1500 to avoid fragmentation.</summary>
    public const int MAX_PACKET_SIZE = 1500;

    /// <summary>Maximum application data per packet: MAX_PACKET - HEADER = 1480.</summary>
    public const int MAX_PAYLOAD = MAX_PACKET_SIZE - HEADER_SIZE; // 1480

    /// <summary>Default sliding window size in packets (both send and receive).</summary>
    public const int DEFAULT_WINDOW = 64;

    /// <summary>Maximum sequence number before wrapping.</summary>
    public const uint SEQ_WRAP = uint.MaxValue;

    /// <summary>Initial retransmission timeout in milliseconds.</summary>
    public const int INITIAL_RTO_MS = 1000;

    /// <summary>Minimum RTO in milliseconds.</summary>
    public const int MIN_RTO_MS = 100;

    /// <summary>Maximum RTO in milliseconds.</summary>
    public const int MAX_RTO_MS = 60000;

    /// <summary>Max retransmissions before giving up.</summary>
    public const int MAX_RETRANSMITS = 10;

    /// <summary>Keepalive interval in milliseconds (0 = disabled).</summary>
    public const int KEEPALIVE_MS = 10000;

    /// <summary>Initial congestion window in packets. RFC 6928: initcwnd = 10.</summary>
    public const int INITIAL_CWND = 10;

    /// <summary>Minimum congestion window.</summary>
    public const int MIN_CWND = 2;
}

// ================================================================
// PACKET TYPE — determines how the packet body is interpreted
// ================================================================

/// <summary>
/// The 4-bit packet type field. Defines the semantic meaning of the packet.
/// Extracted from the low nibble of byte 0 in the header.
/// </summary>
public enum RudpPacketType : byte
{
    /// <summary>Connection initiation. Contains no payload. Starts 3-way handshake.</summary>
    SYN = 0x01,

    /// <summary>Accept connection. No payload. Responds to SYN.</summary>
    SYN_ACK = 0x02,

    /// <summary>Pure acknowledgment. Payload may contain SACK blocks.</summary>
    ACK = 0x03,

    /// <summary>Application data. Payload is user bytes.</summary>
    DATA = 0x04,

    /// <summary>Connection teardown request. No payload.</summary>
    FIN = 0x05,

    /// <summary>Acknowledgment of FIN. No payload.</summary>
    FIN_ACK = 0x06,

    /// <summary>Force-reset connection. No payload.</summary>
    RST = 0x07,

    /// <summary>Keepalive probe. No payload.</summary>
    PING = 0x08,

    /// <summary>Keepalive response. No payload.</summary>
    PONG = 0x09,
}

// ================================================================
// PACKET FLAGS — modifiers applicable to any packet type
// ================================================================

/// <summary>
/// Bit flags stored in byte 1 of the header.
/// Modifiers that apply to any packet type.
/// </summary>
[Flags]
public enum RudpFlags : byte
{
    None = 0x00,
    /// <summary>Payload contains Selective ACK blocks (for DATA/ACK types).</summary>
    SACK = 0x01,
    /// <summary>This packet is a retransmission.</summary>
    Retransmit = 0x02,
    /// <summary>The sender has more data immediately following (not used for MESSAGE_BEGIN/END since we are packet-oriented).</summary>
    More = 0x04,
}

// ================================================================
// CONNECTION STATES — finite state machine
// ================================================================

public enum RudpConnState
{
    Closed,
    Listen,
    SynSent,
    SynReceived,
    Established,
    FinWait1,
    FinWait2,
    CloseWait,
    LastAck,
    TimeWait,
    Closing,
}

// ================================================================
// PACKET HEADER STRUCT — 20 bytes, packed, network byte order
// ================================================================

/// <summary>
/// Every RUDP packet starts with this 20-byte header.
/// Layout is designed for fixed 4-byte alignment of sequence numbers
/// to minimize misaligned reads on CPUs that penalize it.
/// 
/// Byte layout:
///   [0]     VersionType  — high nibble=version, low nibble=packet type
///   [1]     Flags        — bit flags (SACK, retransmit, etc.)
///   [2..5]  ConnectionId — 32-bit, uniquely identifies this connection
///   [6..9]  SeqNumber    — 32-bit, monotonic packet sequence number
///   [10..13] AckNumber   — 32-bit, cumulative ACK ("I have everything before this")
///   [14..15] Window      — 16-bit, receiver's buffer capacity in packets
///   [16..17] PayloadLen  — 16-bit, payload bytes following header
///   [18..19] Checksum    — 16-bit, Internet checksum of header+payload
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RudpHeader
{
    public byte VersionType;    // Offset 0
    public byte Flags;          // Offset 1
    public uint ConnectionId;   // Offset 2  (network order)
    public uint SeqNumber;      // Offset 6  (network order)
    public uint AckNumber;      // Offset 10 (network order)
    public ushort Window;       // Offset 14 (network order)
    public ushort PayloadLength; // Offset 16 (network order)
    public ushort Checksum;     // Offset 18 (network order)

    // Helpers
    public byte Version
    {
        readonly get => (byte)(VersionType >> 4);
        set => VersionType = (byte)((value << 4) | (VersionType & 0x0F));
    }

    public RudpPacketType Type
    {
        readonly get => (RudpPacketType)(VersionType & 0x0F);
        set => VersionType = (byte)((VersionType & 0xF0) | ((byte)value & 0x0F));
    }
}

/// <summary>
/// A complete RUDP packet: header + raw payload bytes.
/// </summary>
public class RudpPacket
{
    public RudpHeader Header;
    public byte[]? Payload;   // null for control packets
    public int TotalLength => RudpConstants.HEADER_SIZE + (Payload?.Length ?? 0);

    // Timing: when this packet was sent (for RTT measurement)
    public long SendTimestampMs;
    // Retry counter
    public int RetransmitCount;

    public RudpPacket Clone()
    {
        var p = new RudpPacket
        {
            Header = Header,
            SendTimestampMs = SendTimestampMs,
            RetransmitCount = RetransmitCount,
        };
        if (Payload != null)
        {
            p.Payload = new byte[Payload.Length];
            Array.Copy(Payload, p.Payload, Payload.Length);
        }
        return p;
    }
}
