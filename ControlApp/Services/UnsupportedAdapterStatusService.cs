using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Watches for USB adapters DsHidMini recognizes but does not bind, and
///     surfaces a Devices-page warning. See <c>docs/TWIN_USB_ADAPTER.md</c>.
/// </summary>
public partial class UnsupportedAdapterStatusService : ObservableObject, IDisposable
{
    /// <summary>
    ///     How long to wait after the last USB arrival/removal notification before
    ///     actually rescanning, so a burst of notifications coalesces into one scan.
    /// </summary>
    private static readonly TimeSpan RescanDebounce = TimeSpan.FromMilliseconds(250);

    private DeviceNotificationListener? _listener;
    private CancellationTokenSource? _debounceCts;
    private int _scanGeneration;

    [ObservableProperty]
    private bool _isDetected;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Warning;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    public void Dispose()
    {
        StopListening();
    }

    public void StartListening()
    {
        if (_listener != null)
        {
            return;
        }

        Log.Logger.Information("Starting detection of known-unsupported USB adapters");

        _listener = new DeviceNotificationListener();
        _listener.DeviceArrived += OnListenerDevicesArrivedOrRemoved;
        _listener.DeviceRemoved += OnListenerDevicesArrivedOrRemoved;
        _listener.StartListen(DeviceInterfaceIds.UsbDevice);

        QueueRescan();
    }

    public void StopListening()
    {
        Log.Logger.Information("Stopping detection of known-unsupported USB adapters");
        _listener?.StopListen();
        _listener?.Dispose();
        _listener = null;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;
    }

    private void OnListenerDevicesArrivedOrRemoved(DeviceEventArgs e)
    {
        QueueRescan();
    }

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

    private void RunScan(int generation)
    {
        UnsupportedAdapter? adapter = UnsupportedAdapterDetector.FindFirstPresent();
        Application.Current?.Dispatcher.BeginInvoke(() => ApplyScanResult(generation, adapter));
    }

    private void ApplyScanResult(int generation, UnsupportedAdapter? adapter)
    {
        if (generation != _scanGeneration)
        {
            return;
        }

        IsDetected = adapter is not null;

        if (adapter is not null)
        {
            StatusTitle = adapter.StatusTitle;
            StatusMessage = adapter.StatusMessage;
            Severity = InfoBarSeverity.Warning;
        }
        else
        {
            StatusTitle = string.Empty;
            StatusMessage = string.Empty;
        }
    }
}
