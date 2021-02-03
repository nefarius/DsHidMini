using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using AdonisUI.Controls;
using Nefarius.Devcon;

namespace DSHMC
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        private readonly DeviceNotificationListener _listener = new DeviceNotificationListener();

        private readonly DsHidMiniDeviceCollectionViewModel _vm = new DsHidMiniDeviceCollectionViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _vm;

            _vm.Devices.Add(new DsHidMiniDeviceViewModel("PLAYSTATION(R)3 Controller", "Bluetooth"));
            _vm.Devices.Add(new DsHidMiniDeviceViewModel("PLAYSTATION(R)3 Controller", "USB"));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _listener.DeviceArrived+= ListenerOnDeviceArrived;
            _listener.DeviceRemoved+=ListenerOnDeviceRemoved;

            _listener.StartListen(this);
        }

        /// <summary>
        ///     DsHidMini device disconnected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceRemoved(string obj)
        {
            
        }

        /// <summary>
        ///     DsHidMini device connected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceArrived(string obj)
        {
            using (var device = Device.GetDeviceByInterfaceId(obj))
            {
                var enumerator = device.GetProperty<string>(DevicePropertyDevice.EnumeratorName);

                var friendlyName = device.GetProperty<string>(DevicePropertyDevice.FriendlyName);

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
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _listener.EndListen();
        }
    }
}