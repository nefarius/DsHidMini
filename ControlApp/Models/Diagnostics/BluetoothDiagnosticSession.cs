using System.Globalization;

using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Orchestrates one end-to-end Bluetooth diagnostic run: preflight, pair, wait for USB removal,
///     wait for a wireless attempt while capturing structured driver ETW events, then classify.
///     Lives as a singleton service (not inside a device view model) so it survives the PnP-driven
///     device view model churn described in the Bluetooth Diagnostic Assistant plan.
/// </summary>
public sealed partial class BluetoothDiagnosticSession : ObservableObject, IAsyncDisposable
{
    /// <summary>
    ///     How long to keep capturing after the USB cable is unplugged before giving up and
    ///     classifying with whatever evidence was observed.
    /// </summary>
    public static readonly TimeSpan WirelessAttemptTimeout = TimeSpan.FromSeconds(25);

    private readonly IDiagnosticBundleWriter _bundleWriter;
    private readonly IDiagnosticClassifier _classifier;
    private readonly DshmDevMan _devMan;
    private readonly IPreflightProbe _preflightProbe;
    private readonly List<DiagnosticEventRecord> _timeline = new();
    private readonly object _timelineLock = new();
    private readonly ITraceCapture _traceCapture;

    private ulong? _candidateAddress;
    private PnPDevice? _candidateDevice;
    private string? _candidateInstanceId;
    private TaskCompletionSource<Exception>? _captureFaultSignal;
    private DateTimeOffset _lastRunFinishedAt;
    private DateTimeOffset _lastRunStartedAt;
    private CancellationTokenSource? _runCts;

    // Volatile: read from the PnP notification callback thread in OnDeviceListUpdated, written
    // from RunAsync. Guarantees that thread observes a fresh (non-cached) reference rather than
    // a value reordered/cached before RunAsync's assignment becomes visible.
    private volatile TaskCompletionSource<bool>? _unplugSignal;

    [ObservableProperty]
    private BluetoothDiagnosticStage _stage = BluetoothDiagnosticStage.Idle;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private DiagnosticVerdict? _verdict;

    public BluetoothDiagnosticSession(
        IPreflightProbe preflightProbe,
        ITraceCapture traceCapture,
        IDiagnosticClassifier classifier,
        IDiagnosticBundleWriter bundleWriter,
        DshmDevMan devMan)
    {
        _preflightProbe = preflightProbe;
        _traceCapture = traceCapture;
        _classifier = classifier;
        _bundleWriter = bundleWriter;
        _devMan = devMan;

        _traceCapture.EventCaptured += OnEventCaptured;
        _traceCapture.CaptureFaulted += OnCaptureFaulted;
        _devMan.ConnectedDeviceListUpdated += OnDeviceListUpdated;
    }

    public IReadOnlyList<PreflightCheckResult> PreflightResults { get; private set; } =
        Array.Empty<PreflightCheckResult>();

    public IReadOnlyList<DiagnosticEventRecord> Timeline
    {
        get
        {
            lock (_timelineLock)
            {
                return _timeline.ToArray();
            }
        }
    }

    /// <summary>
    ///     Raised (on a background thread) every time a new event is added to <see cref="Timeline" />.
    /// </summary>
    public event EventHandler? TimelineUpdated;

    public async ValueTask DisposeAsync()
    {
        _devMan.ConnectedDeviceListUpdated -= OnDeviceListUpdated;
        _traceCapture.EventCaptured -= OnEventCaptured;
        _traceCapture.CaptureFaulted -= OnCaptureFaulted;
        await _traceCapture.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Runs the full sequence once. Safe to call again after completion/cancellation/fault to
    ///     retry from the beginning.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = _runCts.Token;

        lock (_timelineLock)
        {
            _timeline.Clear();
        }

        Verdict = null;
        _lastRunStartedAt = DateTimeOffset.UtcNow;

        Stage = BluetoothDiagnosticStage.RunningPreflight;
        StatusMessage = "Checking your Bluetooth setup...";
        PreflightResults = _preflightProbe.Run();

        if (PreflightResults.Any(r => !r.Passed) || _preflightProbe.FindEligibleUsbController() is not { } device)
        {
            Stage = BluetoothDiagnosticStage.PreflightBlocked;
            Verdict = _classifier.Classify(PreflightResults, Array.Empty<DiagnosticEventRecord>(), _candidateAddress);
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        _candidateDevice = device;
        _candidateInstanceId = device.InstanceId;
        _candidateAddress = TryGetDeviceAddress(device);

        Stage = BluetoothDiagnosticStage.Pairing;
        StatusMessage = "Pairing the controller to this PC...";

        if (!await TryPairAsync(device, token).ConfigureAwait(false))
        {
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        _captureFaultSignal = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await _traceCapture.StartAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to start diagnostic ETW capture.");
            Stage = BluetoothDiagnosticStage.Faulted;
            StatusMessage =
                "Could not start the driver trace. Make sure ControlApp is running as Administrator, then try again.";
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        _unplugSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Stage = BluetoothDiagnosticStage.WaitingForUnplug;
        StatusMessage = "Unplug the USB cable now.";

        // The device may already have been removed while pairing/trace-start was in progress: that
        // removal notification would have arrived while '_unplugSignal' was still null and been
        // dropped by 'OnDeviceListUpdated'. Check the current device list now so an already-missed
        // removal is not waited on forever.
        if (!_devMan.Devices.Any(d => d.InstanceId == _candidateInstanceId))
        {
            _unplugSignal.TrySetResult(true);
        }

        WaitOutcome unplugOutcome = await WaitForStepAsync(_unplugSignal.Task, token).ConfigureAwait(false);
        if (unplugOutcome != WaitOutcome.Completed)
        {
            await _traceCapture.StopAsync().ConfigureAwait(false);
            ApplyIncompleteOutcome(unplugOutcome);
            return;
        }

        Stage = BluetoothDiagnosticStage.WaitingForWirelessAttempt;
        StatusMessage = "Press the PS button on the controller once.";

        WaitOutcome wirelessOutcome = await WaitForStepAsync(
            Task.Delay(WirelessAttemptTimeout, CancellationToken.None), token).ConfigureAwait(false);
        if (wirelessOutcome != WaitOutcome.Completed)
        {
            await _traceCapture.StopAsync().ConfigureAwait(false);
            ApplyIncompleteOutcome(wirelessOutcome);
            return;
        }

        Stage = BluetoothDiagnosticStage.Classifying;
        StatusMessage = "Analyzing what happened...";
        await _traceCapture.StopAsync().ConfigureAwait(false);

        // A fault can land in the narrow window between the wireless-attempt wait naturally
        // completing and this point (e.g. the pump fails right as the timer elapses, and
        // Task.WhenAny happened to pick the timer). Stopping the capture never sets this signal on
        // its own (StopAsync's cancellation is caught as expected inside the pump), so if it is set
        // here the timeline genuinely may be truncated -- report that instead of a clean Completed.
        if (_captureFaultSignal?.Task.IsCompleted == true)
        {
            ApplyIncompleteOutcome(WaitOutcome.CaptureFaulted);
            return;
        }

        Verdict = _classifier.Classify(PreflightResults, Timeline, _candidateAddress);
        Stage = BluetoothDiagnosticStage.Completed;
        StatusMessage = "Done.";
        _lastRunFinishedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Reads the candidate device's Bluetooth address for evidence correlation. Returns
    ///     <see langword="null" /> on any failure (missing property, synthesized/unreadable
    ///     address) so classification simply falls back to its unfiltered behavior.
    /// </summary>
    private static ulong? TryGetDeviceAddress(PnPDevice device)
    {
        try
        {
            string? address = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty);
            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            return ulong.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong parsed)
                ? parsed
                : null;
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Failed to read candidate device Bluetooth address for diagnostic correlation.");
            return null;
        }
    }

    private void ApplyIncompleteOutcome(WaitOutcome outcome)
    {
        if (outcome == WaitOutcome.CaptureFaulted)
        {
            Stage = BluetoothDiagnosticStage.Faulted;
            StatusMessage = "The driver trace stopped unexpectedly. Try again.";
        }
        else
        {
            Stage = BluetoothDiagnosticStage.Cancelled;
        }

        _lastRunFinishedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Requests cooperative cancellation of an in-progress <see cref="RunAsync" /> call.
    /// </summary>
    public void Cancel()
    {
        _runCts?.Cancel();
    }

    /// <summary>
    ///     Attempts to automatically fix the first failed, auto-repairable preflight check from the
    ///     most recent <see cref="RunAsync" /> call (currently only BthPS3 RawPDO/PSM settings).
    ///     Requires administrator privileges. Callers should call <see cref="RunAsync" /> again
    ///     afterward to re-verify.
    /// </summary>
    public bool TryAutoRepairBlockedCheck()
    {
        PreflightCheckResult? blocked = PreflightResults.FirstOrDefault(r => !r.Passed && r.CanAutoRepair);
        return blocked is not null && _preflightProbe.TryAutoRepair(blocked.Id);
    }

    /// <summary>
    ///     Writes the most recent run to a support bundle ZIP. See
    ///     <see cref="IDiagnosticBundleWriter.WriteAsync" /> for redaction behavior.
    /// </summary>
    public Task ExportBundleAsync(
        string destinationZipPath,
        bool redact = true,
        CancellationToken cancellationToken = default)
    {
        DiagnosticBundleContent content = new(
            Verdict,
            PreflightResults,
            Timeline,
            typeof(BluetoothDiagnosticSession).Assembly.GetName().Version?.ToString() ?? "unknown",
            _candidateDevice is not null ? DsHidMiniDriverCompatibility.FormatInstalledVersion(_candidateDevice) : null,
            _preflightProbe.BthPS3VersionDisplay,
            _lastRunStartedAt,
            _lastRunFinishedAt);

        return _bundleWriter.WriteAsync(content, destinationZipPath, redact, cancellationToken);
    }

    private async Task<bool> TryPairAsync(PnPDevice device, CancellationToken token)
    {
        try
        {
            int? slot = DsHidMiniInterop.TryGetIpcSlotIndex(device);
            if (slot is not int deviceIndex || !DsHidMiniInterop.IsAvailable)
            {
                Stage = BluetoothDiagnosticStage.PairingFailed;
                Verdict = BuildPairingFailureVerdict("Driver communication is not available for this controller.");
                return false;
            }

            SetHostResult result = await Task.Run(() =>
            {
                using DsHidMiniInterop interop = new();
                return interop.PairToCurrentHost(deviceIndex);
            }, token).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                Stage = BluetoothDiagnosticStage.PairingFailed;
                Verdict = BuildPairingFailureVerdict(result.ToString());
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            Stage = BluetoothDiagnosticStage.Cancelled;
            return false;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Bluetooth diagnostic pairing step failed.");
            Stage = BluetoothDiagnosticStage.PairingFailed;
            Verdict = BuildPairingFailureVerdict(ex.Message);
            return false;
        }
    }

    private enum WaitOutcome
    {
        Completed,
        Cancelled,
        CaptureFaulted
    }

    /// <summary>
    ///     Awaits <paramref name="work" />, racing it against cooperative cancellation and an
    ///     unexpected trace-capture fault so a dead ETW pump never leaves the wizard stuck waiting
    ///     for a signal that will never arrive.
    /// </summary>
    private async Task<WaitOutcome> WaitForStepAsync(Task work, CancellationToken token)
    {
        Task cancelTask = Task.Delay(Timeout.InfiniteTimeSpan, token);
        Task faultTask = _captureFaultSignal?.Task ?? Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);

        Task completed = await Task.WhenAny(work, cancelTask, faultTask).ConfigureAwait(false);
        if (completed == faultTask)
        {
            return WaitOutcome.CaptureFaulted;
        }

        if (completed == cancelTask)
        {
            return WaitOutcome.Cancelled;
        }

        // Propagate a real failure from 'work' itself (not cancellation/fault).
        await work.ConfigureAwait(false);
        return WaitOutcome.Completed;
    }

    private static DiagnosticVerdict BuildPairingFailureVerdict(string detail)
    {
        return new DiagnosticVerdict(
            DiagnosticVerdictCode.PairingWriteFailed,
            DiagnosticConfidence.High,
            "Preflight checks passed",
            $"The pairing information could not be written to (or read back from) the controller: {detail}",
            "Reconnect the controller with USB and try again.",
            Array.Empty<DiagnosticEventRecord>());
    }

    private void OnDeviceListUpdated(object? sender, EventArgs e)
    {
        // Snapshot both into locals: this runs on the PnP notification thread while RunAsync (on a
        // different thread) may concurrently reassign '_candidateInstanceId'/'_unplugSignal' for a
        // fresh run, so re-reading the fields between the null-checks and the TrySetResult call
        // below could otherwise observe a signal from a different run than the one just checked.
        string? candidateInstanceId = _candidateInstanceId;
        TaskCompletionSource<bool>? unplugSignal = _unplugSignal;
        if (candidateInstanceId is null || unplugSignal is null)
        {
            return;
        }

        bool stillPresent = _devMan.Devices.Any(d => d.InstanceId == candidateInstanceId);
        if (!stillPresent)
        {
            unplugSignal.TrySetResult(true);
        }
    }

    private void OnCaptureFaulted(Exception ex)
    {
        Log.Logger.Warning(ex, "Diagnostic ETW capture faulted during an active run.");
        _captureFaultSignal?.TrySetResult(ex);
    }

    private void OnEventCaptured(DiagnosticEventRecord record)
    {
        lock (_timelineLock)
        {
            _timeline.Add(record);
        }

        TimelineUpdated?.Invoke(this, EventArgs.Empty);
    }
}
