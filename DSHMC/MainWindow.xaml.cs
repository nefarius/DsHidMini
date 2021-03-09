using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using AdonisUI.Controls;
using Nefarius.DsHidMini.Drivers;
using Nefarius.DsHidMini.MVVM;
using Nefarius.DsHidMini.Util;
using Nefarius.DsHidMini.Util.Web;

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
            Validator.IsGenuineAddress(PhysicalAddress.Parse("AC7A4D2819AC"));

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

        private async void ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    _vm.SelectedDevice.ApplyChanges();
                }
                catch
                {
                    // TODO: handle better
                }
            });
        }

        private void Help_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vigem.org/projects/DsHidMini/");
        }

        private void DownloadBthPS3_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/ViGEm/BthPS3/releases");
        }

        private void OpenHelp_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://vigem.org/projects/BthPS3/");
        }

        private void RectifyBthPS3Settings_OnClick(object sender, RoutedEventArgs e)
        {
            BthPS3ProfileDriver.RawPDO = false;
            BthPS3FilterDriver.IsFilterEnabled = true;

            _vm.RefreshProperties();
        }

        private void ReleasesHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start((((Hyperlink)sender).NavigateUri).ToString());
        }
    }
}