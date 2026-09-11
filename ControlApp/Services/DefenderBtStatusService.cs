using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Watches for a Retro Fighters Defender Bluetooth Edition controller enumerated in its default
///     DualShock 4 USB identity and offers a one-click way to switch it into its DualShock 3 identity, which
///     DsHidMini can bind to. See issue #282 and <c>docs/PS3_USB_STARTUP.md</c>.
/// </summary>
public partial class DefenderBtStatusService : ObservableObject, IDisposable
{
    /// <summary>
    ///     How long to wait after the last HID arrival/removal notification before actually rescanning, so a
    ///     burst of notifications (e.g. whole-bus re-enumeration) coalesces into a single scan.
    /// </summary>
    private static readonly TimeSpan RescanDebounce = TimeSpan.FromMilliseconds(250);

    /// <summary>
    ///     How long a late probe is given to make the DualShock 4 identity disappear before we treat it as ignored.
    /// </summary>
    private static readonly TimeSpan LateProbeWait = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    ///     How long to wait after a USB port cycle for either DualShock 3 appearance or a DualShock 4 re-arrival
    ///     that we can probe again.
    /// </summary>
    private static readonly TimeSpan ReenumerateWait = TimeSpan.FromSeconds(4);

    private DeviceNotificationListener? _listener;

    /// <summary>
    ///     HID device path (symbolic link) of the currently detected Defender BT in DualShock 4 mode, if any.
    /// </summary>
    private string? _detectedDevicePath;

    /// <summary>
    ///     When set, the next DualShock 4 arrival is probed immediately (no debounce), matching the PS3's
    ///     post-SET_IDLE timing.
    /// </summary>
    private int _pendingImmediateSwitch;

    /// <summary>
    ///     Cancellation source for the pending debounced rescan, if any. Re-created (cancelling the previous
    ///     one) every time a new notification arrives so only the latest one actually runs.
    /// </summary>
    private CancellationTokenSource? _debounceCts;

    /// <summary>
    ///     Incremented for every queued scan; used to discard results from a stale/out-of-order scan that
    ///     completed after a newer one.
    /// </summary>
    private int _scanGeneration;

    [ObservableProperty]
    private bool _isDetected;

    [ObservableProperty]
    private bool _isSwitching;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    /// <summary>
    ///     True if a switch attempt can currently be made.
    /// </summary>
    public bool CanSwitch => IsDetected && !IsSwitching;

    public void Dispose()
    {
        StopListening();
    }

    /// <summary>
    ///     Starts watching for HID device arrivals/removals and performs an initial scan.
    /// </summary>
    public void StartListening()
    {
        if (_listener != null)
        {
            return;
        }

        Log.Logger.Information("Starting detection of Defender BT (DualShock 4 mode) devices");

        _listener = new DeviceNotificationListener();
        _listener.DeviceArrived += OnListenerDevicesArrivedOrRemoved;
        _listener.DeviceRemoved += OnListenerDevicesArrivedOrRemoved;
        _listener.StartListen(DefenderBtModeSwitcher.HidDeviceInterfaceGuid);

        QueueRescan();
    }

    /// <summary>
    ///     Stops watching for HID device arrivals/removals.
    /// </summary>
    public void StopListening()
    {
        Log.Logger.Information("Stopping detection of Defender BT (DualShock 4 mode) devices");
        _listener?.StopListen();
        _listener?.Dispose();
        _listener = null;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    /// <summary>
    ///     Sends the PS3 mode-switch probe and, if the controller ignores a late probe, cycles the USB port so
    ///     the next arrival can be probed immediately. Intended to be called from the UI thread.
    /// </summary>
    public async Task<DefenderBtModeSwitchResult> SwitchToPs3ModeAsync()
    {
        if (_detectedDevicePath is null)
        {
            return DefenderBtModeSwitchResult.NotADefenderBt;
        }

        IsSwitching = true;
        OnPropertyChanged(nameof(CanSwitch));

        try
        {
            string devicePath = _detectedDevicePath;
            DefenderBtModeSwitchResult sent = DefenderBtModeSwitcher.TrySwitchToPs3Mode(devicePath);
            Log.Logger.Information(
                "Defender BT PS3 mode-switch probe for {DevicePath} resulted in {Result}",
                devicePath, sent);

            if (sent != DefenderBtModeSwitchResult.Sent)
            {
                return sent;
            }

            if (await WaitForSwitchAsync(LateProbeWait).ConfigureAwait(true))
            {
                return DefenderBtModeSwitchResult.Switched;
            }

            Volatile.Write(ref _pendingImmediateSwitch, 1);
            if (!DefenderBtModeSwitcher.TryCycleUsbPort(devicePath))
            {
                Log.Logger.Warning(
                    "Defender BT stayed in DualShock 4 mode after a late probe; USB port cycle failed");
                return DefenderBtModeSwitchResult.NeedsReconnect;
            }

            if (await WaitForSwitchAsync(ReenumerateWait).ConfigureAwait(true))
            {
                return DefenderBtModeSwitchResult.Switched;
            }

            Volatile.Write(ref _pendingImmediateSwitch, 0);
            return DefenderBtModeSwitchResult.IgnoredByHardware;
        }
        finally
        {
            IsSwitching = false;
            OnPropertyChanged(nameof(CanSwitch));
        }
    }

    private void OnListenerDevicesArrivedOrRemoved(DeviceEventArgs e)
    {
        if (Volatile.Read(ref _pendingImmediateSwitch) != 0 ||
            ApplicationConfiguration.Instance.AutoSwitchDefenderBtToPs3Mode)
        {
            TrySendImmediateProbe();
        }

        QueueRescan();
    }

    /// <summary>
    ///     Sends the probe as soon as a DualShock 4 identity is visible, without waiting for the UI debounce.
    ///     The PS3 issued Feature 0x14 a few milliseconds after SET_IDLE; a 250 ms delay is already late.
    /// </summary>
    private void TrySendImmediateProbe()
    {
        _ = Task.Run(() =>
        {
            try
            {
                string? path = FindDefenderBtCandidatePath();
                if (path is null)
                {
                    return;
                }

                DefenderBtModeSwitchResult result = DefenderBtModeSwitcher.TrySwitchToPs3Mode(path);
                Log.Logger.Information(
                    "Immediate Defender BT PS3 mode-switch probe for {DevicePath} resulted in {Result}",
                    path, result);

                // One-shot: a failed button click arms this so the next plugin is probed
                // even when auto-switch is off. Do not clear on a removal-only event.
                Volatile.Write(ref _pendingImmediateSwitch, 0);

                if (result == DefenderBtModeSwitchResult.Sent)
                {
                    Thread.Sleep(50);
                    if (FindDefenderBtCandidatePath() is { } stillThere)
                    {
                        DefenderBtModeSwitcher.TrySwitchToPs3Mode(stillThere);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Immediate Defender BT PS3 mode-switch probe failed");
            }
        });
    }

    /// <summary>
    ///     Coalesces bursts of HID arrival/removal notifications into a single debounced rescan. The actual HID
    ///     enumeration and <c>CreateFile</c>/<c>HidD_GetAttributes</c> work happens on a thread-pool thread;
    ///     only the resulting observable state update is marshaled back onto the WPF dispatcher.
    /// </summary>
    private void QueueRescan()
    {
        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _debounceCts, cts);
        previous?.Cancel();
        previous?.Dispose();

        int generation = Interlocked.Increment(ref _scanGeneration);
        CancellationToken token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(RescanDebounce, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            RunScan(generation);
        }, token);
    }

    /// <summary>
    ///     Runs on a thread-pool thread: enumerates HID devices and opens/queries each candidate. Immediate
    ///     probes happen in <see cref="TrySendImmediateProbe" /> so this scan only updates UI state.
    /// </summary>
    private void RunScan(int generation)
    {
        string? foundPath = FindDefenderBtCandidatePath();
        Application.Current?.Dispatcher.BeginInvoke(() => ApplyScanResult(generation, foundPath));
    }

    private static string? FindDefenderBtCandidatePath()
    {
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(
                   DefenderBtModeSwitcher.HidDeviceInterfaceGuid, out string? path, out string? _, instance++))
        {
            if (path is not null && DefenderBtModeSwitcher.IsDefenderBtInDs4Mode(path))
            {
                return path;
            }
        }

        return null;
    }

    private async Task<bool> WaitForSwitchAsync(TimeSpan timeout)
    {
        TimeSpan poll = TimeSpan.FromMilliseconds(100);
        TimeSpan elapsed = TimeSpan.Zero;

        while (elapsed < timeout)
        {
            await Task.Delay(poll).ConfigureAwait(true);
            elapsed += poll;

            bool ds4Gone = FindDefenderBtCandidatePath() is null;
            if (!ds4Gone)
            {
                continue;
            }

            if (DefenderBtModeSwitcher.IsDualShock3UsbPresent())
            {
                Volatile.Write(ref _pendingImmediateSwitch, 0);
                return true;
            }

            // Port cycle drops 05C4 briefly before it reappears. Keep waiting unless a DS3 showed up.
        }

        return FindDefenderBtCandidatePath() is null && DefenderBtModeSwitcher.IsDualShock3UsbPresent();
    }

    /// <summary>
    ///     Runs on the UI thread. Applies the result of a background scan to observable state, unless a newer
    ///     scan has since been queued or completed.
    /// </summary>
    private void ApplyScanResult(int generation, string? foundPath)
    {
        if (generation != _scanGeneration)
        {
            return;
        }

        _detectedDevicePath = foundPath;
        IsDetected = foundPath is not null;

        if (IsDetected)
        {
            StatusTitle = "Retro Fighters Defender BT detected in DualShock 4 mode";
            StatusMessage =
                "Switch it to PS3 (DualShock 3) mode so DsHidMini can bind to it and Bluetooth pairing becomes available. If a late switch is ignored, the USB port is reset and the probe is retried immediately after re-enumeration.";
            Severity = InfoBarSeverity.Informational;
        }
        else
        {
            StatusTitle = string.Empty;
            StatusMessage = string.Empty;
        }

        OnPropertyChanged(nameof(CanSwitch));
    }
}
