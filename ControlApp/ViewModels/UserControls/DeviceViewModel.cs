using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC.Models.Drivers;
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
    private readonly AddressValidator _addressValidator;

    private readonly DeviceData _deviceUserData;

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
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
    private List<ProfileData> _listOfProfiles;

    /// <summary>
    ///     The desired Bluetooth pairing mode for the device when plugging via cable or applying settings
    /// </summary>
    private BluetoothPairingMode? _pairingMode;

    [ObservableProperty]
    private bool _isGenuine;


    // ------------------------------------------------------ METHODS

    [ObservableProperty]
    private ProfileData? _selectedProfile;


    // ------------------------------------------------------ CONSTRUCTOR

    internal DeviceViewModel(
        PnPDevice device, 
        DshmDevMan dshmDevMan,
        DshmConfigManager dshmConfigManager,
        AppSnackbarMessagesService appSnackbarMessagesService,
        IContentDialogService contentDialogService,
        AddressValidator addressValidator
        )
    {
        Device = device;
        Log.Logger.Debug($"Creating Device ViewModel for device '{DeviceAddress}'");
        _dshmDevMan = dshmDevMan;
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _contentDialogService = contentDialogService;
        _addressValidator = addressValidator;
        _batteryQuery = new Timer(UpdateBatteryStatus, null, 10000, 10000);
        _deviceUserData = _dshmConfigManager.GetDeviceData(DeviceAddress);
        // Loads correspondent controller data based on controller's MAC address 


        //DisplayName = DeviceAddress;
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
    public SettingsContext ExpectedHidMode => _dshmConfigManager.GetDeviceExpectedHidMode(_deviceUserData);


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
                activeProfile = CurrentDeviceSettingsMode switch
                {
                    SettingsModes.Global => $"{_dshmConfigManager.GlobalProfile}",
                    SettingsModes.Profile => $"{SelectedProfile}",
                    _ => activeProfile
                };

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
    private string? DeviceAddress =>
        Device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty)?.ToUpperInvariant();

    /// <summary>
    ///     The Bluetooth MAC address of this device.
    /// </summary>
    public string? DeviceAddressFriendly
    {
        get
        {
            if (string.IsNullOrEmpty(DeviceAddress))
            {
                return null;
            }

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
                return "Unknown";
            }

            string hostAddress = Device.GetProperty<ulong>(DsHidMiniDriver.HostAddressProperty).ToString("X12")
                .ToUpperInvariant();

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
    public BluetoothPairingMode PairingMode
    {
        get => _pairingMode ?? BluetoothPairingMode.Auto;
        set
        {
            _pairingMode = value;
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
    public SymbolRegular BatteryIcon =>
        BatteryStatus switch
        {
            DsBatteryStatus.Charged => SymbolRegular.Battery1024,
            DsBatteryStatus.Charging => SymbolRegular.BatteryCharge24,
            DsBatteryStatus.Full => SymbolRegular.Battery1024,
            DsBatteryStatus.High => SymbolRegular.Battery724,
            DsBatteryStatus.Medium => SymbolRegular.Battery524,
            DsBatteryStatus.Low => SymbolRegular.Battery224,
            DsBatteryStatus.Dying => SymbolRegular.Battery024,
            _ => SymbolRegular.BatteryWarning24
        };

    /// <summary>
    ///     Representation of last pairing attempt status
    /// </summary>
    public SymbolRegular LastPairingStatusIcon
    {
        get
        {
            int ntStatus = Device.GetProperty<int>(DsHidMiniDriver.LastPairingStatusProperty);
            return ntStatus == 0
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
            string enumerator = Device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName)!;

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
    public string DriverVersion =>
        Device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion)!.ToUpperInvariant();

    /// <summary>
    ///     The device Instance ID.
    /// </summary>
    public string InstanceId => Device.InstanceId;

    private void UpdateBatteryStatus(object? state)
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
        SelectedProfile = CurrentDeviceSettingsMode switch
        {
            SettingsModes.Global => _dshmConfigManager.GlobalProfile,
            SettingsModes.Profile => _dshmConfigManager.GetProfile(_deviceUserData.GuidOfProfileToUse),
            _ => SelectedProfile
        };
    }

    [RelayCommand]
    public async Task RefreshDeviceSettings()
    {
        Log.Logger.Debug("Refreshing ViewModel of Device '{Address}'", DeviceAddress);
        // Bluetooth
        PairingMode = _deviceUserData.BluetoothPairingMode;
        CustomPairingAddress = _deviceUserData.PairingAddress;

        // Settings and selected profile
        CurrentDeviceSettingsMode = _deviceUserData.SettingsMode;
        DeviceCustomsVM.LoadDatasToAllGroups(_deviceUserData.Settings);
        ListOfProfiles = _dshmConfigManager.GetListOfProfilesWithDefault();

        Log.Logger.Information(
            "Device '{S}' set for {SettingsContext} HID Mode (currently in {HidModeShort1}), {BluetoothPairingMode} Bluetooth pairing mode."
            , DeviceAddress, ExpectedHidMode, HidModeShort, PairingMode);
        if (PairingMode == BluetoothPairingMode.Custom)
        {
            Log.Logger.Information("Custom pairing address: {CustomPairingAddress}.", CustomPairingAddress);
        }

        IsGenuine = await _addressValidator.IsGenuineAddress(PhysicalAddress.Parse(DeviceAddress));

        AdjustSettingsTabState();
        OnPropertyChanged(nameof(DeviceSettingsStatus));
        OnPropertyChanged(nameof(IsHidModeMismatched));
    }

    [RelayCommand]
    private async Task ApplyChanges()
    {
        Log.Logger.Information("Saving and applying changes made to Device '{DeviceAddress}'", DeviceAddress);
        _deviceUserData.BluetoothPairingMode = PairingMode;

        string formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
        if (formattedCustomMacAddress.Length > 12)
        {
            formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
        }

        _deviceUserData.PairingAddress = formattedCustomMacAddress;

        _deviceUserData.SettingsMode = CurrentDeviceSettingsMode;
        if (CurrentDeviceSettingsMode == SettingsModes.Custom)
        {
            DeviceCustomsVM.SaveAllChangesToBackingData(_deviceUserData.Settings);
        }

        if (CurrentDeviceSettingsMode == SettingsModes.Profile)
        {
            _deviceUserData.GuidOfProfileToUse = SelectedProfile.ProfileGuid;
        }

        _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateSuccessMessage();
        await RefreshDeviceSettings();
    }

    [RelayCommand]
    private void RestartDevice()
    {
        bool reconnectionResult = DshmDevMan.TryReconnectDevice(Device);
        Log.Logger.Information(
            "User instructed {Wireless} device '{DeviceAddress}' to restart/disconnect.",
            IsWireless ? "wireless" : "wired", DeviceAddress);
        _appSnackbarMessagesService.ShowPowerCyclingDeviceMessage(IsWireless, SecurityUtil.IsElevated,
            reconnectionResult);
    }

    [RelayCommand]
    private async Task TriggerPairingOnHotReload()
    {
        _deviceUserData.BluetoothPairingMode = (BluetoothPairingMode)PairingMode;
        string formattedCustomMacAddress = Regex.Replace(CustomPairingAddress, @"[^a-fA-F0-9]", "").ToUpper();
        if (formattedCustomMacAddress.Length > 12)
        {
            formattedCustomMacAddress = formattedCustomMacAddress.Substring(0, 12);
        }

        _deviceUserData.PairingAddress = formattedCustomMacAddress;

        _deviceUserData.PairOnHotReload = true;
        _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        await ShowPairingDialog();
        _deviceUserData.PairOnHotReload = false;
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
                Content = """
                          Pairing was requested.

                          Wait 2 or 5 seconds before hitting ok to check for results.
                          """,
                //PrimaryButtonText = "Ok",
                //SecondaryButtonText = "Don't Save",
                CloseButtonText = "OK"
            }
        );
    }

    [RelayCommand]
    private async Task PairingHelpButtonPressed()
    {
        ContentDialogResult result = await ShowPairingHelpInfoDialog();
        //if(result == ContentDialogResult.Primary)
        //{
        //    // to-do: open bluetooth pairing troubleshooting page
        //}
    }

    private async Task<ContentDialogResult> ShowPairingHelpInfoDialog()
    {
        ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Bluetooth pairing info",
                Content = """
                          ➤ Pairing is only possible when connected via USB.

                          ➤ NEVER TRY PAIRING A PS3 CONTROLLER VIA WINDOWS' BLUETOOTH SETTINGS MENU!! Pairing is done by the DsHidMini driver itself.

                          ➤ Pairing happens automatically when connecting via USB or when using the "Pair now" button, unless pairing has been set to "Disabled".

                          ➤ The BthPS3 driver is required to be installed and operating for Windows to allow paired controllers to connect.

                          ➤ If in "To this PC" mode, the PC's bluetooth adapter must be enabled and turned ON for pairing to succeed.
                          """,
                PrimaryButtonText = "I need more help!",
                CloseButtonText = "Close"
            }
        );
        return result;
    }
}