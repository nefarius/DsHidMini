using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models;

/// <summary>
///     Mirrors the driver diagnostics layout constants (IPC.h).
/// </summary>
internal static class DiagnosticsLayout
{
    public const uint Version = 1;
    public const int DeviceDescriptorLength = 18;
    public const int ConfigDescriptorMax = 256;
    public const int MaxPipes = 8;
    public const int StringCount = 3;
    public const int StringChars = 64;
    public const int FeatureCount = 5;
    public const int EepromPageCount = 16;
    public const int ReportLength = 64;
}

/// <summary>
///     Request for the USB diagnostics sweep.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 Version;

    public UInt32 Flags;
}

/// <summary>
///     One raw feature report or EEPROM page.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal unsafe struct DSHM_IPC_DIAG_REPORT
{
    public byte Id;
    public byte Reserved;
    public ushort Length;
    public UInt32 SetStatus;
    public UInt32 GetStatus;
    public fixed byte Data[DiagnosticsLayout.ReportLength];
}

/// <summary>
///     One USB string descriptor (UTF-16 text).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal unsafe struct DSHM_IPC_DIAG_STRING
{
    public byte Index;
    public byte Reserved;
    public ushort Length;
    public UInt32 Status;
    public fixed char Text[DiagnosticsLayout.StringChars];
}

/// <summary>
///     One configured USB pipe.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal unsafe struct DSHM_IPC_DIAG_PIPE
{
    public byte PipeType;
    public byte EndpointAddress;
    public ushort MaximumPacketSize;
    public byte Interval;
    public fixed byte Reserved[3];
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REQUEST" />. Arrays of structs are exposed as raw
///     byte blocks (netstandard2.0 has no inline arrays) and decoded element-wise by the SDK.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal unsafe struct DSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 Version;

    public UInt32 NtStatus;

    public fixed byte DeviceDescriptor[DiagnosticsLayout.DeviceDescriptorLength];
    public fixed byte Reserved0[2];

    public ushort ConfigDescriptorLength;
    public ushort Reserved1;
    public UInt32 ConfigDescriptorStatus;
    public fixed byte ConfigDescriptor[DiagnosticsLayout.ConfigDescriptorMax];

    public UInt32 PipeCount;

    // 8 x DSHM_IPC_DIAG_PIPE
    public fixed byte Pipes[DiagnosticsLayout.MaxPipes * 8];

    // 3 x DSHM_IPC_DIAG_STRING
    public fixed byte Strings[DiagnosticsLayout.StringCount * (8 + DiagnosticsLayout.StringChars * 2)];

    // 5 x DSHM_IPC_DIAG_REPORT
    public fixed byte Features[DiagnosticsLayout.FeatureCount * (12 + DiagnosticsLayout.ReportLength)];

    // 16 x DSHM_IPC_DIAG_REPORT
    public fixed byte EepromPages[DiagnosticsLayout.EepromPageCount * (12 + DiagnosticsLayout.ReportLength)];

    public UInt32 RestoreStatus;
}
