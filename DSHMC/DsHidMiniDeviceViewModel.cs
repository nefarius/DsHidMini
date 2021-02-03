using System.ComponentModel;

namespace DSHMC
{
    public class DsHidMiniDeviceViewModel : INotifyPropertyChanged
    {
        public DsHidMiniDeviceViewModel(string name, string type)
        {
            DisplayName = name;
            ConnectionType = type;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayName { get; }

        public string ConnectionType { get; }
    }
}
