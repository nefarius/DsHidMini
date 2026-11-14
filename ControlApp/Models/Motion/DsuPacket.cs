using System.Buffers.Binary;
using System.Text;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     DSU protocol packet builder and parser for Cemuhook UDP communication.
/// </summary>
internal static class DsuPacket
{
    public const ushort ProtocolVersion = 1001;

    public const uint MessageVersion = 0x00100000;
    public const uint MessageControllerInfo = 0x00100001;
    public const uint MessageControllerData = 0x00100002;

    private const int HeaderSize = 16;
    private const int MessageTypeSize = 4;
    private const int MinimumPacketSize = HeaderSize + MessageTypeSize;

    /// <summary>Builds a server-side DSU packet with CRC.</summary>
    internal static byte[] BuildServerPacket(
        uint messageType,
        ReadOnlySpan<byte> payload,
        uint serverId)
    {
        int totalSize = MinimumPacketSize + payload.Length;

        if (totalSize - HeaderSize > ushort.MaxValue)
            throw new ArgumentException("DSU packet is too large.");

        byte[] packet = new byte[totalSize];

        Encoding.ASCII.GetBytes("DSUS", packet.AsSpan(0, 4));
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(4, 2), ProtocolVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(6, 2), (ushort)(totalSize - HeaderSize));
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12, 4), serverId);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(16, 4), messageType);
        payload.CopyTo(packet.AsSpan(MinimumPacketSize));

        uint crc = CalculateCrc32(packet);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8, 4), crc);

        return packet;
    }

    /// <summary>Validates and decodes a client-side DSU packet.</summary>
    internal static bool TryParseClientPacket(
        ReadOnlySpan<byte> packet,
        out uint messageType,
        out byte[] payload)
    {
        messageType = 0;
        payload = Array.Empty<byte>();

        if (packet.Length < MinimumPacketSize)
            return false;

        if (!packet[..4].SequenceEqual(Encoding.ASCII.GetBytes("DSUC")))
            return false;

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(4, 2));
        if (version != ProtocolVersion)
            return false;

        ushort declaredLength = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(6, 2));
        if (packet.Length != HeaderSize + declaredLength)
            return false;

        uint receivedCrc = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(8, 4));
        byte[] crcBuffer = packet.ToArray();
        Array.Clear(crcBuffer, 8, 4);

        uint calculatedCrc = CalculateCrc32(crcBuffer);
        if (receivedCrc != calculatedCrc)
            return false;

        messageType = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(16, 4));
        payload = packet[MinimumPacketSize..].ToArray();
        return true;
    }

    private static uint CalculateCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte value in data)
        {
            crc ^= value;

            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }

        return ~crc;
    }
}
