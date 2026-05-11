using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ReliableUdp;

// ================================================================
// PACKET SERIALIZATION / DESERIALIZATION
// 
// Converts RudpPacket ↔ byte[] for socket I/O.
// All multi-byte fields are sent in network byte order (big-endian).
// 
// DESIGN RATIONALE for checksum:
//   UDP's own checksum is optional in IPv4. Adding our own 16-bit
//   Internet checksum guarantees end-to-end data integrity regardless
//   of UDP configuration, and catches bugs in intermediate buffers.
// ================================================================

public static class RudpSerializer
{
    public static int Serialize(RudpPacket packet, byte[] buffer, int offset = 0)
    {
        int headerSize = RudpConstants.HEADER_SIZE;
        int payloadLen = packet.Payload?.Length ?? 0;
        int totalLen = headerSize + payloadLen;

        // Write header fields in network byte order
        buffer[offset + 0] = packet.Header.VersionType;
        buffer[offset + 1] = packet.Header.Flags;
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 2), packet.Header.ConnectionId);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 6), packet.Header.SeqNumber);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 10), packet.Header.AckNumber);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 14), packet.Header.Window);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 16), (ushort)payloadLen);

        // Copy payload
        if (payloadLen > 0)
            Array.Copy(packet.Payload!, 0, buffer, offset + headerSize, payloadLen);

        // Compute and write checksum (over header[0..17] + payload, header[18..19] zeroed during compute)
        ushort checksum = ComputeChecksum(buffer, offset, headerSize + payloadLen);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 18), checksum);

        return totalLen;
    }

    public static RudpPacket? Deserialize(byte[] buffer, int offset, int length)
    {
        if (length < RudpConstants.HEADER_SIZE)
            return null;

        // Verify checksum
        ushort receivedChecksum = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 18));
        ushort computed = ComputeChecksum(buffer, offset, length);
        if (receivedChecksum != 0 && computed != receivedChecksum)
            return null; // checksum mismatch, drop packet

        var packet = new RudpPacket();

        packet.Header.VersionType = buffer[offset + 0];
        packet.Header.Flags = buffer[offset + 1];
        packet.Header.ConnectionId = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 2));
        packet.Header.SeqNumber = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 6));
        packet.Header.AckNumber = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 10));
        packet.Header.Window = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 14));
        packet.Header.PayloadLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 16));

        int payloadLen = packet.Header.PayloadLength;
        if (payloadLen > 0 && offset + RudpConstants.HEADER_SIZE + payloadLen <= length)
        {
            packet.Payload = new byte[payloadLen];
            Array.Copy(buffer, offset + RudpConstants.HEADER_SIZE, packet.Payload, 0, payloadLen);
        }

        return packet;
    }

    /// <summary>
    /// Internet checksum (RFC 1071): one's complement of the one's complement sum
    /// of all 16-bit words. Covers header bytes [0..17] + payload. The checksum
    /// field itself (bytes 18..19) is zeroed during computation.
    /// 
    /// Returns 0 if the checksum is valid (the sum of all words including the 
    /// checksum word equals 0xFFFF in one's complement).
    /// </summary>
    public static ushort ComputeChecksum(byte[] buffer, int offset, int length)
    {
        // Zero out the checksum field during computation
        byte savedHigh = buffer[offset + 18];
        byte savedLow = buffer[offset + 19];
        buffer[offset + 18] = 0;
        buffer[offset + 19] = 0;

        uint sum = 0;
        int end = offset + length;

        // Sum 16-bit words
        for (int i = offset; i < end - 1; i += 2)
        {
            sum += (uint)((buffer[i] << 8) | buffer[i + 1]);
        }
        // Odd byte: pad with zero
        if ((length & 1) != 0)
        {
            sum += (uint)(buffer[offset + length - 1] << 8);
        }

        // Fold carries
        while (sum > 0xFFFF)
            sum = (sum & 0xFFFF) + (sum >> 16);

        ushort checksum = (ushort)(~sum);

        // Restore original bytes
        buffer[offset + 18] = savedHigh;
        buffer[offset + 19] = savedLow;

        return checksum;
    }
}
