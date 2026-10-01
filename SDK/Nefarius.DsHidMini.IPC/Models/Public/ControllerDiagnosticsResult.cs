namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     One raw feature report or EEPROM page read during the driver diagnostics sweep.
/// </summary>
public sealed class DiagnosticsReport
{
    /// <summary>
    ///     Feature report ID, or the EEPROM page number for 0xEF page reads.
    /// </summary>
    public byte Id { get; init; }

    /// <summary>
    ///     NTSTATUS of the EEPROM page-select transfer (zero for plain feature reads).
    /// </summary>
    public UInt32 SetStatus { get; init; }

    /// <summary>
    ///     NTSTATUS of the GET_REPORT transfer.
    /// </summary>
    public UInt32 GetStatus { get; init; }

    /// <summary>
    ///     Bytes returned by the device; empty when the read failed or was skipped.
    /// </summary>
    public byte[] Data { get; init; } = Array.Empty<byte>();
}

/// <summary>
///     One USB string descriptor read during the driver diagnostics sweep.
/// </summary>
public sealed class DiagnosticsString
{
    /// <summary>
    ///     String descriptor index from the device descriptor (0 = not provided).
    /// </summary>
    public byte Index { get; init; }

    /// <summary>
    ///     NTSTATUS of the string query.
    /// </summary>
    public UInt32 Status { get; init; }

    /// <summary>
    ///     The string text, or <see langword="null" /> when unavailable.
    /// </summary>
    public string? Text { get; init; }
}

/// <summary>
///     One configured USB pipe/endpoint.
/// </summary>
public sealed class DiagnosticsPipe
{
    /// <summary>
    ///     WDF_USB_PIPE_TYPE value.
    /// </summary>
    public byte PipeType { get; init; }

    /// <summary>
    ///     USB endpoint address (bit 7 set = IN).
    /// </summary>
    public byte EndpointAddress { get; init; }

    /// <summary>
    ///     Maximum packet size in bytes.
    /// </summary>
    public ushort MaximumPacketSize { get; init; }

    /// <summary>
    ///     Polling interval.
    /// </summary>
    public byte Interval { get; init; }
}

/// <summary>
///     Raw result of the driver-side USB diagnostics sweep of a wired DS3. Every item carries its own
///     NTSTATUS so a partial sweep remains useful.
/// </summary>
public sealed class ControllerDiagnosticsResult
{
    /// <summary>
    ///     Layout version reported by the driver.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    ///     Overall NTSTATUS; per-item statuses are only meaningful when this is success.
    /// </summary>
    public UInt32 Status { get; init; }

    /// <summary>
    ///     18-byte USB device descriptor.
    /// </summary>
    public byte[] DeviceDescriptor { get; init; } = Array.Empty<byte>();

    /// <summary>
    ///     Full USB configuration descriptor (configuration, interface, HID and endpoint descriptors).
    /// </summary>
    public byte[] ConfigDescriptor { get; init; } = Array.Empty<byte>();

    /// <summary>
    ///     NTSTATUS of the configuration descriptor retrieval.
    /// </summary>
    public UInt32 ConfigDescriptorStatus { get; init; }

    /// <summary>
    ///     Configured endpoints.
    /// </summary>
    public IReadOnlyList<DiagnosticsPipe> Pipes { get; init; } = Array.Empty<DiagnosticsPipe>();

    /// <summary>
    ///     Manufacturer, product and serial number strings, in that order.
    /// </summary>
    public IReadOnlyList<DiagnosticsString> Strings { get; init; } = Array.Empty<DiagnosticsString>();

    /// <summary>
    ///     Feature reports 0x01, 0xF2, 0xF5, 0xF7 and 0xF8, in that order.
    /// </summary>
    public IReadOnlyList<DiagnosticsReport> Features { get; init; } = Array.Empty<DiagnosticsReport>();

    /// <summary>
    ///     EEPROM pages 0x00, 0x10, ... 0xF0 read through Feature 0xEF.
    /// </summary>
    public IReadOnlyList<DiagnosticsReport> EepromPages { get; init; } = Array.Empty<DiagnosticsReport>();

    /// <summary>
    ///     NTSTATUS of re-selecting the motion calibration page (0xA0) after the sweep.
    /// </summary>
    public UInt32 RestoreStatus { get; init; }

    /// <summary>
    ///     <see langword="true" /> when the driver executed the sweep.
    /// </summary>
    public bool Succeeded => PowerOffUsbResult.IsNtSuccess(Status);
}
