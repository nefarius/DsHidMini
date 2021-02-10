using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Security.Principal;

namespace Nefarius.DsHidMini.MVVM
{
    public class DeviceCollectionViewModel : INotifyPropertyChanged
    {
        public DeviceCollectionViewModel()
        {
            Devices = new ObservableCollection<DeviceViewModel>();

            Devices.CollectionChanged += (sender, args) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasNoDevices"));
            };
        }

        public ObservableCollection<DeviceViewModel> Devices { get; set; }

        public DeviceViewModel SelectedDevice { get; set; }

        /// <summary>
        ///     Is a device currently selected.
        /// </summary>
        public bool HasDeviceSelected => SelectedDevice != null;

        public bool HasNoDevices => Devices.Count == 0;

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

        /// <summary>
        ///     Is it possible to edit the selected device.
        /// </summary>
        public bool IsEditable => IsElevated && HasDeviceSelected;

        public string Version => $"Version: {Assembly.GetEntryAssembly().GetName().Version}";

        public event PropertyChangedEventHandler PropertyChanged;
    }
}