using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Raw input report as it is sent by the SIXAXIS/DualShock 3.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public unsafe struct DS3_RAW_INPUT_REPORT
{
    /// <summary>
    ///     Report ID (always 1).
    /// </summary>
    public byte ReportId;

    internal byte Reserved0;

    /// <summary>
    ///     Button breakouts in various formats.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public struct ButtonUnion
    {
        [FieldOffset(0)]
        internal uint lButtons;

        [FieldOffset(0)]
        internal fixed byte bButtons[4];

        [FieldOffset(0)]
        internal ushort packedButtons;

        /// <summary>
        ///     Gets or sets the Select button state.
        /// </summary>
        public bool Select { get => GetBit(packedButtons, 0); set => SetBit(ref packedButtons, 0, value); }

        /// <summary>
        ///     Gets or sets the Left Thumb button state.
        /// </summary>
        public bool L3 { get => GetBit(packedButtons, 1); set => SetBit(ref packedButtons, 1, value); }

        /// <summary>
        ///     Gets or sets the Right Thumb button state.
        /// </summary>
        public bool R3 { get => GetBit(packedButtons, 2); set => SetBit(ref packedButtons, 2, value); }

        /// <summary>
        ///     Gets or sets the Start button state.
        /// </summary>
        public bool Start { get => GetBit(packedButtons, 3); set => SetBit(ref packedButtons, 3, value); }

        /// <summary>
        ///     Gets or sets the DPad Up button state.
        /// </summary>
        public bool Up { get => GetBit(packedButtons, 4); set => SetBit(ref packedButtons, 4, value); }

        /// <summary>
        ///     Gets or sets the DPad Right button state.
        /// </summary>
        public bool Right { get => GetBit(packedButtons, 5); set => SetBit(ref packedButtons, 5, value); }

        /// <summary>
        ///     Gets or sets the DPad Down button state.
        /// </summary>
        public bool Down { get => GetBit(packedButtons, 6); set => SetBit(ref packedButtons, 6, value); }

        /// <summary>
        ///     Gets or sets the DPad Left button state.
        /// </summary>
        public bool Left { get => GetBit(packedButtons, 7); set => SetBit(ref packedButtons, 7, value); }

        /// <summary>
        ///     Gets or sets the Left Trigger button state.
        /// </summary>
        public bool L2 { get => GetBit(packedButtons, 8); set => SetBit(ref packedButtons, 8, value); }

        /// <summary>
        ///     Gets or sets the Right Trigger button state.
        /// </summary>
        public bool R2 { get => GetBit(packedButtons, 9); set => SetBit(ref packedButtons, 9, value); }

        /// <summary>
        ///     Gets or sets the Left Shoulder button state.
        /// </summary>
        public bool L1 { get => GetBit(packedButtons, 10); set => SetBit(ref packedButtons, 10, value); }

        /// <summary>
        ///     Gets or sets the Right Shoulder button state.
        /// </summary>
        public bool R1 { get => GetBit(packedButtons, 11); set => SetBit(ref packedButtons, 11, value); }

        /// <summary>
        ///     Gets or sets the Triangle button state.
        /// </summary>
        public bool Triangle { get => GetBit(packedButtons, 12); set => SetBit(ref packedButtons, 12, value); }

        /// <summary>
        ///     Gets or sets the Circle button state.
        /// </summary>
        public bool Circle { get => GetBit(packedButtons, 13); set => SetBit(ref packedButtons, 13, value); }

        /// <summary>
        ///     Gets or sets the Cross button state.
        /// </summary>
        public bool Cross { get => GetBit(packedButtons, 14); set => SetBit(ref packedButtons, 14, value); }

        /// <summary>
        ///     Gets or sets the Square button state.
        /// </summary>
        public bool Square { get => GetBit(packedButtons, 15); set => SetBit(ref packedButtons, 15, value); }

        /// <summary>
        ///     Gets or sets the PS button state.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
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

    /// <summary>
    ///     The individual controller buttons.
    /// </summary>
    public ButtonUnion Buttons;

    /// <summary>
    ///     Left Thumb X-axis.
    /// </summary>
    /// <remarks>0x00 = left/bottom, 0x80 = centered, 0xFF = right/top</remarks>
    public byte LeftThumbX;

    /// <summary>
    ///     Left Thumb Y-axis.
    /// </summary>
    /// <remarks>0x00 = left/bottom, 0x80 = centered, 0xFF = right/top</remarks>
    public byte LeftThumbY;

    /// <summary>
    ///     Right Thumb X-axis.
    /// </summary>
    /// <remarks>0x00 = left/bottom, 0x80 = centered, 0xFF = right/top</remarks>
    public byte RightThumbX;

    /// <summary>
    ///     Right Thumb Y-axis.
    /// </summary>
    /// <remarks>0x00 = left/bottom, 0x80 = centered, 0xFF = right/top</remarks>
    public byte RightThumbY;

    internal fixed byte Reserved1[4];

    /// <summary>
    ///     Pressure value breakouts in various formats.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PressureUnion
    {
        [FieldOffset(0)]
        internal fixed byte bValues[12];

        /// <summary>
        ///     The individual pressure values.
        /// </summary>
        [FieldOffset(0)]
        public PressureValues Values;

        /// <summary>
        ///     Pressure button values.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PressureValues
        {
            /// <summary>
            ///     DPad Up pressure value.
            /// </summary>
            public byte Up;

            /// <summary>
            ///     DPad Right pressure value.
            /// </summary>
            public byte Right;

            /// <summary>
            ///     DPad Down pressure value.
            /// </summary>
            public byte Down;

            /// <summary>
            ///     DPad Left pressure value.
            /// </summary>
            public byte Left;

            /// <summary>
            ///     Left Trigger pressure value.
            /// </summary>
            public byte L2;

            /// <summary>
            ///     Right Trigger pressure value.
            /// </summary>
            public byte R2;

            /// <summary>
            ///     Left Shoulder pressure value.
            /// </summary>
            public byte L1;

            /// <summary>
            ///     Right Shoulder pressure value.
            /// </summary>
            public byte R1;

            /// <summary>
            ///     Triangle pressure value.
            /// </summary>
            public byte Triangle;

            /// <summary>
            ///     Circle pressure value.
            /// </summary>
            public byte Circle;

            /// <summary>
            ///     Cross pressure value.
            /// </summary>
            public byte Cross;

            /// <summary>
            ///     Square pressure value.
            /// </summary>
            public byte Square;
        }
    }

    /// <summary>
    ///     Pressure button values.
    /// </summary>
    public PressureUnion Pressure;

    internal fixed byte Reserved2[4];

    /// <summary>
    ///     Battery charge status.
    /// </summary>
    public byte BatteryStatus;

    internal fixed byte Reserved3[10];

    //
    // Motion information
    // 
    public ushort AccelerometerX;
    public ushort AccelerometerY;
    public ushort AccelerometerZ;
    public ushort Gyroscope;
}