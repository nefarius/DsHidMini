using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Nefarius.DsHidMini.MVVM;
using Nefarius.DsHidMini.Util;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace Nefarius.DsHidMini.UI.Devices
{
    /// <summary>
    ///     Interaction logic for DeviceDetailsView.xaml
    /// </summary>
    public partial class DeviceDetailsView : UserControl
    {
        public DeviceDetailsView()
        {
            InitializeComponent();
        }

        private async void ApplyChanges_Click(object sender, RoutedEventArgs e)
        {
            var tb = (Button)e.OriginalSource;
            var vm = (MainViewModel)tb.DataContext;

            await Task.Run(() =>
            {
                try
                {
                    vm.IsRestarting = true;
                    vm.SelectedDevice.ApplyChanges();
                }
                catch
                {
                    Dispatcher.Invoke(() => MessageBox.Show(
                        "Failed to restart device. The settings have been saved, but couldn't be applied. " +
                        "Please manually reconnect the device for the changes to become active.",
                        "Device restart failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    ));
                }
                finally
                {
                    vm.IsRestarting = false;
                }
            });
        }

        private void DeviceDisconnect_OnClick(object sender, RoutedEventArgs e)
        {
            var tb = (Button)e.OriginalSource;
            var vm = (MainViewModel)tb.DataContext;

            var mac = PhysicalAddress.Parse(vm.SelectedDevice.DeviceAddress);

            BluetoothHelper.DisconnectRemoteDevice(mac);
        }
    }
}