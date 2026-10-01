using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     One timed step of the guided capture (for example "hold still").
/// </summary>
public sealed record ControllerCapturePhase(string Name, string Instruction, TimeSpan Duration);

/// <summary>
///     Per-phase evidence derived from the live motion stream. Distinct-value counts help tell a live
///     sensor from a frozen/constant one without needing the raw numbers.
/// </summary>
public sealed class ControllerPhaseResult
{
    public required string Name { get; init; }

    public double StartMs { get; init; }

    public double EndMs { get; init; }

    public int SampleCount { get; init; }

    /// <summary>
    ///     Samples per second over the phase, based on unique driver sample indices.
    /// </summary>
    public double SamplesPerSecond { get; init; }

    public int DistinctRawAccelX { get; init; }

    public int DistinctRawAccelY { get; init; }

    public int DistinctRawAccelZ { get; init; }

    public int DistinctRawGyro { get; init; }
}

/// <summary>
///     One once-per-second input-report rate observation from the driver.
/// </summary>
public sealed record ControllerReportRateSample(double OffsetMs, uint ReportRateHz, uint AverageIntervalUs);

/// <summary>
///     Result of the guided live capture.
/// </summary>
public sealed class ControllerTelemetryCapture
{
    /// <summary>
    ///     <see langword="true" /> when the driver exposes motion telemetry and at least one sample was received.
    /// </summary>
    public bool MotionAvailable { get; init; }

    /// <summary>
    ///     Why parts of the capture are missing, or <see langword="null" /> when complete.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    ///     UTF-8 motion CSV (same format as the Motion viewer recorder); empty when no samples were received.
    /// </summary>
    public byte[] MotionCsv { get; init; } = Array.Empty<byte>();

    public IReadOnlyList<ControllerPhaseResult> Phases { get; init; } = Array.Empty<ControllerPhaseResult>();

    public IReadOnlyList<ControllerReportRateSample> ReportRates { get; init; } =
        Array.Empty<ControllerReportRateSample>();
}

/// <summary>
///     Everything the controller diagnostic export is built from. Every optional part may be missing so a
///     partial export (for example a Bluetooth pad, an old driver, or a cancelled capture) still works.
/// </summary>
public sealed class ControllerDiagnosticContent
{
    public required string ControlAppVersion { get; init; }

    public string? DsHidMiniDriverVersion { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public required string DeviceType { get; init; }

    public required string ConnectionType { get; init; }

    public string? DeviceAddress { get; init; }

    public bool IsDeviceAddressSynthesized { get; init; }

    public int? SlotIndex { get; init; }

    /// <summary>
    ///     Cached Feature 0x01 identification blob, if available.
    /// </summary>
    public byte[]? IdentificationBlob { get; init; }

    /// <summary>
    ///     ControlApp's heuristic wording at export time (never a verdict).
    /// </summary>
    public string? AuthenticityLabel { get; init; }

    /// <summary>
    ///     Raw driver sweep result, or <see langword="null" /> when unavailable.
    /// </summary>
    public ControllerDiagnosticsResult? DriverSweep { get; init; }

    /// <summary>
    ///     Human-readable reason when <see cref="DriverSweep" /> is missing or has failed items.
    /// </summary>
    public string? DriverSweepNote { get; init; }

    public ControllerTelemetryCapture? Telemetry { get; init; }
}
