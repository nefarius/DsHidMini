using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Versioned per-slot input-report arrival metrics. Pack=1, 28 bytes; must
///     stay in sync with driver <c>IPC_INPUT_REPORT_METRICS_MESSAGE</c>.
/// </summary>
/// <remarks>
///     <see cref="ReportRateHz" /> and <see cref="AverageIntervalUs" /> cover
///     the driver's last completed ~1 s window. The interval is host-side
///     arrival spacing between successful USB interrupt completions or
///     Bluetooth interrupt packets, not one-way packet latency.
///     A value of <c>0</c> means no sufficient reports were observed.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct DsInputReportMetrics
{
    public const ushort CurrentVersion = 1;

    public const int Size = 28;

    public uint SlotIndex;

    public int SequenceNumber;

    public ushort Version;

    public ushort Reserved0;

    public uint ReportRateHz;

    public uint AverageIntervalUs;

    public ulong TimestampQpc;
}
