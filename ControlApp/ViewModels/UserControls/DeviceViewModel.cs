using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Enums;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        // ------------------------------------------------------ FIELDS

        private readonly DshmConfigManager _dshmConfigManager;
        private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
        private readonly PnPDevice _device;
        private readonly Timer _batteryQuery;
        private DeviceData deviceUserData;

        /// <summary>
        /// Settings View Model for device's custom settings
        /// Editing allowed, changes saved only if applying settings with custom settings mode selected
        /// </summary>
        [ObservableProperty] private SettingsEditorViewModel _deviceCustomsVM = new() { AllowEditing = true };

        /// <summary>
        /// Setting View Model for profile device is linked to
        /// Editing not allowed
        /// </summary>
        [ObservableProperty] private SettingsEditorViewModel _profileCustomsVM = new();

        /// <summary>
        /// Settings View Model for current global profile
        /// Editing not allowed
        /// </summary>
        [ObservableProperty] private SettingsEditorViewModel _globalCustomsVM = new();

        /// <summary>
        /// Current selected settings, accordingly to device's settings mode
        /// </summary>
        [ObservableProperty] private SettingsEditorViewModel _selectedGroupsVM = new();


        [ObservableProperty] private bool _isEditorEnabled;

        /// <summary>
        /// Determines if the profile selector is visible.
        /// True if in Profile settings mode, false otherwise
        /// </summary>
        [ObservableProperty] private bool _isProfileSelectorVisible;

        /// <summary>
        /// Desired settings mode for current device. Saved to device data only if applying settings
        /// </summary>
        [ObservableProperty] private SettingsModes _currentDeviceSettingsMode;

        /// <summary>
        ///     Current HID device emulation mode.
        /// </summary>
        public DsHidDeviceMode HidEmulationMode => (DsHidDeviceMode)_device.GetProperty<byte>(DsHidMiniDriver.HidDeviceModeProperty);

        /// <summary>
        /// Summary of device's current HID mode and Settings mode
        /// </summary>
        public string DeviceSettingsStatus => $"{(HidModeShort)_device.GetProperty<byte>(DsHidMiniDriver.HidDeviceModeProperty)} • {CurrentDeviceSettingsMode}";


        /// <summary>
        ///     The friendly (product) name of this device.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var name = _device.GetProperty<string>(DevicePropertyKey.Device_FriendlyName);

                return string.IsNullOrEmpty(name) ? "DS3 Compatible HID Device" : name;
            }
        }


        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public string DeviceAddress => _device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();

        /// <summary>
        ///     The Bluetooth MAC address of this device.
        /// </summary>
        public string DeviceAddressFriendly
        {
            get
            {
                var friendlyAddress = DeviceAddress;

                var insertedCount = 0;
                for (var i = 2; i < DeviceAddress.Length; i = i + 2)
                    friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");

                return friendlyAddress;
            }
        }


        /// <summary>
        ///     The Bluetooth MAC address of the host radio this device is currently paired to.
        /// </summary>
        public string HostAddress
        {
            get
            {
                var hostAddress = _device.GetProperty<ulong>(DsHidMiniDriver.HostAddressProperty).ToString("X12")
                    .ToUpper();

                var friendlyAddress = hostAddress;

                var insertedCount = 0;
                for (var i = 2; i < hostAddress.Length; i = i + 2)
                    friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");

                return friendlyAddress;
            }
        }
        
        /// <summary>
        /// The Bluetooth MAC address of the host radio the controller should pair to if in custom pairing mode
        /// </summary>
        [ObservableProperty] private string? _customPairingAddress;

        /// <summary>
        /// The desired Bluetooth pairing mode for the device when plugging via cable or applying settings
        /// </summary>
        private BluetoothPairingMode? _pairingMode;

        /// <summary>
        /// Index of the desired Bluetooth pairing mode
        /// </summary>
        public int PairingMode
        {
            get => (int)_pairingMode;
            set
            {
                _pairingMode = (BluetoothPairingMode)value;
                this.OnPropertyChanged(nameof(PairingMode));
            }

        }


        /// <summary>
        ///     Current battery status.
        /// </summary>
        public DsBatteryStatus BatteryStatus =>
            (DsBatteryStatus)_device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty);

        /// <summary>
        ///     String representation of current battery status
        /// </summary>
        public string BatteryStatusInText =>
            ((DsBatteryStatus)_device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty)).ToString();

        /// <summary>
        ///     Return a battery icon depending on the charge.
        /// </summary>
        public SymbolRegular BatteryIcon
        {
            get
            {
                switch (BatteryStatus)
                {
                    case DsBatteryStatus.Charged:
                        return SymbolRegular.Battery1024;
                    case DsBatteryStatus.Charging:
                        return SymbolRegular.BatteryCharge24;
                    case DsBatteryStatus.Full:
                        return SymbolRegular.Battery1024;
                    case DsBatteryStatus.High:
                        return SymbolRegular.Battery724;
                    case DsBatteryStatus.Medium:
                        return SymbolRegular.Battery524;
                    case DsBatteryStatus.Low:
                        return SymbolRegular.Battery224;
                    case DsBatteryStatus.Dying:
                        return SymbolRegular.Battery024;
                    default:
                        return SymbolRegular.BatteryWarning24;
                }
            }
        }

        /// <summary>
        /// Representation of last pairing attempt status
        /// </summary>
        public SymbolRegular LastPairingStatusIcon
        {
            get
            {
                var ntstatus = _device.GetProperty<int>(DsHidMiniDriver.LastPairingStatusProperty);
                return ( ntstatus == 0) 
                    ? SymbolRegular.DismissCircle24
                    : SymbolRegular.ErrorCircle24;
            }
        }

        //public EFontAwesomeIcon GenuineIcon
        //{
        //    get
        //    {
        //        if (Validator.IsGenuineAddress(PhysicalAddress.Parse(DeviceAddress)))
        //            return EFontAwesomeIcon.Regular_CheckCircle;
        //        return EFontAwesomeIcon.Solid_ExclamationTriangle;
        //    }
        //}


        /// <summary>
        /// The wireless state of the device
        /// </summary>
        public bool IsWireless
        {
            get
            {
                var enumerator = _device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);

                return !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        ///     Icon for connection protocol
        /// </summary>
        public SymbolRegular ConnectionTypeIcon =>
            !IsWireless
                ? SymbolRegular.UsbPlug24
                : SymbolRegular.Bluetooth24;

        /// <summary>
        ///     Last time this device has been seen connected (applies to Bluetooth connected devices only).
        /// </summary>
        public DateTimeOffset LastConnected =>
            _device.GetProperty<DateTimeOffset>(DsHidMiniDriver.BluetoothLastConnectedTimeProperty);

        /// <summary>
        ///     The driver version of the device
        /// </summary>
        public string DriverVersion => _device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion).ToUpper();

        /// <summary>
        ///     The device Instance ID.
        /// </summary>
        public string InstanceId => _device.InstanceId;

        private void UpdateBatteryStatus(object state)
        {
            OnPropertyChanged(nameof(BatteryStatus));
        }



        // ------------------------------------------------------ CONSTRUCTOR

        internal DeviceViewModel(PnPDevice device, DshmConfigManager dshmConfigManager, AppSnackbarMessagesService appSnackbarMessagesService)
        {
            _device = device;
            _dshmConfigManager = dshmConfigManager;
            _appSnackbarMessagesService = appSnackbarMessagesService;
            _batteryQuery = new Timer(UpdateBatteryStatus, null, 10000, 10000);
            deviceUserData = _dshmConfigManager.GetDeviceData(DeviceAddress);
            // Loads correspondent controller data based on controller's MAC address 



            //DisplayName = DeviceAddress;
            RefreshDeviceSettings();
            UpdateSettingsEditor();
        }


        // ------------------------------------------------------ METHODS

        [ObservableProperty] private ProfileData? _selectedProfile;

        [ObservableProperty] public List<ProfileData> _listOfProfiles;

        partial void OnCurrentDeviceSettingsModeChanged(SettingsModes value)
        {
            UpdateSettingsEditor();
        }

        public void UpdateSettingsEditor()
        {
            switch (CurrentDeviceSettingsMode)
            {
                case SettingsModes.Custom:
                    SelectedGroupsVM = DeviceCustomsVM;
                    break;
                case SettingsModes.Profile:
                    SelectedGroupsVM = ProfileCustomsVM;
                    break;
                case SettingsModes.Global:
                default:
                    SelectedGroupsVM = GlobalCustomsVM;
                    break;
            }
            IsProfileSelectorVisible = CurrentDeviceSettingsMode == SettingsModes.Profile;
        }

        partial void OnSelectedProfileChanged(ProfileData? value)
        {
            ProfileCustomsVM.LoadDatasToAllGroups(SelectedProfile.Settings);
        }

        [RelayCommand]
        public void RefreshDeviceSettings()
        {
            // Bluetooth
            PairingMode = (int)deviceUserData.BluetoothPairingMode;
            CustomPairingAddress = deviceUserData.PairingAddress;

            // Settings and selected profile
            CurrentDeviceSettingsMode = deviceUserData.SettingsMode;
            DeviceCustomsVM.LoadDatasToAllGroups(deviceUserData.Settings);
            ListOfProfiles = _dshmConfigManager.GetListOfProfilesWithDefault();
            SelectedProfile = _dshmConfigManager.GetProfile(deviceUserData.GuidOfProfileToUse);
            GlobalCustomsVM.LoadDatasToAllGroups(_dshmConfigManager.GlobalProfile.Settings);
            
            this.OnPropertyChanged(nameof(DeviceSettingsStatus));
        }

        [RelayCommand]
        private void ApplyChanges()
        {
            deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;
            deviceUserData.PairingAddress = CustomPairingAddress;

            deviceUserData.SettingsMode = CurrentDeviceSettingsMode;
            if (CurrentDeviceSettingsMode == SettingsModes.Custom)
            {
                SelectedGroupsVM.SaveAllChangesToBackingData(deviceUserData.Settings);
            }

            if (CurrentDeviceSettingsMode == SettingsModes.Profile)
            {
                deviceUserData.GuidOfProfileToUse = SelectedProfile.ProfileGuid;
            }

            _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
            _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateSuccessMessage();
            this.OnPropertyChanged(nameof(DeviceSettingsStatus));
        }

        [RelayCommand]
        private void RestartDevice()
        {
            if(IsWireless)
            {
                using (var radio = new HostRadio())
                {
                    radio.DisconnectRemoteDevice(DeviceAddress);
                };
            }
            else
            {
                try
                {
                    (_device).RemoveAndSetup();
                }
                catch
                {

                }
            }
        }

    }

}