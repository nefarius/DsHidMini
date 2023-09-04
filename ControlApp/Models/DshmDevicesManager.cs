using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.Utilities.DeviceManagement.PnP;
using Nefarius.Utilities.Bluetooth;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels;

namespace Nefarius.DsHidMini.ControlApp.Models
{
    public class DshmDevicesManager
    {
        private DeviceNotificationListener _listener;
        private readonly HostRadio _hostRadio = new HostRadio();

        public List<PnPDevice> Devices { get; private set; } = new();

        public DshmDevicesManager()
        {
            UpdateConnectedDshmDevicesList();
        }

        public bool StartListeningForDshmDevices()
        {
            if (_listener != null) return false;
            _listener = new DeviceNotificationListener();
            _listener.DeviceArrived += OnListenerDevicesRemovedOrAdded;
            _listener.DeviceRemoved += OnListenerDevicesRemovedOrAdded;
            _listener.StartListen(DsHidMiniDriver.DeviceInterfaceGuid);
            return true;
        }

        public void StopListeningForDshmDevices()
        {
            _listener.StopListen();
            _listener.Dispose();
            _listener = null;
        }

        public void OnListenerDevicesRemovedOrAdded(DeviceEventArgs e)
        {
            UpdateConnectedDshmDevicesList();
        }

        public void UpdateConnectedDshmDevicesList()
        {
            Devices.Clear();
            var instance = 0;
            while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
            {
                Devices.Add(PnPDevice.GetDeviceByInstanceId(instanceId));
            }
            ConnectedDeviceListUpdated?.Invoke(this, new());
        }
        
        public void DisconnectDevice(PnPDevice device)
        {
            var enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);
            var IsWireless = !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);

            if (IsWireless)
            {
                var deviceAddress = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();
                _hostRadio.DisconnectRemoteDevice(deviceAddress);
            }
            else
            {
                ((UsbPnPDevice)device).CyclePort();
            }
        }

        public event EventHandler ConnectedDeviceListUpdated;

    }
}
