using System;
using System.ComponentModel;
using System.Threading;
using FontAwesome5;
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

        public bool MuteDigitalPressureButtons
        {
            get => _device.GetProperty<byte>(DsHidMiniDriver.MuteDigitalPressureButtonsProperty) > 0;
            set
            {
                using (var evt = EventWaitHandle.OpenExisting(
                    $"Global\\DsHidMiniConfigHotReloadEvent{DeviceAddress}"
                    ))
                {
                    _device.SetProperty(DsHidMiniDriver.MuteDigitalPressureButtonsProperty, (byte)(value ? 1 : 0));

                    evt.Set();
                }
            }
        }

        /// <summary>
        ///     Current HID device emulation mode.
        /// </summary>
        public DsHidDeviceMode HidEmulationMode
        {
            get =>
                (DsHidDeviceMode)_device.GetProperty<byte>(
                    DsHidMiniDriver.HidDeviceModeProperty);
            set => _device.SetProperty(DsHidMiniDriver.HidDeviceModeProperty, (byte)value);
        }

        public uint OutputReportTimerPeriodMs
        {
            get => _device.GetProperty<uint>(DsHidMiniDriver.OutputReportTimerPeriodMsProperty);
            set => _device.SetProperty(DsHidMiniDriver.OutputReportTimerPeriodMsProperty, value);
        }

        /// <summary>
        ///     The device Instance ID.
        /// </summary>
        public string InstanceId => _device.InstanceId;

        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public string DeviceAddress => _device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();

        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public string DeviceAddressFriendly
        {
            get
            {
                var friendlyAddress = DeviceAddress;

                var insertedCount = 0;
                for (var i = 2; i < DeviceAddress.Length; i = i + 2)
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
            (DsBatteryStatus)_device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty);

        /// <summary>
        ///     Return a battery icon depending on the charge.
        /// </summary>
        public EFontAwesomeIcon BatteryIcon
        {
            get
            {
                switch (BatteryStatus)
                {
                    case DsBatteryStatus.Charged:
                    case DsBatteryStatus.Charging:
                    case DsBatteryStatus.Full:
                        return EFontAwesomeIcon.Solid_BatteryFull;
                    case DsBatteryStatus.High:
                        return EFontAwesomeIcon.Solid_BatteryThreeQuarters;
                    case DsBatteryStatus.Medium:
                        return EFontAwesomeIcon.Solid_BatteryHalf;
                    case DsBatteryStatus.Low:
                        return EFontAwesomeIcon.Solid_BatteryQuarter;
                    case DsBatteryStatus.Dying:
                        return EFontAwesomeIcon.Solid_BatteryEmpty;
                    default:
                        return EFontAwesomeIcon.Solid_BatteryEmpty;
                }
            }
        }

        /// <summary>
        ///     The friendly (product) name of this device.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var name = _device.GetProperty<string>(DevicePropertyDevice.FriendlyName);

                return string.IsNullOrEmpty(name) ? "<failed to retrieve device name>" : name;
            }
        }

        /// <summary>
        ///     The connection protocol used by this device.
        /// </summary>
        public EFontAwesomeIcon ConnectionType
        {
            get
            {
                var enumerator = _device.GetProperty<string>(DevicePropertyDevice.EnumeratorName);

                return enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase)
                    ? EFontAwesomeIcon.Brands_Usb
                    : EFontAwesomeIcon.Brands_Bluetooth;
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