using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Principal;

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

        /// <summary>
        ///     Helper to check if run with elevated privileges.
        /// </summary>
        public bool IsElevated
        {
            get
            {
                var securityIdentifier = WindowsIdentity.GetCurrent().Owner;
                return !(securityIdentifier is null) && securityIdentifier
                    .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}