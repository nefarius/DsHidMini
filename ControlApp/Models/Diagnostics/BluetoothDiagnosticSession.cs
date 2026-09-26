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
    ///     How long to keep capturing after the USB cable is unplugged when neither a wireless
    ///     reconnect nor a conclusive ETW success has been observed yet.
    /// </summary>
    public static readonly TimeSpan WirelessAttemptTimeout = TimeSpan.FromSeconds(25);

    /// <summary>
    ///     Observation window used by <see cref="RunAsync" />. Tests shorten this so the
    ///     capture/classify path can be exercised without waiting the full production timeout.
    /// </summary>
    internal TimeSpan WirelessAttemptWait { get; set; } = WirelessAttemptTimeout;

    /// <summary>
    ///     Test seam that replaces live IPC pairing. When set, a null USB candidate is also allowed
    ///     so the capture/classify path can run without hardware.
    /// </summary>
    internal Func<CancellationToken, Task<bool>>? TryPairOverride { get; set; }

    /// <summary>
    ///     When <see langword="true" /> (production default), a run that is blocked only by a
    ///     missing USB controller waits for a device-list change instead of finishing immediately.
    ///     Tests that expect a snapshot preflight set this to <see langword="false" />.
    /// </summary>
    internal bool WaitForUsbWhenMissing { get; set; } = true;

    /// <summary>
    ///     Test seam for a wireless reconnect of the candidate controller. Production uses the live
    ///     DsHidMini device list (Bluetooth enumerator + matching address).
    /// </summary>
    internal Func<bool>? WirelessReconnectObservedOverride { get; set; }

    private readonly IDiagnosticBundleWriter _bundleWriter;
    private readonly IDiagnosticClassifier _classifier;
    private readonly DshmDevMan _devMan;
    private readonly IPreflightProbe _preflightProbe;
    private readonly BluetoothConnectionClassifier _successProbe = new();
    private readonly List<DiagnosticEventRecord> _timeline = new();
    private readonly object _timelineLock = new();
    private readonly ITraceCapture _traceCapture;

    private ulong? _candidateAddress;
    private PnPDevice? _candidateDevice;
    private string? _candidateInstanceId;
    private TaskCompletionSource<Exception>? _captureFaultSignal;
    private DateTimeOffset _lastRunFinishedAt;
    private DateTimeOffset _lastRunStartedAt;
    private volatile bool _observedWirelessReconnect;
    private CancellationTokenSource? _runCts;

    // Volatile: read from the PnP notification callback thread in OnDeviceListUpdated, written
    // from RunAsync. Guarantees that thread observes a fresh (non-cached) reference rather than
    // a value reordered/cached before RunAsync's assignment becomes visible.
    private volatile TaskCompletionSource<bool>? _unplugSignal;
    private volatile TaskCompletionSource<bool>? _usbArrivalSignal;
    private volatile TaskCompletionSource<bool>? _wirelessAttemptCompleteSignal;

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

    private IReadOnlyList<PreflightCheckResult> _preflightResults = Array.Empty<PreflightCheckResult>();

    public IReadOnlyList<PreflightCheckResult> PreflightResults
    {
        get => _preflightResults;
        private set => SetProperty(ref _preflightResults, value);
    }

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
        _usbArrivalSignal = null;
        _unplugSignal = null;
        _wirelessAttemptCompleteSignal = null;
        _observedWirelessReconnect = false;
        _lastRunStartedAt = DateTimeOffset.UtcNow;
        _devMan.RefreshConnectedDevices();

        Stage = BluetoothDiagnosticStage.RunningPreflight;
        StatusMessage = "Checking your Bluetooth setup...";
        PreflightResults = _preflightProbe.Run();

        PnPDevice? device = _preflightProbe.FindEligibleUsbController();
        if (HasNonUsbPreflightFailure(PreflightResults) ||
            (device is null && TryPairOverride is null && !WaitForUsbWhenMissing))
        {
            CompleteAsPreflightBlocked();
            return;
        }

        if (device is null && TryPairOverride is null)
        {
            WaitOutcome usbOutcome = await WaitForUsbControllerAsync(token).ConfigureAwait(false);
            if (usbOutcome != WaitOutcome.Completed)
            {
                ApplyIncompleteOutcome(usbOutcome);
                return;
            }

            device = _preflightProbe.FindEligibleUsbController();
            if (HasNonUsbPreflightFailure(PreflightResults) || device is null)
            {
                CompleteAsPreflightBlocked();
                return;
            }
        }

        _candidateDevice = device;
        _candidateInstanceId = device?.InstanceId;
        _candidateAddress = device is not null ? TryGetDeviceAddress(device) : null;

        Stage = BluetoothDiagnosticStage.Pairing;
        StatusMessage = "Pairing the controller to this PC...";

        bool paired = TryPairOverride is { } pairingOverride
            ? await pairingOverride(token).ConfigureAwait(false)
            : await TryPairAsync(device!, token).ConfigureAwait(false);
        if (!paired)
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
            StatusMessage = SecurityUtil.IsElevated
                ? $"Could not start the driver trace: {ex.Message}"
                : "Could not start the driver trace. Restart ControlApp as Administrator, then try again.";
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

        WaitOutcome wirelessOutcome = await WaitForWirelessAttemptAsync(token).ConfigureAwait(false);
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
        if (Verdict.Code != DiagnosticVerdictCode.Success &&
            (_observedWirelessReconnect || IsCandidateWirelessReconnectObserved()))
        {
            // Older BthPS3 builds may never emit RemoteConnectReceived or even RemoteDeviceOnline,
            // so the ETW-only classifier stays inconclusive even though the same controller is
            // already back over Bluetooth. The live device list is enough to finish first-run.
            Verdict = new DiagnosticVerdict(
                DiagnosticVerdictCode.Success,
                DiagnosticConfidence.High,
                "The controller connected over Bluetooth",
                "The controller reappeared over Bluetooth after pairing. This BthPS3 version did not " +
                "report a full driver-trace sequence, but the device is connected.",
                "No action needed. The controller should now behave normally over Bluetooth.",
                Timeline);
        }

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

    private async Task<WaitOutcome> WaitForUsbControllerAsync(CancellationToken token)
    {
        Stage = BluetoothDiagnosticStage.WaitingForUsb;
        StatusMessage = "Connect the controller to this PC with a USB cable.";

        while (_preflightProbe.FindEligibleUsbController() is null && TryPairOverride is null)
        {
            if (HasNonUsbPreflightFailure(PreflightResults))
            {
                return WaitOutcome.Completed;
            }

            _usbArrivalSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            WaitOutcome outcome = await WaitForStepAsync(_usbArrivalSignal.Task, token).ConfigureAwait(false);
            if (outcome != WaitOutcome.Completed)
            {
                return outcome;
            }

            _devMan.RefreshConnectedDevices();
            PreflightResults = _preflightProbe.Run();
        }

        return WaitOutcome.Completed;
    }

    /// <summary>
    ///     Ends the wireless observation window as soon as the candidate is back over Bluetooth or
    ///     the ETW timeline already classifies as success. Does not require newer BthPS3 events
    ///     such as <c>RemoteConnectReceived</c>. Falls back to <see cref="WirelessAttemptWait" />
    ///     only when neither signal appears.
    /// </summary>
    private async Task<WaitOutcome> WaitForWirelessAttemptAsync(CancellationToken token)
    {
        _wirelessAttemptCompleteSignal =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        TryCompleteWirelessAttempt();

        using CancellationTokenSource windowCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        windowCts.CancelAfter(WirelessAttemptWait);

        Task windowTask = Task.Delay(Timeout.InfiniteTimeSpan, windowCts.Token);
        WaitOutcome outcome = await WaitForStepAsync(
            Task.WhenAny(_wirelessAttemptCompleteSignal.Task, windowTask), token).ConfigureAwait(false);

        _wirelessAttemptCompleteSignal = null;
        return outcome;
    }

    private void TryCompleteWirelessAttempt()
    {
        TaskCompletionSource<bool>? signal = _wirelessAttemptCompleteSignal;
        if (signal is null)
        {
            return;
        }

        if (IsCandidateWirelessReconnectObserved())
        {
            _observedWirelessReconnect = true;
            signal.TrySetResult(true);
            return;
        }

        if (TimelineShowsConclusiveSuccess())
        {
            signal.TrySetResult(true);
        }
    }

    private bool TimelineShowsConclusiveSuccess()
    {
        DiagnosticVerdict verdict = _successProbe.Classify(PreflightResults, Timeline, _candidateAddress);
        return verdict.Code == DiagnosticVerdictCode.Success;
    }

    private bool IsCandidateWirelessReconnectObserved()
    {
        if (WirelessReconnectObservedOverride is { } observedOverride)
        {
            return observedOverride();
        }

        if (_candidateAddress is null)
        {
            return false;
        }

        foreach (PnPDevice device in _devMan.Devices)
        {
            if (string.Equals(device.InstanceId, _candidateInstanceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsWirelessDevice(device))
            {
                continue;
            }

            if (TryGetDeviceAddress(device) == _candidateAddress)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWirelessDevice(PnPDevice device)
    {
        try
        {
            string enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName) ?? "USB";
            return !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void CompleteAsPreflightBlocked()
    {
        Stage = BluetoothDiagnosticStage.PreflightBlocked;
        Verdict = _classifier.Classify(PreflightResults, Array.Empty<DiagnosticEventRecord>(), _candidateAddress);
        StatusMessage = PreflightResults.FirstOrDefault(result => !result.Passed)?.Detail
                        ?? "Fix the items below, then click Start again.";
        _lastRunFinishedAt = DateTimeOffset.UtcNow;
    }

    private static bool HasNonUsbPreflightFailure(IReadOnlyList<PreflightCheckResult> results) =>
        results.Any(result => !result.Passed && result.Id != PreflightCheckId.UsbControllerPresent);

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
        _usbArrivalSignal?.TrySetResult(true);
        TryCompleteWirelessAttempt();

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
        TryCompleteWirelessAttempt();
    }
}
