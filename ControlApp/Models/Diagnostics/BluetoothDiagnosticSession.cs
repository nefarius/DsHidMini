using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.IPC;
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

    private PnPDevice? _candidateDevice;
    private string? _candidateInstanceId;
    private DateTimeOffset _lastRunFinishedAt;
    private DateTimeOffset _lastRunStartedAt;
    private CancellationTokenSource? _runCts;
    private TaskCompletionSource<bool>? _unplugSignal;

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
            Verdict = _classifier.Classify(PreflightResults, Array.Empty<DiagnosticEventRecord>());
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        _candidateDevice = device;
        _candidateInstanceId = device.InstanceId;

        Stage = BluetoothDiagnosticStage.Pairing;
        StatusMessage = "Pairing the controller to this PC...";

        if (!await TryPairAsync(device, token).ConfigureAwait(false))
        {
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

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

        if (!await WaitOrCancelAsync(_unplugSignal.Task, token).ConfigureAwait(false))
        {
            await _traceCapture.StopAsync().ConfigureAwait(false);
            Stage = BluetoothDiagnosticStage.Cancelled;
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        Stage = BluetoothDiagnosticStage.WaitingForWirelessAttempt;
        StatusMessage = "Press the PS button on the controller once.";

        if (!await WaitOrCancelAsync(Task.Delay(WirelessAttemptTimeout, CancellationToken.None), token)
                .ConfigureAwait(false))
        {
            await _traceCapture.StopAsync().ConfigureAwait(false);
            Stage = BluetoothDiagnosticStage.Cancelled;
            _lastRunFinishedAt = DateTimeOffset.UtcNow;
            return;
        }

        Stage = BluetoothDiagnosticStage.Classifying;
        StatusMessage = "Analyzing what happened...";
        await _traceCapture.StopAsync().ConfigureAwait(false);

        Verdict = _classifier.Classify(PreflightResults, Timeline);
        Stage = BluetoothDiagnosticStage.Completed;
        StatusMessage = "Done.";
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

    /// <summary>
    ///     Awaits <paramref name="work" />, returning <see langword="false" /> (instead of throwing)
    ///     when <paramref name="token" /> is cancelled first.
    /// </summary>
    private static async Task<bool> WaitOrCancelAsync(Task work, CancellationToken token)
    {
        Task cancelTask = Task.Delay(Timeout.InfiniteTimeSpan, token);
        Task completed = await Task.WhenAny(work, cancelTask).ConfigureAwait(false);
        if (completed == cancelTask)
        {
            return false;
        }

        // Propagate a real failure from 'work' itself (not cancellation).
        await work.ConfigureAwait(false);
        return true;
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
        if (_candidateInstanceId is null || _unplugSignal is null)
        {
            return;
        }

        bool stillPresent = _devMan.Devices.Any(d => d.InstanceId == _candidateInstanceId);
        if (!stillPresent)
        {
            _unplugSignal.TrySetResult(true);
        }
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
