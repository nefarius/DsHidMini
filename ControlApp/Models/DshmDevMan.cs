using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models;

public class DshmDevMan
{
    private DeviceNotificationListener? _listener;
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

        UpdateConnectedDshmDevicesList();
    }

    public void StopListeningForDshmDevices()
    {
        Log.Logger.Information("Stopping detection of DsHidMini devices");
        Devices.Clear();
        _listener?.StopListen();
        _listener?.Dispose();
        _listener = null;
    }

    private void OnListenerDevicesRemovedOrAdded(DeviceEventArgs e)
    {
        Log.Logger.Information("DsHidMini devices added or removed. Updating device list");
        UpdateConnectedDshmDevicesList();
    }

    private void UpdateConnectedDshmDevicesList()
    {
        Log.Logger.Debug("Rebuilding list of connected DsHidMini devices");
        Devices.Clear();
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out string? path, out string? instanceId,
                   instance++))
        {
            Log.Logger.Debug($"DsHidMini device detected and added to devices list. InstanceID: {instanceId}");
            Devices.Add(PnPDevice.GetDeviceByInstanceId(instanceId));
        }

        Log.Logger.Debug($"DsHidMini devices list rebuilt. {Devices.Count} connected devices");
        ConnectedDeviceListUpdated?.Invoke(this, new EventArgs());
    }

    public static bool TryReconnectDevice(PnPDevice device)
    {
        Log.Logger.Information($"Attempting on reconnecting device of instance {device.InstanceId}");
        string? enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);
        bool isWireless = !enumerator!.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
        Log.Logger.Debug($"Is Device connected wireless: {isWireless}");

        if (isWireless)
        {
            try
            {
                HostRadio hostRadio = new();
                string deviceAddress = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty)!.ToUpper();
                Log.Logger.Debug($"Instructing BT host on disconnecting device of MAC {deviceAddress}");
                hostRadio.DisconnectRemoteDevice(deviceAddress);
                return true;
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
}