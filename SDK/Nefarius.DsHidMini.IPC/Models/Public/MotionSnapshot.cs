using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models.Drivers;

namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Status bits for <see cref="DsMotionSnapshot" />. Must stay in sync with
///     <c>DSHM_IPC_MOTION_FLAG_*</c> in the driver.
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum DsMotionSnapshotFlags : ushort
{
    None = 0,

    /// <summary>
    ///     The slot contains a processed sample.
    /// </summary>
    Available = 0x0001,

    /// <summary>
    ///     EEPROM page <c>0xA0</c> was not used. Nominal <c>zero=512</c>,
    ///     <c>oneG=399</c> are in effect (Bluetooth, or a failed USB read).
    /// </summary>
    Fallback = 0x0002,

    /// <summary>
    ///     The driver is writing a hardware gyro cal byte on USB output reports.
    /// </summary>
    HardwareCal = 0x0004,

    /// <summary>
    ///     Sony's auto-zero tracker is running (<c>HW_CAL</c> / <c>SIXAXIS</c>
    ///     hardware trim, or clone-heuristic <c>PLAIN_ZERO</c> software-only).
    /// </summary>
    Tracker = 0x0008,

    /// <summary>
    ///     The tracker is in software-only mode: <c>zeroRef</c> follows rest
    ///     and the hardware cal byte is never stepped. Used for clone-heuristic
    ///     <c>PLAIN_ZERO</c> pads whose EEPROM gyro zero is a template.
    /// </summary>
    SoftwareZero = 0x0010
}

/// <summary>
///     Versioned per-slot motion telemetry. Pack=1, 80 bytes; must stay in sync
///     with driver <c>IPC_MOTION_SNAPSHOT_MESSAGE</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct DsMotionSnapshot
{
    public const ushort CurrentVersion = 1;

    public const int Size = 80;

    public uint SlotIndex;

    public int SequenceNumber;

    public ushort Version;

    public DsMotionSnapshotFlags Flags;

    public ushort AccelZeroX;

    public ushort AccelZeroY;

    public ushort AccelZeroZ;

    public ushort AccelOneGX;

    public ushort AccelOneGY;

    public ushort AccelOneGZ;

    public ushort GyroZero;

    public ushort GyroEepromCal;

    public ushort RawAccelX;

    public ushort RawAccelY;

    public ushort RawAccelZ;

    public ushort RawGyro;

    public short CalAccelX;

    public short CalAccelY;

    public short CalAccelZ;

    public ushort CalGyro;

    public int AccelMilliGX;

    public int AccelMilliGY;

    public int AccelMilliGZ;

    public int GyroMilliDps;

    public int ZeroRef;

    public uint SampleIndex;

    public ulong TimestampQpc;

    public byte CalByte;

    public DsIdentificationMotionPath MotionPath;

    public byte Reserved0;

    public byte Reserved1;

    public bool IsAvailable => (Flags & DsMotionSnapshotFlags.Available) != 0;

    public bool IsFallback => (Flags & DsMotionSnapshotFlags.Fallback) != 0;

    public bool HasTracker => (Flags & DsMotionSnapshotFlags.Tracker) != 0;

    public bool HasSoftwareZero => (Flags & DsMotionSnapshotFlags.SoftwareZero) != 0;
}
