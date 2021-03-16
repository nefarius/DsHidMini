using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Nefarius.DsHidMini.MVVM;

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
            var tb = (Button) e.OriginalSource;
            var vm = (MainViewModel) tb.DataContext;

            await Task.Run(() =>
            {
                try
                {
                    vm.SelectedDevice.ApplyChanges();
                }
                catch
                {
                    // TODO: handle better
                }
            });
        }
    }
}