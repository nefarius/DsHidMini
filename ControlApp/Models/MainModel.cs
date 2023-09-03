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
    class MainModel
    {
        private readonly DeviceNotificationListener _listener = new DeviceNotificationListener();
        private readonly DshmConfigManager.DshmConfigManager _dshmConfigManager = new DshmConfigManager.DshmConfigManager();
        private readonly HostRadio _hostRadio = new HostRadio();

        public DshmConfigManager.DshmConfigManager DshmConfigManag { get; }

        /// <summary>
        ///     Helper to check if run with elevated privileges.
        /// </summary>
        public bool IsElevated => SecurityUtil.IsElevated;

        public List<PnPDevice> Devices { get; private set; }

        public MainModel()
        {
            _listener.DeviceArrived += UpdateConnectedDevicesList;
            _listener.DeviceRemoved += UpdateConnectedDevicesList;
        }

        public void UpdateConnectedDevicesList(DeviceEventArgs e)
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
                if (IsElevated)
                {
                    ((UsbPnPDevice)device).CyclePort();
                }
            }
        }

        public event EventHandler ConnectedDeviceListUpdated;

    }
}
