using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using AdonisUI.Controls;
using Nefarius.DsHidMini.Drivers;
using Nefarius.DsHidMini.MVVM;
using Nefarius.DsHidMini.Util;

namespace Nefarius.DsHidMini
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
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
            _vm.Devices.Clear();

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));
        }

        /// <summary>
        ///     DsHidMini device connected.
        /// </summary>
        /// <param name="obj">The device path.</param>
        private void ListenerOnDeviceArrived(string obj)
        {
            _vm.Devices.Clear();

            var instance = 0;
            while (Devcon.Find(DsHidMiniDriver.DeviceInterfaceGuid, out var path, out var instanceId, instance++))
                _vm.Devices.Add(new DeviceViewModel(Device.GetDeviceByInstanceId(instanceId)));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _listener.EndListen();
        }

        private void ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            _vm.SelectedDevice.ApplyChanges();
        }

        private void Help_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vigem.org/projects/DsHidMini/");
        }
    }
}