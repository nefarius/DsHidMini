using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.IPC.Models;

/// <summary>
///     Represents the most current raw DS3 HID input report.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal struct IPC_HID_INPUT_REPORT_MESSAGE
{
    /// <summary>
    ///     The one-based device index this report belongs to.
    /// </summary>
    public UInt32 SlotIndex;

    /// <summary>
    ///     The <see cref="DS3_RAW_INPUT_REPORT" /> coming directly from the device with no transformations applied.
    /// </summary>
    public DS3_RAW_INPUT_REPORT InputReport;
}