using System;
using System.ComponentModel;
using System.Threading;
using Nefarius.DsHidMini.Util;

namespace Nefarius.DsHidMini.MVVM
{
    public class DeviceViewModel : INotifyPropertyChanged
    {
        private readonly Device _device;

        private readonly Timer _batteryQuery;

        public DeviceViewModel(Device device)
        {
            _device = device;

            _batteryQuery = new Timer(UpdateBatteryStatus, null, 10000, 10000);
        }

        private void UpdateBatteryStatus(object state)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryStatus"));
        }

        /// <summary>
        ///     Apply changes by requesting device restart.
        /// </summary>
        public void ApplyChanges()
        {
            _device.Restart();
        }

        /// <summary>
        ///     Current HID device emulation mode.
        /// </summary>
        public DsHidDeviceMode HidEmulationMode
        {
            get =>
                (DsHidDeviceMode) _device.GetProperty<byte>(
                    DsHidMiniDriver.HidDeviceModeProperty);
            set => _device.SetProperty(DsHidMiniDriver.HidDeviceModeProperty, (byte) value);
        }

        /// <summary>
        ///     The device Instance ID.
        /// </summary>
        public string InstanceId => _device.InstanceId;

        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public string DeviceAddress
        {
            get
            {
                var deviceAddress = _device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();

                var friendlyAddress = deviceAddress;

                var insertedCount = 0;
                for (var i = 2; i < deviceAddress.Length; i = i + 2)
                    friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");

                return friendlyAddress;
            }
        }

        /// <summary>
        ///     The Bluetooth MAC address of the host radio this device is currently paired to.
        /// </summary>
        public string HostAddress
        {
            get
            {
                var hostAddress = _device.GetProperty<ulong>(DsHidMiniDriver.HostAddressProperty).ToString("X12")
                    .ToUpper();

                var friendlyAddress = hostAddress;

                var insertedCount = 0;
                for (var i = 2; i < hostAddress.Length; i = i + 2)
                    friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");

                return friendlyAddress;
            }
        }

        /// <summary>
        ///     Current battery status.
        /// </summary>
        public DsBatteryStatus BatteryStatus =>
            (DsBatteryStatus) _device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty);

        /// <summary>
        ///     The friendly (product) name of this device.
        /// </summary>
        public string DisplayName => _device.GetProperty<string>(DevicePropertyDevice.FriendlyName);

        /// <summary>
        ///     The connection protocol used by this device.
        /// </summary>
        public string ConnectionType
        {
            get
            {
                var enumerator = _device.GetProperty<string>(DevicePropertyDevice.EnumeratorName);

                return enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase)
                    ? "USB"
                    : "Bluetooth";
            }
        }

        /// <summary>
        ///     Last time this device has been seen connected (applies to Bluetooth connected devices only).
        /// </summary>
        public DateTimeOffset LastConnected =>
            _device.GetProperty<DateTimeOffset>(DsHidMiniDriver.BluetoothLastConnectedTimeProperty);

        public event PropertyChangedEventHandler PropertyChanged;
    }
}