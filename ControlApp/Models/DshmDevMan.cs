using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.Extensions;

namespace Nefarius.DsHidMini.ControlApp.Models
{
    public class DshmDevMan
    {
        private DeviceNotificationListener _listener;
        //private readonly HostRadio _hostRadio;

        public List<PnPDevice> Devices { get; private set; } = new();

        public DshmDevMan()
        {
        }

        public bool StartListeningForDshmDevices()
        {
            Log.Logger.Information("Starting detection of DsHidMini devices");
            if (_listener != null) return false;
            _listener = new DeviceNotificationListener();
            _listener.DeviceArrived += OnListenerDevicesRemovedOrAdded;
            _listener.DeviceRemoved += OnListenerDevicesRemovedOrAdded;
            _listener.StartListen(DsHidMiniDriver.DeviceInterfaceGuid);

            UpdateConnectedDshmDevicesList();
            return true;
        }

        public void StopListeningForDshmDevices()
        {
            Log.Logger.Information("Stopping detection of DsHidMini devices");
            Devices.Clear();
            _listener.StopListen();
            _listener.Dispose();
            _listener = null;
        }

        public void OnListenerDevicesRemovedOrAdded(DeviceEventArgs e)
        {
            Log.Logger.Information("DsHidMini devices added or removed. Updating device list");
            UpdateConnectedDshmDevicesList();
        }

        public void UpdateConnectedDshmDevicesList()
        {
            Log.Logger.Debug("Rebuilding list of connected DsHidMini devices");
            Devices.Clear();
            var instance = 0;
            while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
            {
                Log.Logger.Debug($"DsHidMini device detected and added to devices list. InstanceID: {instanceId}");
                Devices.Add(PnPDevice.GetDeviceByInstanceId(instanceId));
            }
            Log.Logger.Debug($"DsHidMini devices list rebuilt. {Devices.Count} connected devices");
            ConnectedDeviceListUpdated?.Invoke(this, new());
        }
        

        public bool TryReconnectDevice(PnPDevice device)
        {
            Log.Logger.Information($"Attempting on reconnecting device of instance {device.InstanceId}");
            var enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);
            var IsWireless = !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
            Log.Logger.Debug($"Is Device connected wirelessly: {IsWireless}");

            if (IsWireless)
            {
                try
                {
                    HostRadio hostRadio = new();
                    var deviceAddress = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();
                    Log.Logger.Debug($"Instructing BT host on disconnecting device of MAC {deviceAddress}");
                    hostRadio.DisconnectRemoteDevice(deviceAddress);
                    return true;
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Failed to disconnect wireless device.");
                    return false;
                }
            }
            else
            {
                try
                {
                    var ohmy = device.ToUsbPnPDevice();
                    Log.Logger.Debug($"Power cycling device's USB port");
                    ohmy.CyclePort();
                    return true;
                }
                catch (Exception e)
                {
                    Log.Logger.Error(e,$"Failed to power cycle device's USB port");
                    return false;
                }
            }
        }

        public event EventHandler ConnectedDeviceListUpdated;

    }
}
