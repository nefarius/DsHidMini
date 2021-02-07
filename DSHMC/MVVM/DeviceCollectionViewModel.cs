using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Nefarius.DsHidMini.MVVM
{
    public class DeviceCollectionViewModel : INotifyPropertyChanged
    {
        public DeviceCollectionViewModel()
        {
            Devices = new ObservableCollection<DeviceViewModel>();
        }

        public ObservableCollection<DeviceViewModel> Devices { get; set; }

        public DeviceViewModel SelectedDevice { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}