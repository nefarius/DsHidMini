namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Result of the console-style USB power-off sequence (issue #366).
/// </summary>
public readonly struct PowerOffUsbResult
{
    /// <summary>
    ///     NTSTATUS of the 48-byte zero output report that turns LEDs and rumble off.
    /// </summary>
    public UInt32 IndicatorsOffStatus { get; init; }

    /// <summary>
    ///     NTSTATUS of the Feature 0xF4 disable transfer that stops input reports.
    /// </summary>
    public UInt32 ShutdownStatus { get; init; }

    /// <summary>
    ///     <see langword="true" /> when both transfers completed successfully.
    /// </summary>
    public bool Succeeded => IsNtSuccess(IndicatorsOffStatus) && IsNtSuccess(ShutdownStatus);

    /// <summary>
    ///     Interprets a raw NTSTATUS value the same way the driver does (<c>NT_SUCCESS</c>).
    /// </summary>
    public static bool IsNtSuccess(UInt32 status)
    {
        return unchecked((Int32)status) >= 0;
    }

    public override string ToString()
    {
        return $"Indicators-off: 0x{IndicatorsOffStatus:X}, shutdown: 0x{ShutdownStatus:X}";
    }
}
