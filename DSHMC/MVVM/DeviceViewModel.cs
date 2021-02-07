using System;
using System.ComponentModel;
using Nefarius.DsHidMini.Util;
using Device = Nefarius.DsHidMini.Util.Device;

namespace Nefarius.DsHidMini.MVVM
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly string _deviceAddress;

        public DeviceViewModel(Device device)
        {
            InterfaceId = device.InterfaceId;

            var enumerator = device.GetProperty<string>(DevicePropertyDevice.EnumeratorName);

            ConnectionType = enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase)
                ? "USB"
                : "Bluetooth";

            DisplayName = device.GetProperty<string>(DevicePropertyDevice.FriendlyName);

            _deviceAddress = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty);

            DeviceAddress = _deviceAddress.ToUpper();
            var insertedCount = 0;
            for (var i = 2; i < _deviceAddress.Length; i = i + 2)
                DeviceAddress = DeviceAddress.Insert(i + insertedCount++, ":");

            var manufacturer = device.GetProperty<string>(DevicePropertyDevice.Manufacturer);

            var battery =
                (DsHidMiniDriver.DsBatteryStatus) device.GetProperty<byte>(
                    DsHidMiniDriver.BatteryStatusProperty);

            var mode =
                (DsHidMiniDriver.DsHidDeviceMode) device.GetProperty<byte>(
                    DsHidMiniDriver.HidDeviceModeProperty);

            var lastConnected =
                device.GetProperty<DateTimeOffset>(DsHidMiniDriver.BluetoothLastConnectedTimeProperty);
        }

        public string InterfaceId { get; }

        public string DeviceAddress { get; }

        public string DisplayName { get; }

        public string ConnectionType { get; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}