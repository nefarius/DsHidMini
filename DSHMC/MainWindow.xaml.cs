using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Nefarius.DsHidMini.MVVM;
using Nefarius.DsHidMini.Util;

namespace Nefarius.DsHidMini
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DeviceNotificationListener _listener = new DeviceNotificationListener();

        private readonly DeviceCollectionViewModel _vm = new DeviceCollectionViewModel();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _vm;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));

            _listener.DeviceArrived += ListenerOnDeviceArrived;
            _listener.DeviceRemoved += ListenerOnDeviceRemoved;

            _listener.StartListen(this);
        }

        /// <summary>
        ///     DsHidMini device disconnected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceRemoved(string obj)
        {
            var itemsToRemove = _vm.Devices.Where(
                i => _vm.Devices.All(d => d.InstanceId == Device.GetInstanceIdFromInterfaceId(obj))).ToList();

            foreach (var item in itemsToRemove)
                _vm.Devices.Remove(item);
        }

        /// <summary>
        ///     DsHidMini device connected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceArrived(string obj)
        {
            _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInterfaceId(obj)));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _listener.EndListen();
        }
    }
}