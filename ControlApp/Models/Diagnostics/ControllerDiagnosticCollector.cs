using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Snapshot of the device card state the export is built from.
/// </summary>
public sealed record ControllerExportRequest(
    string DeviceTitle,
    string DeviceType,
    string ConnectionType,
    bool IsWireless,
    int? SlotIndex,
    string? DeviceAddress,
    bool IsDeviceAddressSynthesized,
    string? DriverVersion,
    byte[]? IdentificationBlob,
    string? AuthenticityLabel);

/// <summary>
///     Outcome of the driver sweep step; <see cref="Result" /> may be missing, <see cref="Note" /> then says why.
/// </summary>
public sealed record ControllerSweepOutcome(ControllerDiagnosticsResult? Result, string? Note);

/// <summary>
///     Orchestrates the driver sweep and bundle assembly with graceful fallbacks, so the UI only has to
///     show progress and a save dialog.
/// </summary>
public static class ControllerDiagnosticCollector
{
    private const uint StatusNotImplemented = 0xC0000002;
    private const uint StatusNotSupported = 0xC00000BB;

    /// <summary>
    ///     Runs the driver sweep via <paramref name="sweep" /> (normally
    ///     <see cref="DsHidMiniInterop.CollectControllerDiagnostics" />) and never throws.
    /// </summary>
    public static Task<ControllerSweepOutcome> CollectSweepAsync(
        ControllerExportRequest request,
        bool ipcAvailable,
        Func<int, ControllerDiagnosticsResult> sweep)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sweep);

        if (request.IsWireless)
        {
            return Task.FromResult(new ControllerSweepOutcome(null,
                "The raw USB probe needs a wired controller; this Bluetooth controller was exported with cached " +
                "identification data and live telemetry only. Connect it via USB for a complete export."));
        }

        if (request.SlotIndex is not int slot)
        {
            return Task.FromResult(new ControllerSweepOutcome(null,
                "The driver has not published an IPC slot for this controller (the driver may be outdated)."));
        }

        if (!ipcAvailable)
        {
            return Task.FromResult(new ControllerSweepOutcome(null, "Driver IPC is not available."));
        }

        return Task.Run(() =>
        {
            try
            {
                ControllerDiagnosticsResult result = sweep(slot);
                return Interpret(result);
            }
            catch (Exception ex)
            {
                return new ControllerSweepOutcome(null, $"The driver sweep failed: {ex.Message}");
            }
        });
    }

    internal static ControllerSweepOutcome Interpret(ControllerDiagnosticsResult result)
    {
        if (result.Succeeded)
        {
            List<string> failures = new();
            failures.AddRange(result.Features
                .Where(f => !PowerOffUsbResult.IsNtSuccess(f.GetStatus))
                .Select(f => $"Feature 0x{f.Id:X2}: 0x{f.GetStatus:X8}"));

            int failedPages = result.EepromPages.Count(p =>
                !PowerOffUsbResult.IsNtSuccess(p.SetStatus) || !PowerOffUsbResult.IsNtSuccess(p.GetStatus));
            if (failedPages > 0)
            {
                failures.Add($"{failedPages} of {result.EepromPages.Count} EEPROM pages unreadable");
            }

            if (!PowerOffUsbResult.IsNtSuccess(result.RestoreStatus))
            {
                failures.Add($"re-selecting EEPROM page 0xA0 failed (0x{result.RestoreStatus:X8}); " +
                             "replug the controller before using motion features");
            }

            return new ControllerSweepOutcome(result,
                failures.Count == 0 ? null : "Some items could not be read: " + string.Join("; ", failures) + ".");
        }

        return result.Status switch
        {
            StatusNotImplemented => new ControllerSweepOutcome(null,
                "The installed driver is too old for the full USB probe; update DsHidMini for a complete export."),
            StatusNotSupported => new ControllerSweepOutcome(null,
                "The driver does not support the USB probe for this controller type."),
            _ => new ControllerSweepOutcome(null, $"The driver refused the sweep (0x{result.Status:X8}).")
        };
    }

    /// <summary>
    ///     Builds the bundle content from the request and the collected parts.
    /// </summary>
    public static ControllerDiagnosticContent BuildContent(
        ControllerExportRequest request,
        string controlAppVersion,
        ControllerSweepOutcome sweep,
        ControllerTelemetryCapture? telemetry)
    {
        return new ControllerDiagnosticContent
        {
            ControlAppVersion = controlAppVersion,
            DsHidMiniDriverVersion = request.DriverVersion,
            DeviceType = request.DeviceType,
            ConnectionType = request.ConnectionType,
            DeviceAddress = request.DeviceAddress,
            IsDeviceAddressSynthesized = request.IsDeviceAddressSynthesized,
            SlotIndex = request.SlotIndex,
            IdentificationBlob = request.IdentificationBlob,
            AuthenticityLabel = request.AuthenticityLabel,
            DriverSweep = sweep.Result,
            DriverSweepNote = sweep.Note,
            Telemetry = telemetry
        };
    }
}
