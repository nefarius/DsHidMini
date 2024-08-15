using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Raw input report as it is sent by the SIXAXIS/DualShock 3.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Ds3RawInputReport
{
    //
    // Report ID (always 0x01)
    // 
    public byte ReportId;

    public byte Reserved0;

    //
    // Button breakouts in various formats
    // 
    [StructLayout(LayoutKind.Explicit)]
    public struct ButtonUnion
    {
        [FieldOffset(0)]
        public uint lButtons;

        [FieldOffset(0)]
        public fixed byte bButtons[4];

        [FieldOffset(0)]
        private ushort packedButtons;

        public bool Select { get => GetBit(packedButtons, 0); set => SetBit(ref packedButtons, 0, value); }
        public bool L3 { get => GetBit(packedButtons, 1); set => SetBit(ref packedButtons, 1, value); }
        public bool R3 { get => GetBit(packedButtons, 2); set => SetBit(ref packedButtons, 2, value); }
        public bool Start { get => GetBit(packedButtons, 3); set => SetBit(ref packedButtons, 3, value); }
        public bool Up { get => GetBit(packedButtons, 4); set => SetBit(ref packedButtons, 4, value); }
        public bool Right { get => GetBit(packedButtons, 5); set => SetBit(ref packedButtons, 5, value); }
        public bool Down { get => GetBit(packedButtons, 6); set => SetBit(ref packedButtons, 6, value); }
        public bool Left { get => GetBit(packedButtons, 7); set => SetBit(ref packedButtons, 7, value); }

        public bool L2 { get => GetBit(packedButtons, 8); set => SetBit(ref packedButtons, 8, value); }
        public bool R2 { get => GetBit(packedButtons, 9); set => SetBit(ref packedButtons, 9, value); }
        public bool L1 { get => GetBit(packedButtons, 10); set => SetBit(ref packedButtons, 10, value); }
        public bool R1 { get => GetBit(packedButtons, 11); set => SetBit(ref packedButtons, 11, value); }
        public bool Triangle { get => GetBit(packedButtons, 12); set => SetBit(ref packedButtons, 12, value); }
        public bool Circle { get => GetBit(packedButtons, 13); set => SetBit(ref packedButtons, 13, value); }
        public bool Cross { get => GetBit(packedButtons, 14); set => SetBit(ref packedButtons, 14, value); }
        public bool Square { get => GetBit(packedButtons, 15); set => SetBit(ref packedButtons, 15, value); }
        public bool PS { get => GetBit(packedButtons, 16); set => SetBit(ref packedButtons, 16, value); }

        // Helper methods to manipulate individual bits
        private static bool GetBit(ushort value, int bitNumber)
        {
            return (value & (1 << bitNumber)) != 0;
        }

        private static void SetBit(ref ushort value, int bitNumber, bool bitValue)
        {
            if (bitValue)
            {
                value |= (ushort)(1 << bitNumber);
            }
            else
            {
                value &= (ushort)~(1 << bitNumber);
            }
        }
    }

    public ButtonUnion Buttons;

    //
    // Left Thumb Axes (0x00 = left/bottom, 0x80 = centered, 0xFF = right/top)
    // 
    public byte LeftThumbX;
    public byte LeftThumbY;

    //
    // Right Thumb Axes (0x00 = left/bottom, 0x80 = centered, 0xFF = right/top)
    // 
    public byte RightThumbX;
    public byte RightThumbY;

    public fixed byte Reserved1[4];

    //
    // Pressure value breakouts in various formats
    // 
    [StructLayout(LayoutKind.Explicit)]
    public struct PressureUnion
    {
        [FieldOffset(0)]
        public fixed byte bValues[12];

        [FieldOffset(0)]
        public PressureValues Values;

        [StructLayout(LayoutKind.Sequential)]
        public struct PressureValues
        {
            public byte Up;
            public byte Right;
            public byte Down;
            public byte Left;

            public byte L2;
            public byte R2;
            public byte L1;
            public byte R1;

            public byte Triangle;
            public byte Circle;
            public byte Cross;
            public byte Square;
        }
    }

    public PressureUnion Pressure;

    public fixed byte Reserved2[4];

    //
    // Battery charge status
    // 
    public byte BatteryStatus;

    public fixed byte Reserved3[10];

    //
    // Motion information
    // 
    public ushort AccelerometerX;
    public ushort AccelerometerY;
    public ushort AccelerometerZ;
    public ushort Gyroscope;
}