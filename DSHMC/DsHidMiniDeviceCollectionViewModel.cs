using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DSHMC
{
    public class DsHidMiniDeviceCollectionViewModel : INotifyPropertyChanged
    {
        public DsHidMiniDeviceCollectionViewModel()
        {
            Devices = new ObservableCollection<DsHidMiniDeviceViewModel>();
        }

        public ObservableCollection<DsHidMiniDeviceViewModel> Devices { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}