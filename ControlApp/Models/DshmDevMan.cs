using System.Threading;

using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models;

public class DshmDevMan
{
    private static readonly TimeSpan XusbRefreshDebounce = TimeSpan.FromMilliseconds(250);

    private DeviceNotificationListener? _listener;
    private DeviceNotificationListener? _xusbListener;
    private CancellationTokenSource? _xusbRefreshCts;
    //private readonly HostRadio _hostRadio;

    public List<PnPDevice> Devices { get; } = new();

    public void StartListeningForDshmDevices()
    {
        Log.Logger.Information("Starting detection of DsHidMini devices");
        if (_listener != null)
        {
            return;
        }

        _listener = new DeviceNotificationListener();
        _listener.DeviceArrived += OnListenerDevicesRemovedOrAdded;
        _listener.DeviceRemoved += OnListenerDevicesRemovedOrAdded;
        _listener.StartListen(DsHidMiniDriver.DeviceInterfaceGuid);

        _xusbListener = new DeviceNotificationListener();
        _xusbListener.DeviceArrived += OnXusbInterfaceChanged;
        _xusbListener.DeviceRemoved += OnXusbInterfaceChanged;
        _xusbListener.StartListen(XInputSlotResolver.XusbDeviceInterfaceGuid);

        UpdateConnectedDshmDevicesList();
    }

    public void StopListeningForDshmDevices()
    {
        Log.Logger.Information("Stopping detection of DsHidMini devices");
        Devices.Clear();
        _xusbRefreshCts?.Cancel();
        _xusbRefreshCts?.Dispose();
        _xusbRefreshCts = null;
        _xusbListener?.StopListen();
        _xusbListener?.Dispose();
        _xusbListener = null;
        _listener?.StopListen();
        _listener?.Dispose();
        _listener = null;
    }

    private void OnListenerDevicesRemovedOrAdded(DeviceEventArgs e)
    {
        Log.Logger.Information("DsHidMini devices added or removed. Updating device list");
        UpdateConnectedDshmDevicesList();
    }

    private void OnXusbInterfaceChanged(DeviceEventArgs e)
    {
        QueueXusbInterfaceRefresh();
    }

    /// <summary>
    ///     Coalesces XUSB arrive/remove bursts (common on Bluetooth) and then asks the UI to
    ///     re-resolve player slots without rebuilding the DsHidMini device list.
    /// </summary>
    private void QueueXusbInterfaceRefresh()
    {
        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _xusbRefreshCts, cts);
        previous?.Cancel();
        previous?.Dispose();

        CancellationToken token = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(XusbRefreshDebounce, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            Log.Logger.Debug("XUSB interfaces changed. Refreshing XInput slot resolution.");
            XInputSlotResolver.InvalidateResolutionCache();
            XInputInterfacesUpdated?.Invoke(this, EventArgs.Empty);
        }, token);
    }

    private void UpdateConnectedDshmDevicesList()
    {
        XInputSlotResolver.InvalidateResolutionCache();
        Log.Logger.Debug("Rebuilding list of connected DsHidMini devices");
        Devices.Clear();
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out string? path, out string? instanceId,
                   instance++))
        {
            Log.Logger.Debug(
                "DsHidMini device detected and added to devices list. InstanceID: {InstanceId}", instanceId);
            Devices.Add(PnPDevice.GetDeviceByInstanceId(instanceId));
        }

        Log.Logger.Debug("DsHidMini devices list rebuilt. {DevicesCount} connected devices", Devices.Count);
        ConnectedDeviceListUpdated?.Invoke(this, EventArgs.Empty);
    }

    public static bool TryReconnectDevice(PnPDevice device)
    {
        Log.Logger.Information("Attempting on reconnecting device of instance {DeviceInstanceId}", device.InstanceId);
        string enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName) ?? "USB";
        bool isWireless = !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
        Log.Logger.Debug("Is Device connected wireless: {IsWireless}", isWireless);

        if (isWireless)
        {
            try
            {
                int? slot = DsHidMiniInterop.TryGetIpcSlotIndex(device);
                if (slot is not int deviceIndex)
                {
                    Log.Logger.Warning(
                        "Wireless disconnect skipped for '{InstanceId}': no readable IPC slot.",
                        device.InstanceId);
                    return false;
                }

                if (!DsHidMiniInterop.IsAvailable)
                {
                    Log.Logger.Warning(
                        "Wireless disconnect skipped for '{InstanceId}': driver IPC is not available.",
                        device.InstanceId);
                    return false;
                }

                using DsHidMiniInterop interop = new();
                uint status = interop.DisconnectBluetoothDevice(deviceIndex);
                Log.Logger.Debug(
                    "IPC Bluetooth disconnect for '{InstanceId}' slot {Slot}: 0x{Status:X}",
                    device.InstanceId,
                    deviceIndex,
                    status);
                return PowerOffUsbResult.IsNtSuccess(status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to disconnect wireless device.");
                return false;
            }
        }

        try
        {
            UsbPnPDevice? usbPnPDevice = device.ToUsbPnPDevice();
            Log.Logger.Debug("Power cycling device's USB port");
            usbPnPDevice.CyclePort();
            return true;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Failed to power cycle device's USB port");
            return false;
        }
    }

    public event EventHandler? ConnectedDeviceListUpdated;

    /// <summary>
    ///     Raised after XUSB interfaces appear or disappear so existing devices can retry slot lookup.
    /// </summary>
    public event EventHandler? XInputInterfacesUpdated;
}
