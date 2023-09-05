using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Nefarius.DsHidMini.Drivers;
using Nefarius.DsHidMini.MVVM;

namespace Nefarius.DsHidMini.UI.Settings
{
    /// <summary>
    ///     Interaction logic for BthPS3DriverSettingsView.xaml
    /// </summary>
    public partial class BthPS3DriverSettingsView : UserControl
    {
        public BthPS3DriverSettingsView()
        {
            InitializeComponent();
        }

        private void RectifyBthPS3Settings_OnClick(object sender, RoutedEventArgs e)
        {
            BthPS3ProfileDriver.RawPDO = false;
            BthPS3FilterDriver.IsFilterEnabled = true;

            var tb = (Button) e.OriginalSource;
            var vm = (MainViewModel) tb.DataContext;

            vm.RefreshProperties();
        }

        private void DownloadBthPS3_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/nefarius/BthPS3/releases");
        }

        private void OpenHelp_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://docs.nefarius.at/projects/BthPS3/");
        }
    }
}