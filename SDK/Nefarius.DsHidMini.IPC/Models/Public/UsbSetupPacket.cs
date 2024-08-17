using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Describes a USB control transfer setup packet.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public struct WDF_USB_CONTROL_SETUP_PACKET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RequestStruct
    {
        private byte _byte;

        public byte Recipient
        {
            get => (byte)(_byte & 0x03); // Mask for the lowest 2 bits
            set => _byte = (byte)((_byte & ~0x03) | (value & 0x03));
        }

        public byte Reserved
        {
            get => (byte)((_byte >> 2) & 0x07); // Next 3 bits
            set => _byte = (byte)((_byte & ~(0x07 << 2)) | ((value & 0x07) << 2));
        }

        public byte Type
        {
            get => (byte)((_byte >> 5) & 0x03); // Next 2 bits
            set => _byte = (byte)((_byte & ~(0x03 << 5)) | ((value & 0x03) << 5));
        }

        public byte Dir
        {
            get => (byte)((_byte >> 7) & 0x01); // The highest bit
            set => _byte = (byte)((_byte & ~(0x01 << 7)) | ((value & 0x01) << 7));
        }

        public byte Byte
        {
            get => _byte;
            set => _byte = value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BytesStruct
    {
        public byte LowByte;
        public byte HiByte;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PacketStruct
    {
        [FieldOffset(0)]
        public RequestStruct bm;

        [FieldOffset(1)]
        public byte bRequest;

        [FieldOffset(2)]
        internal BytesStruct wValueBytes;

        [FieldOffset(4)]
        internal BytesStruct wIndexBytes;

        [FieldOffset(6)]
        public ushort wLength;

        public ushort wValue
        {
            get => (ushort)((wValueBytes.HiByte << 8) | wValueBytes.LowByte);
            set
            {
                wValueBytes.LowByte = (byte)(value & 0xFF);
                wValueBytes.HiByte = (byte)((value >> 8) & 0xFF);
            }
        }

        public ushort wIndex
        {
            get => (ushort)((wIndexBytes.HiByte << 8) | wIndexBytes.LowByte);
            set
            {
                wIndexBytes.LowByte = (byte)(value & 0xFF);
                wIndexBytes.HiByte = (byte)((value >> 8) & 0xFF);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct GenericStruct
    {
        public fixed byte Bytes[8];
    }

    [FieldOffset(0)]
    public PacketStruct Packet;

    [FieldOffset(0)]
    public GenericStruct Generic;
}