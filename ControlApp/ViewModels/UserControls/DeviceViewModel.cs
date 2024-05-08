using System.Net.NetworkInformation;
using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;
using Nefarius.Utilities.Bluetooth;
using Nefarius.Utilities.DeviceManagement.PnP;

using Serilog;

using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Text.RegularExpressions;

namespace Nefarius.DsHidMini.ControlApp.ViewModels
{
    public partial class DeviceViewModel : ObservableObject
    {
        // ------------------------------------------------------ FIELDS

        private readonly DshmConfigManager _dshmConfigManager;
        private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
        private readonly IContentDialogService _contentDialogService;
        private readonly PnPDevice _device;
        private readonly DshmDevMan _dshmDevMan;
        private readonly Timer _batteryQuery;
        private DeviceData deviceUserData;

        public PnPDevice Device => _device;

        /// <summary>
        /// Settings View Model for device's custom settings
        /// Editing allowed, changes saved only if applying settings with custom settings mode selected
        /// </summary>
        [ObservableProperty] private SettingsEditorViewModel _deviceCustomsVM = new() { AllowEditing = true };

        [ObservableProperty] private bool _isEditorEnabled;


        /// <summary>
        /// Determines if the settings editor is visible.
        /// True if in custom mode, false otherwise
        /// </summary>
        [ObservableProperty] private bool _isEditorVisible;

        /// <summary>
        /// Determines if the profile selector is visible.
        /// True if in Global or Profile settings mode, false otherwise
        /// </summary>
        [ObservableProperty] private bool _isProfileSelectorVisible;

        /// <summary>
        /// Determines if the profile selector is enabled.
        /// True if in Profile settings mode, false otherwise
        /// </summary>
        [ObservableProperty] private bool _isProfileSelectorEnabled;

        /// <summary>
        /// Desired settings mode for current device. Saved to device data only if applying settings
        /// </summary>
        [ObservableProperty] private SettingsModes _currentDeviceSettingsMode;

        /// <summary>
        ///     Current HID device emulation mode.
        /// </summary>
        public SettingsContext HidEmulationMode => DshmDriverTranslationUtils.HidDeviceMode[_device.GetProperty<byte>(DsHidMiniDriver.HidDeviceModeProperty)];

        public HidModeShort HidModeShort => (HidModeShort)HidEmulationMode;

    /// <summary>
        /// The Hid Mode the device is expected to be based on the device's user data
        /// </summary>
        public SettingsContext ExpectedHidMode => _dshmConfigManager.GetDeviceExpectedHidMode(deviceUserData);


        /// <summary>
        /// State of Device's current HID Mode in relation to mode it's expected to be
        /// </summary>
        public bool IsHidModeMismatched => (HidEmulationMode != ExpectedHidMode) ? true : false;

        /// <summary>
        /// Summary of device's current HID mode and Settings mode
        /// </summary>
        public string DeviceSettingsStatus
        {
            get
            {
                string activeProfile = "";
                if (CurrentDeviceSettingsMode != SettingsModes.Custom)
                {
                    switch (CurrentDeviceSettingsMode)
                    {
                        case SettingsModes.Global:
                            activeProfile = $"{_dshmConfigManager.GlobalProfile.ToString()}";
                            break;
                        case SettingsModes.Profile:
                            activeProfile = $"{SelectedProfile.ToString()}";
                            break;
                        default: break;
                    }

                    activeProfile = $" 🡪 {activeProfile}";
                }
                return $"{CurrentDeviceSettingsMode}{activeProfile}";
            }
        }


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
                if (!WasLastHostRequestSuccessful)
                {
                    return "Unkown";
                }
                var hostAddress = _device.GetProperty<ulong>(DsHidMiniDriver.HostAddressProperty).ToString("X12")
                    .ToUpper();

                var friendlyAddress = hostAddress;

                var insertedCount = 0;
                for (var i = 2; i < hostAddress.Length; i = i + 2)
                    friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");

                return friendlyAddress;
            }
        }
        

        private string? _customPairingAddress = "";

        /// <summary>
        /// The Bluetooth MAC address of the host radio the controller should pair to if in custom pairing mode
        /// </summary>
        public string? CustomPairingAddress
        {
            get
            {
                var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
                var replace = "$1:$2:$3:$4:$5:$6";
                var newformat = Regex.Replace(_customPairingAddress, regex, replace);
                return newformat;
            }
            set
            {
                var formattedCustomMacAddress = Regex.Replace(value, @"[^a-fA-F0-9]", "").ToUpper();
                if (formattedCustomMacAddress.Length > 12)
                {
                    formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
                }
                _customPairingAddress = formattedCustomMacAddress;
                this.OnPropertyChanged(nameof(CustomPairingAddress));
            }
        }

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
                    ? SymbolRegular.CheckmarkCircle24
                    : SymbolRegular.DismissCircle24;
            }
        }

        /// <summary>
        /// True if last host request was sucessful
        /// </summary>
        public bool WasLastHostRequestSuccessful => _device.GetProperty<int>(DsHidMiniDriver.LastHostRequestStatusProperty) == 0;

        /// <summary>
        ///     Representation of genuine status of device
        /// </summary>
        //public SymbolRegular GenuineIcon
        //{
        //    get
        //    {
        //        // if (Validator.IsGenuineAddress(PhysicalAddress.Parse(DeviceAddress)))
        //        //return SymbolRegular.CheckmarkCircle24;
        //        //return SymbolRegular.ErrorCircle24;
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

        internal DeviceViewModel(PnPDevice device, DshmDevMan dshmDevMan, DshmConfigManager dshmConfigManager, AppSnackbarMessagesService appSnackbarMessagesService, IContentDialogService contentDialogService)
        {
            _device = device;
            Log.Logger.Debug($"Creating Device ViewModel for device '{DeviceAddress}'");
            _dshmDevMan = dshmDevMan;
            _dshmConfigManager = dshmConfigManager;
            _appSnackbarMessagesService = appSnackbarMessagesService;
            _contentDialogService = contentDialogService;
            _batteryQuery = new Timer(UpdateBatteryStatus, null, 10000, 10000);
            deviceUserData = _dshmConfigManager.GetDeviceData(DeviceAddress);
            // Loads correspondent controller data based on controller's MAC address 


            //DisplayName = DeviceAddress;
            RefreshDeviceSettings();
        }


        // ------------------------------------------------------ METHODS

        [ObservableProperty] private ProfileData? _selectedProfile;

        [ObservableProperty] public List<ProfileData> _listOfProfiles;

        partial void OnCurrentDeviceSettingsModeChanged(SettingsModes value)
        {
            AdjustSettingsTabState();
        }

        private void AdjustSettingsTabState()
        {
            IsProfileSelectorVisible = CurrentDeviceSettingsMode != SettingsModes.Custom;
            IsProfileSelectorEnabled = CurrentDeviceSettingsMode == SettingsModes.Profile;
            IsEditorVisible = CurrentDeviceSettingsMode == SettingsModes.Custom;
            switch (CurrentDeviceSettingsMode)
            {
                case SettingsModes.Global:
                    SelectedProfile = _dshmConfigManager.GlobalProfile;
                    break;
                case SettingsModes.Profile:
                    SelectedProfile = _dshmConfigManager.GetProfile(deviceUserData.GuidOfProfileToUse);
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        public void RefreshDeviceSettings()
        {
            Log.Logger.Debug($"Refreshing ViewModel of Device '{DeviceAddress}'");
            // Bluetooth
            PairingMode = (int)deviceUserData.BluetoothPairingMode;
            CustomPairingAddress = deviceUserData.PairingAddress;

            // Settings and selected profile
            CurrentDeviceSettingsMode = deviceUserData.SettingsMode;
            DeviceCustomsVM.LoadDatasToAllGroups(deviceUserData.Settings);
            ListOfProfiles = _dshmConfigManager.GetListOfProfilesWithDefault();

            Log.Logger.Information($"Device '{DeviceAddress}' set for {ExpectedHidMode} HID Mode (currently in {HidModeShort}), {(BluetoothPairingMode)PairingMode} Bluetooth pairing mode.");
            if ((BluetoothPairingMode)PairingMode == BluetoothPairingMode.Custom)
            {
                Log.Logger.Information($"Custom pairing address: {CustomPairingAddress}.");
            }
            AdjustSettingsTabState();
            this.OnPropertyChanged(nameof(DeviceSettingsStatus));
            this.OnPropertyChanged(nameof(IsHidModeMismatched));
        }

        [RelayCommand]
        private void ApplyChanges()
        {
            Log.Logger.Information($"Saving and applying changes made to Device '{DeviceAddress}'");
            deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;

            var formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
            if(formattedCustomMacAddress.Length > 12)
            {
                formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
            }
            deviceUserData.PairingAddress = formattedCustomMacAddress;

            deviceUserData.SettingsMode = CurrentDeviceSettingsMode;
            if (CurrentDeviceSettingsMode == SettingsModes.Custom)
            {
                DeviceCustomsVM.SaveAllChangesToBackingData(deviceUserData.Settings);
            }

            if (CurrentDeviceSettingsMode == SettingsModes.Profile)
            {
                deviceUserData.GuidOfProfileToUse = SelectedProfile.ProfileGuid;
            }

            _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
            _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateSuccessMessage();
            RefreshDeviceSettings();
        }

        [RelayCommand]
        private void RestartDevice()
        {
            bool reconnectionResult = _dshmDevMan.TryReconnectDevice(_device);
            Log.Logger.Information($"User instructed {(IsWireless ? "wireless" : "wired")} device '{DeviceAddress}' to restart/disconnect.");
            _appSnackbarMessagesService.ShowPowerCyclingDeviceMessage(IsWireless, Main.IsAdministrator(), reconnectionResult);
        }

        [RelayCommand]
        private void TriggerPairingOnHotReload()
        {
            deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;
            var formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
            if (formattedCustomMacAddress.Length > 12)
            {
                formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
            }
            deviceUserData.PairingAddress = formattedCustomMacAddress;

            deviceUserData.PairOnHotReload = true;
            _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
            Thread.Sleep(3000);
            deviceUserData.PairOnHotReload = false;
            _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        }
    }

}