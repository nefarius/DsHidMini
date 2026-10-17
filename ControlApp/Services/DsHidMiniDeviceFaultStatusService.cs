using System.Windows.Interop;

using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Watches for DsHidMini devnodes that Windows failed to start, including devices that never
///     expose the interface the normal device list enumerates.
/// </summary>
public partial class DsHidMiniDeviceFaultStatusService : ObservableObject, IDisposable
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDevNodesChanged = 0x0007;
    private static readonly TimeSpan RescanDebounce = TimeSpan.FromMilliseconds(250);

    private readonly object _rescanGate = new();
    private DeviceNotificationListener? _usbListener;
    private HwndSource? _source;
    private CancellationTokenSource? _debounceCts;
    private int _scanGeneration;
    private bool _listening;

    [ObservableProperty]
    private bool _hasFault;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Error;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    [ObservableProperty]
    private int _deviceCount;

    public void Dispose()
    {
        StopListening();
    }

    public void StartListening(Window window)
    {
        if (_listening)
        {
            return;
        }

        _listening = true;
        Log.Logger.Information("Starting detection of DsHidMini devices that failed to start");

        AttachDevNodeChanges(window);

        _usbListener = new DeviceNotificationListener();
        _usbListener.DeviceArrived += OnUsbDeviceArrivedOrRemoved;
        _usbListener.DeviceRemoved += OnUsbDeviceArrivedOrRemoved;
        _usbListener.StartListen(DeviceInterfaceIds.UsbDevice);

        QueueRescan();
    }

    public void StopListening()
    {
        if (!_listening && _usbListener is null && _source is null)
        {
            return;
        }

        Log.Logger.Information("Stopping detection of DsHidMini devices that failed to start");
        _listening = false;

        if (_source is not null)
        {
            _source.RemoveHook(OnWindowMessage);
            _source = null;
        }

        _usbListener?.StopListen();
        _usbListener?.Dispose();
        _usbListener = null;

        lock (_rescanGate)
        {
            _scanGeneration++;
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }

    private void AttachDevNodeChanges(Window window)
    {
        if (PresentationSource.FromVisual(window) is not HwndSource source)
        {
            Log.Logger.Warning("DsHidMini fault monitor could not attach to the main window.");
            return;
        }

        _source = source;
        _source.AddHook(OnWindowMessage);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmDeviceChange && wParam == (IntPtr)DbtDevNodesChanged)
        {
            QueueRescan();
        }

        return IntPtr.Zero;
    }

    private void OnUsbDeviceArrivedOrRemoved(DeviceEventArgs e)
    {
        QueueRescan();
    }

    private void QueueRescan()
    {
        CancellationToken token;
        int generation;

        lock (_rescanGate)
        {
            CancellationTokenSource cts = new();
            CancellationTokenSource? previous = _debounceCts;
            _debounceCts = cts;
            previous?.Cancel();
            previous?.Dispose();
            generation = ++_scanGeneration;
            token = cts.Token;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(RescanDebounce, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
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
        IReadOnlyList<DsHidMiniDevNodeSnapshot> nodes;
        try
        {
            nodes = DsHidMiniDeviceFaultDetector.EnumeratePresent();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to scan for DsHidMini devices that could not start.");
            return;
        }

        if (generation != Volatile.Read(ref _scanGeneration))
        {
            return;
        }

        DsHidMiniDeviceFaultNotice? notice =
            DsHidMiniDeviceFaultPolicy.Describe(DsHidMiniDeviceFaultPolicy.SelectFaulted(nodes));
        Application.Current?.Dispatcher.BeginInvoke(() => ApplyScanResult(generation, notice));
    }

    private void ApplyScanResult(int generation, DsHidMiniDeviceFaultNotice? notice)
    {
        if (generation != _scanGeneration)
        {
            return;
        }

        HasFault = notice is not null;
        DeviceCount = notice?.DeviceCount ?? 0;
        Severity = InfoBarSeverity.Error;
        StatusTitle = notice?.Title ?? string.Empty;
        StatusMessage = notice?.Message ?? string.Empty;

        if (notice is not null)
        {
            Log.Logger.Warning("DsHidMini device start failure: {Title} {Message}", notice.Title, notice.Message);
        }
    }
}
