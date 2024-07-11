using System.Text.RegularExpressions;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Enums;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

public partial class DeviceViewModel : ObservableObject
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly Timer _batteryQuery;
    private readonly IContentDialogService _contentDialogService;

    // ------------------------------------------------------ FIELDS

    private readonly DshmConfigManager _dshmConfigManager;
    private readonly DshmDevMan _dshmDevMan;

    /// <summary>
    ///     Desired settings mode for current device. Saved to device data only if applying settings
    /// </summary>
    [ObservableProperty]
    private SettingsModes _currentDeviceSettingsMode;


    private string? _customPairingAddress = "";

    /// <summary>
    ///     Settings View Model for device's custom settings
    ///     Editing allowed, changes saved only if applying settings with custom settings mode selected
    /// </summary>
    [ObservableProperty]
    private SettingsEditorViewModel _deviceCustomsVM = new() { AllowEditing = true };

    [ObservableProperty]
    private bool _isEditorEnabled;


    /// <summary>
    ///     Determines if the settings editor is visible.
    ///     True if in custom mode, false otherwise
    /// </summary>
    [ObservableProperty]
    private bool _isEditorVisible;

    /// <summary>
    ///     Determines if the profile selector is enabled.
    ///     True if in Profile settings mode, false otherwise
    /// </summary>
    [ObservableProperty]
    private bool _isProfileSelectorEnabled;

    /// <summary>
    ///     Determines if the profile selector is visible.
    ///     True if in Global or Profile settings mode, false otherwise
    /// </summary>
    [ObservableProperty]
    private bool _isProfileSelectorVisible;

    [ObservableProperty]
    public List<ProfileData> _listOfProfiles;

    /// <summary>
    ///     The desired Bluetooth pairing mode for the device when plugging via cable or applying settings
    /// </summary>
    private BluetoothPairingMode? _pairingMode;


    // ------------------------------------------------------ METHODS

    [ObservableProperty]
    private ProfileData? _selectedProfile;

    private readonly DeviceData deviceUserData;


    // ------------------------------------------------------ CONSTRUCTOR

    internal DeviceViewModel(PnPDevice device, DshmDevMan dshmDevMan, DshmConfigManager dshmConfigManager,
        AppSnackbarMessagesService appSnackbarMessagesService, IContentDialogService contentDialogService)
    {
        Device = device;
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

    public PnPDevice Device { get; }

    /// <summary>
    ///     Current HID device emulation mode.
    /// </summary>
    public SettingsContext HidEmulationMode =>
        DshmDriverTranslationUtils.HidDeviceMode[Device.GetProperty<byte>(DsHidMiniDriver.HidDeviceModeProperty)];

    public HidModeShort HidModeShort => (HidModeShort)HidEmulationMode;

    /// <summary>
    ///     The Hid Mode the device is expected to be based on the device's user data
    /// </summary>
    public SettingsContext ExpectedHidMode => _dshmConfigManager.GetDeviceExpectedHidMode(deviceUserData);


    /// <summary>
    ///     State of Device's current HID Mode in relation to mode it's expected to be
    /// </summary>
    public bool IsHidModeMismatched => HidEmulationMode != ExpectedHidMode ? true : false;

    /// <summary>
    ///     Summary of device's current HID mode and Settings mode
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
                        activeProfile = $"{_dshmConfigManager.GlobalProfile}";
                        break;
                    case SettingsModes.Profile:
                        activeProfile = $"{SelectedProfile}";
                        break;
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
            string? name = Device.GetProperty<string>(DevicePropertyKey.Device_FriendlyName);

            return string.IsNullOrEmpty(name) ? "DS3 Compatible HID Device" : name;
        }
    }


    /// <summary>
    ///     The Bluetooth MAC address of this device.
    /// </summary>
    public string DeviceAddress => Device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty).ToUpper();

    /// <summary>
    ///     The Bluetooth MAC address of this device.
    /// </summary>
    public string DeviceAddressFriendly
    {
        get
        {
            string friendlyAddress = DeviceAddress;

            int insertedCount = 0;
            for (int i = 2; i < DeviceAddress.Length; i = i + 2)
            {
                friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");
            }

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

            string hostAddress = Device.GetProperty<ulong>(DsHidMiniDriver.HostAddressProperty).ToString("X12")
                .ToUpper();

            string friendlyAddress = hostAddress;

            int insertedCount = 0;
            for (int i = 2; i < hostAddress.Length; i = i + 2)
            {
                friendlyAddress = friendlyAddress.Insert(i + insertedCount++, ":");
            }

            return friendlyAddress;
        }
    }

    /// <summary>
    ///     The Bluetooth MAC address of the host radio the controller should pair to if in custom pairing mode
    /// </summary>
    public string? CustomPairingAddress
    {
        get
        {
            string regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            string replace = "$1:$2:$3:$4:$5:$6";
            string newformat = Regex.Replace(_customPairingAddress, regex, replace);
            return newformat;
        }
        set
        {
            string formattedCustomMacAddress = Regex.Replace(value, @"[^a-fA-F0-9]", string.Empty).ToUpper();
            if (formattedCustomMacAddress.Length > 12)
            {
                formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
            }

            _customPairingAddress = formattedCustomMacAddress;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Index of the desired Bluetooth pairing mode
    /// </summary>
    public int PairingMode
    {
        get => (int)_pairingMode;
        set
        {
            _pairingMode = (BluetoothPairingMode)value;
            OnPropertyChanged();
        }
    }


    /// <summary>
    ///     Current battery status.
    /// </summary>
    public DsBatteryStatus BatteryStatus =>
        (DsBatteryStatus)Device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty);

    /// <summary>
    ///     String representation of current battery status
    /// </summary>
    public string BatteryStatusInText =>
        ((DsBatteryStatus)Device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty)).ToString();

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
    ///     Representation of last pairing attempt status
    /// </summary>
    public SymbolRegular LastPairingStatusIcon
    {
        get
        {
            int ntstatus = Device.GetProperty<int>(DsHidMiniDriver.LastPairingStatusProperty);
            return ntstatus == 0
                ? SymbolRegular.CheckmarkCircle24
                : SymbolRegular.DismissCircle24;
        }
    }

    /// <summary>
    ///     True if last host request was sucessful
    /// </summary>
    public bool WasLastHostRequestSuccessful =>
        Device.GetProperty<int>(DsHidMiniDriver.LastHostRequestStatusProperty) == 0;

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
    ///     The wireless state of the device
    /// </summary>
    public bool IsWireless
    {
        get
        {
            string? enumerator = Device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName);

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
        Device.GetProperty<DateTimeOffset>(DsHidMiniDriver.BluetoothLastConnectedTimeProperty);

    /// <summary>
    ///     The driver version of the device
    /// </summary>
    public string DriverVersion => Device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion).ToUpper();

    /// <summary>
    ///     The device Instance ID.
    /// </summary>
    public string InstanceId => Device.InstanceId;

    private void UpdateBatteryStatus(object state)
    {
        OnPropertyChanged(nameof(BatteryStatus));
    }

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

        Log.Logger.Information(
            $"Device '{DeviceAddress}' set for {ExpectedHidMode} HID Mode (currently in {HidModeShort}), {(BluetoothPairingMode)PairingMode} Bluetooth pairing mode.");
        if ((BluetoothPairingMode)PairingMode == BluetoothPairingMode.Custom)
        {
            Log.Logger.Information($"Custom pairing address: {CustomPairingAddress}.");
        }

        AdjustSettingsTabState();
        OnPropertyChanged(nameof(DeviceSettingsStatus));
        OnPropertyChanged(nameof(IsHidModeMismatched));
    }

    [RelayCommand]
    private void ApplyChanges()
    {
        Log.Logger.Information($"Saving and applying changes made to Device '{DeviceAddress}'");
        deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;

        string formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
        if (formattedCustomMacAddress.Length > 12)
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
        bool reconnectionResult = _dshmDevMan.TryReconnectDevice(Device);
        Log.Logger.Information(
            $"User instructed {(IsWireless ? "wireless" : "wired")} device '{DeviceAddress}' to restart/disconnect.");
        _appSnackbarMessagesService.ShowPowerCyclingDeviceMessage(IsWireless, SecurityUtil.IsElevated,
            reconnectionResult);
    }

    [RelayCommand]
    private async Task TriggerPairingOnHotReload()
    {
        deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;
        string formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
        if (formattedCustomMacAddress.Length > 12)
        {
            formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
        }

        deviceUserData.PairingAddress = formattedCustomMacAddress;

        deviceUserData.PairOnHotReload = true;
        _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        await ShowPairingDialog();
        deviceUserData.PairOnHotReload = false;
        _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        OnPropertyChanged(nameof(HostAddress));
        OnPropertyChanged(nameof(LastPairingStatusIcon));
    }

    private async Task ShowPairingDialog()
    {
        ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Manual pairing triggered",
                Content = @"Pairing was requested.

Wait 2 or 5 seconds before hitting ok to check for results.",
                //PrimaryButtonText = "Ok",
                //SecondaryButtonText = "Don't Save",
                CloseButtonText = "OK"
            }
        );
    }
}