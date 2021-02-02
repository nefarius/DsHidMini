using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DSHMC.Annotations;

namespace DSHMC
{
    internal class DsHidMiniDeviceCollectionViewModel : INotifyPropertyChanged
    {
        public DsHidMiniDeviceCollectionViewModel()
        {
            Devices = new ObservableCollection<DsHidMiniDeviceViewModel>();
        }

        public ObservableCollection<DsHidMiniDeviceViewModel> Devices { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}