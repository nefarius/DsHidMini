using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

public partial class DeviceViewModel : ObservableObject, IDisposable
{
    private readonly AddressValidator _addressValidator;
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly Timer _batteryQuery;
    private readonly IContentDialogService _contentDialogService;

    private int _xInputSlotRefreshGeneration;

    /// <summary>
    ///     Backoff after an XInput miss. Wireless XUSB interfaces and LED slot assignment
    ///     often appear several seconds after the DsHidMini device itself.
    /// </summary>
    private static readonly TimeSpan[] XInputSlotRetryDelays =
    [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

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
    ///     Whether the controller is deemed official Sony genuine by using the online address database.
    /// </summary>
    [ObservableProperty]
    private bool _isGenuine;

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
        Log.Logger.Debug("Creating Device ViewModel for device '{S}'", DeviceAddress);
        _dshmDevMan = dshmDevMan;
        _dshmConfigManager = dshmConfigManager;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _contentDialogService = contentDialogService;
        _addressValidator = addressValidator;
        _batteryQuery = new Timer(UpdateBatteryStatus, null, 10000, 10000);
        _deviceUserData = _dshmConfigManager.GetDeviceData(DeviceAddress);
        _pairingMode = _deviceUserData.BluetoothPairingMode;
        DeviceCustomsVM.ApplyDeviceCapabilities(DeviceType);
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
    ///     True when the driver reports active HID mode XInput (0x05). Used to show XInput player slot only in that mode.
    /// </summary>
    public bool IsXInputHidMode => HidEmulationMode == SettingsContext.XInput;

    /// <summary>
    ///     Full line for the device list (e.g. "XInput: Player 1") when <see cref="IsXInputHidMode" />; otherwise null.
    /// </summary>
    [ObservableProperty]
    private string? _xInputSlotBanner;

    /// <summary>
    ///     Short value for the Info tab (e.g. "Player 1" or "Unavailable"); null when not in XInput HID mode.
    /// </summary>
    [ObservableProperty]
    private string? _xInputSlotDetail;

    /// <summary>
    ///     The Hid Mode the device is expected to be based on the device's user data
    /// </summary>
    public SettingsContext ExpectedHidMode => _dshmConfigManager.GetDeviceExpectedHidMode(_deviceUserData);

    /// <summary>
    ///     Configured Bluetooth HID channel used to send output reports (LEDs/rumble).
    ///     Resolved from custom, profile, or global settings, or the Control default.
    /// </summary>
    public BluetoothOutputReportTransport BluetoothOutputReportTransport =>
        _dshmConfigManager.ResolveEffectiveSettings(_deviceUserData).OutputReport.BluetoothOutputReportTransport;


    /// <summary>
    ///     State of Device's current HID Mode in relation to mode it's expected to be
    /// </summary>
    public bool IsHidModeMismatched => HidEmulationMode != ExpectedHidMode;

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
    ///     Hardware family published by the driver. Missing property is Unknown.
    /// </summary>
    public DsDeviceType DeviceType
    {
        get
        {
            try
            {
                DsDeviceType publishedType =
                    (DsDeviceType)Device.GetProperty<byte>(DsHidMiniDriver.DeviceTypeProperty);
                if (publishedType != DsDeviceType.Unknown)
                {
                    return publishedType;
                }
            }
            catch (Exception)
            {
                // Older driver builds do not publish DeviceTypeProperty.
            }

            return DsDeviceCapabilities.FromInstanceId(Device.InstanceId);
        }
    }

    public bool IsNavigationController => DsDeviceCapabilities.IsNavigation(DeviceType);

    public bool HasRumble => DsDeviceCapabilities.HasRumble(DeviceType);

    public string DeviceTypeDisplay => DsDeviceCapabilities.DisplayName(DeviceType);

    public string DeviceCapabilityNote => DsDeviceCapabilities.HidModeGuidance(DeviceType);

    public bool HasDeviceCapabilityNote => !string.IsNullOrEmpty(DeviceCapabilityNote);

    /// <summary>
    ///     The friendly (product) name of this device.
    /// </summary>
    public string DisplayName
    {
        get
        {
            string? name = Device.GetProperty<string>(DevicePropertyKey.NAME);

            if (IsNavigationController)
            {
                return string.IsNullOrEmpty(name) || name == "DS3 Compatible HID Device"
                    ? DsDeviceCapabilities.DisplayName(DsDeviceType.Navigation)
                    : name;
            }

            return string.IsNullOrEmpty(name) ? "DS3 Compatible HID Device" : name;
        }
    }


    /// <summary>
    ///     The Bluetooth MAC address of this device.
    /// </summary>
    internal string? DeviceAddress =>
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

            string friendly = MacAddressFormatter.ToFriendly(DeviceAddress);

            return IsDeviceAddressSynthesized ? $"{friendly} (not reported by device)" : friendly;
        }
    }

    /// <summary>
    ///     <see langword="true"/> if this device never reported its own Bluetooth MAC address (the driver
    ///     synthesized a deterministic fallback instead). Bluetooth pairing is unavailable in this case. See issue
    ///     #321.
    /// </summary>
    public bool IsDeviceAddressSynthesized =>
        Device.GetProperty<bool>(DsHidMiniDriver.DeviceAddressSynthesizedProperty);

    /// <summary>
    ///     <see langword="true"/> if this device is capable of being paired to a Bluetooth host at all, i.e. it is
    ///     wired and reported its own Bluetooth MAC address. Controls whether pairing UI should be enabled.
    /// </summary>
    public bool SupportsBluetoothPairing =>
        !IsWireless && !IsDeviceAddressSynthesized && !string.IsNullOrEmpty(DeviceAddress);

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

            return MacAddressFormatter.ToFriendly(hostAddress);
        }
    }

    /// <summary>
    ///     The Bluetooth MAC address of the host radio the controller should pair to if in custom pairing mode
    /// </summary>
    public string? CustomPairingAddress
    {
        get => MacAddressFormatter.ToFriendly(_customPairingAddress);
        set
        {
            _customPairingAddress = MacAddressFormatter.Normalize(value);
            OnPropertyChanged();
        }
    }

    public bool IsCustomPairingAddressVisible => PairingMode == BluetoothPairingMode.Custom;

    /// <summary>
    ///     Desired Bluetooth pairing mode. Defaults to pairing to this PC.
    /// </summary>
    public BluetoothPairingMode PairingMode
    {
        get => _pairingMode ?? BluetoothPairingMode.Auto;
        set
        {
            _pairingMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustomPairingAddressVisible));
            OnPropertyChanged(nameof(CanPairNow));
        }
    }

    /// <summary>
    ///     Pair Now is available when the device can be paired and pairing is not disabled.
    /// </summary>
    public bool CanPairNow => SupportsBluetoothPairing && PairingMode != BluetoothPairingMode.Disabled;


    /// <summary>
    ///     Current battery status.
    /// </summary>
    public DsBatteryStatus BatteryStatus => DsDeviceCapabilities.NormalizeBatteryStatus(
        Device.GetProperty<byte>(DsHidMiniDriver.BatteryStatusProperty));

    /// <summary>
    ///     String representation of current battery status
    /// </summary>
    public string BatteryStatusInText => BatteryStatus.ToString();

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
            string enumerator = Device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName) ?? "USB";

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
    ///     Tooltip for the list-card restart / disconnect button.
    /// </summary>
    public string RestartDeviceToolTip =>
        IsWireless
            ? "Disconnect this wireless controller"
            : "Restart this USB controller. Requires running as Administrator.";

    /// <summary>
    ///     Wired controllers can receive the console USB power-off sequence.
    /// </summary>
    public bool CanPowerOffUsb => !IsWireless;

    /// <summary>
    ///     Tooltip for the wired-only USB power-off button.
    /// </summary>
    public string PowerOffUsbDeviceToolTip =>
        "Turn off this USB controller. It stays plugged in; use Restart to wake it.";

    /// <summary>
    ///     Last time this device has been seen connected (applies to Bluetooth connected devices only).
    /// </summary>
    public DateTimeOffset LastConnected =>
        Device.GetProperty<DateTimeOffset>(DsHidMiniDriver.BluetoothLastConnectedTimeProperty);

    /// <summary>
    ///     Display text for <see cref="LastConnected" />. USB sessions have no Bluetooth timestamp.
    /// </summary>
    public string LastConnectedDisplay
    {
        get
        {
            if (!IsWireless)
            {
                return "Only available when connected wirelessly";
            }

            DateTimeOffset lastConnected = LastConnected;
            if (lastConnected == default || lastConnected.Year < 2000)
            {
                return "Unknown";
            }

            return lastConnected.ToLocalTime().ToString("g");
        }
    }

    /// <summary>
    ///     The driver version of the device
    /// </summary>
    public string DriverVersion =>
        Device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion)!.ToUpperInvariant();

    /// <summary>
    ///     <see langword="true"/> if Feature 0x01 identification was published (USB pads
    ///     that answered GET). Bluetooth instances do not have this property.
    /// </summary>
    public bool HasIdentification
    {
        get
        {
            try
            {
                byte[]? raw = Device.GetProperty<byte[]>(DsHidMiniDriver.IdentificationDataProperty);
                return raw is { Length: > 0 };
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Failed to read identification data of device '{Address}'", DeviceAddress);
                return false;
            }
        }
    }

    private DsIdentificationInfo? IdentificationInfo
    {
        get
        {
            try
            {
                byte[]? raw = Device.GetProperty<byte[]>(DsHidMiniDriver.IdentificationDataProperty);
                if (raw is { Length: > 0 })
                {
                    if (DsIdentification.TryParse(raw, out DsIdentificationInfo? parsed))
                    {
                        return parsed;
                    }

                    Log.Logger.Warning(
                        "Failed to parse identification data of device '{Address}'",
                        DeviceAddress);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Failed to read identification data of device '{Address}'", DeviceAddress);
            }

            return null;
        }
    }

    /// <summary>
    ///     Feature 0x01 firmware/board revision, e.g. <c>04 00 08</c>.
    /// </summary>
    public string IdentificationFirmware => IdentificationInfo?.FirmwareDisplay ?? "Unknown";

    /// <summary>
    ///     Feature 0x01 pad/sensor type byte. Informational; not a gyro-path test.
    /// </summary>
    public string IdentificationPadType
    {
        get
        {
            if (IdentificationInfo is null)
            {
                return "Unknown";
            }

            return IdentificationInfo.PadType switch
            {
                0x18 => "DualShock 3-class (0x18)",
                0x17 => "SIXAXIS-class (0x17)",
                byte value => $"Unknown (0x{value:X2})"
            };
        }
    }

    /// <summary>
    ///     Feature 0x01 motion path derived from the calibration field list.
    /// </summary>
    public string IdentificationMotionPath =>
        IdentificationInfo?.MotionPath switch
        {
            DsIdentificationMotionPath.HwCal => "Hardware-calibrated gyro",
            DsIdentificationMotionPath.PlainZero => "Software zero",
            DsIdentificationMotionPath.Sixaxis => "SIXAXIS",
            _ => "Unknown"
        };

    /// <summary>
    ///     Feature 0x01 clone heuristic (field list <c>01 02</c> and byte <c>0x29 == 0x64</c>).
    ///     Not the OUI genuine check.
    /// </summary>
    public bool IdentificationCloneHeuristic => IdentificationInfo?.CloneHeuristic ?? false;

    public string IdentificationCloneHeuristicText =>
        IdentificationInfo is null
            ? "Unknown"
            : IdentificationCloneHeuristic
                ? "Likely counterfeit"
                : "No match";

    /// <summary>
    ///     The device Instance ID.
    /// </summary>
    public string InstanceId => Device.InstanceId;

    private void UpdateBatteryStatus(object? state)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            OnPropertyChanged(nameof(BatteryStatus));
            OnPropertyChanged(nameof(BatteryIcon));
            OnPropertyChanged(nameof(BatteryStatusInText));
        });
    }

    public void Dispose()
    {
        Interlocked.Increment(ref _xInputSlotRefreshGeneration);
        _batteryQuery.Dispose();
        GC.SuppressFinalize(this);
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
            SettingsModes.Profile => _dshmConfigManager.ResolveProfileOrDefault(_deviceUserData.GuidOfProfileToUse),
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

        if (string.IsNullOrWhiteSpace(DeviceAddress))
        {
            IsGenuine = false;
        }
        else
        {
            try
            {
                IsGenuine = await _addressValidator.IsGenuineAddress(PhysicalAddress.Parse(DeviceAddress));
            }
            catch (FormatException ex)
            {
                Log.Logger.Warning(ex, "Failed to parse device address '{DeviceAddress}' as PhysicalAddress.",
                    DeviceAddress);
                IsGenuine = false;
            }
        }

        AdjustSettingsTabState();
        OnPropertyChanged(nameof(DeviceSettingsStatus));
        OnPropertyChanged(nameof(IsHidModeMismatched));
        OnPropertyChanged(nameof(BluetoothOutputReportTransport));
        await RefreshXInputSlotLabelAsync();
    }

    /// <summary>
    ///     Re-resolves the XInput player slot without reloading the rest of the device settings.
    /// </summary>
    internal Task RefreshXInputSlotAsync() => RefreshXInputSlotLabelAsync();

    private async Task RefreshXInputSlotLabelAsync()
    {
        int refreshGeneration = Interlocked.Increment(ref _xInputSlotRefreshGeneration);

        OnPropertyChanged(nameof(IsXInputHidMode));
        if (HidEmulationMode != SettingsContext.XInput)
        {
            XInputSlotBanner = null;
            XInputSlotDetail = null;
            return;
        }

        PnPDevice device = Device;
        (bool ok, byte userIndex) = await ResolveXInputUserIndexAsync(device, ignoreNegativeCache: false);
        if (await TryApplyXInputSlotResultAsync(refreshGeneration, ok, userIndex))
        {
            return;
        }

        _ = RetryXInputSlotLabelAsync(refreshGeneration, device);
    }

    private async Task RetryXInputSlotLabelAsync(int refreshGeneration, PnPDevice device)
    {
        try
        {
            foreach (TimeSpan delay in XInputSlotRetryDelays)
            {
                await Task.Delay(delay);
                if (refreshGeneration != _xInputSlotRefreshGeneration)
                {
                    return;
                }

                (bool ok, byte userIndex) = await ResolveXInputUserIndexAsync(device, ignoreNegativeCache: true);
                if (await TryApplyXInputSlotResultAsync(refreshGeneration, ok, userIndex))
                {
                    return;
                }
            }

            await ApplyXInputSlotLabelsAsync(refreshGeneration, "Unavailable", "XInput: Unavailable");
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "XInput slot retry failed for device '{Address}'", DeviceAddress);
        }
    }

    private static Task<(bool ok, byte userIndex)> ResolveXInputUserIndexAsync(PnPDevice device,
        bool ignoreNegativeCache)
    {
        return Task.Run(() =>
        {
            bool success = XInputSlotResolver.TryGetXInputUserIndex(device, out byte idx, ignoreNegativeCache);
            return (success, idx);
        });
    }

    private async Task<bool> TryApplyXInputSlotResultAsync(int refreshGeneration, bool ok, byte userIndex)
    {
        if (!ok)
        {
            return false;
        }

        await ApplyXInputSlotLabelsAsync(refreshGeneration, $"Player {userIndex + 1}",
            $"XInput: Player {userIndex + 1}");
        return true;
    }

    private async Task ApplyXInputSlotLabelsAsync(int refreshGeneration, string detail, string banner)
    {
        if (Application.Current is null)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (refreshGeneration != _xInputSlotRefreshGeneration)
            {
                return;
            }

            if (HidEmulationMode != SettingsContext.XInput)
            {
                XInputSlotBanner = null;
                XInputSlotDetail = null;
                return;
            }

            OnPropertyChanged(nameof(IsXInputHidMode));
            XInputSlotDetail = detail;
            XInputSlotBanner = banner;
        });
    }

    [RelayCommand]
    private async Task ApplyChanges()
    {
        Log.Logger.Information("Saving and applying changes made to Device '{DeviceAddress}'", DeviceAddress);
        _deviceUserData.BluetoothPairingMode = PairingMode;

        _deviceUserData.PairingAddress = MacAddressFormatter.Normalize(CustomPairingAddress);

        _deviceUserData.SettingsMode = CurrentDeviceSettingsMode;
        if (CurrentDeviceSettingsMode == SettingsModes.Custom)
        {
            DeviceCustomsVM.SaveAllChangesToBackingData(_deviceUserData.Settings);
        }

        if (CurrentDeviceSettingsMode == SettingsModes.Profile)
        {
            _deviceUserData.GuidOfProfileToUse =
                (SelectedProfile ?? _dshmConfigManager.ResolveProfileOrDefault(_deviceUserData.GuidOfProfileToUse))
                .ProfileGuid;
        }

        bool updated = _dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile();
        if (updated)
        {
            _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateSuccessMessage();
        }
        else
        {
            _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateFailedMessage();
        }

        await RefreshDeviceSettings();
    }

    /// <summary>
    ///     Writes the expected HID mode into DEVPKEY_DsHidMini_RW_HidDeviceMode before requesting a reconnect, so the
    ///     driver's D0Entry-time mismatch check (see issue #374) sees a matching value on the very next power-up
    ///     instead of the still-stale one the PnP-time filters probed. Best-effort: setting a device property
    ///     requires elevation, same as <see cref="DshmDevMan.TryReconnectDevice" />.
    /// </summary>
    public bool ApplyExpectedHidModeProperty()
    {
        try
        {
            Device.SetProperty(DsHidMiniDriver.HidDeviceModeProperty,
                DshmDriverTranslationUtils.ToHidDeviceModePropertyValue(ExpectedHidMode));
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex,
                "Failed to write expected HID mode {ExpectedHidMode} to device '{DeviceAddress}' before reconnecting.",
                ExpectedHidMode, DeviceAddress);
            return false;
        }
    }

    [RelayCommand]
    private void RestartDevice()
    {
        bool propertyApplyResult = ApplyExpectedHidModeProperty();
        bool reconnectionResult = DshmDevMan.TryReconnectDevice(Device);
        Log.Logger.Information(
            "User instructed {Wireless} device '{DeviceAddress}' to restart/disconnect.",
            IsWireless ? "wireless" : "wired", DeviceAddress);
        _appSnackbarMessagesService.ShowPowerCyclingDeviceMessage(IsWireless, SecurityUtil.IsElevated,
            reconnectionResult && propertyApplyResult);
    }

    [RelayCommand]
    private async Task PowerOffUsbDevice()
    {
        if (!CanPowerOffUsb)
        {
            return;
        }

        ContentDialogResult confirmation = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Turn off controller?",
                Content = """
                          This sends the PlayStation 3 USB power-off sequence. The controller stays plugged in and can keep charging, but LEDs and input stop.

                          Use Restart on this card to wake it again.
                          """,
                PrimaryButtonText = "Turn off",
                CloseButtonText = "Cancel"
            }
        );

        if (confirmation != ContentDialogResult.Primary)
        {
            return;
        }

        int? slot = DsHidMiniInterop.TryGetIpcSlotIndex(Device);
        if (slot is not int deviceIndex)
        {
            Log.Logger.Warning(
                "USB power-off skipped for '{DeviceAddress}': no readable IPC slot.",
                DeviceAddress);
            _appSnackbarMessagesService.ShowUsbPowerOffFailedMessage(
                "The driver did not report an IPC slot for this device.");
            return;
        }

        if (!DsHidMiniInterop.IsAvailable)
        {
            _appSnackbarMessagesService.ShowUsbPowerOffFailedMessage(
                "Driver IPC is not available. Confirm the controller is still connected.");
            return;
        }

        try
        {
            PowerOffUsbResult result = await Task.Run(() =>
            {
                using DsHidMiniInterop interop = new();
                return interop.PowerOffUsbDevice(deviceIndex);
            });

            Log.Logger.Information(
                "USB power-off for '{DeviceAddress}' slot {Slot}: {Result}",
                DeviceAddress,
                deviceIndex,
                result);

            if (result.Succeeded)
            {
                _appSnackbarMessagesService.ShowUsbPowerOffSucceededMessage();
            }
            else
            {
                _appSnackbarMessagesService.ShowUsbPowerOffFailedMessage(
                    $"{result}. Restart the controller if it stopped responding.");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "USB power-off failed for '{DeviceAddress}'", DeviceAddress);
            _appSnackbarMessagesService.ShowUsbPowerOffFailedMessage(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PairNow()
    {
        if (!CanPairNow)
        {
            _appSnackbarMessagesService.ShowPairingFailedMessage(
                "Pairing is disabled for this controller.");
            return;
        }

        _deviceUserData.BluetoothPairingMode = PairingMode;
        _deviceUserData.PairingAddress = MacAddressFormatter.Normalize(CustomPairingAddress);

        if (!_dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile())
        {
            _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateFailedMessage();
            return;
        }

        int? slot = DsHidMiniInterop.TryGetIpcSlotIndex(Device);
        if (slot is not int deviceIndex)
        {
            Log.Logger.Warning(
                "Pairing skipped for '{DeviceAddress}': no readable IPC slot.",
                DeviceAddress);
            _appSnackbarMessagesService.ShowPairingFailedMessage(
                "The driver did not report an IPC slot for this device.");
            return;
        }

        if (!DsHidMiniInterop.IsAvailable)
        {
            _appSnackbarMessagesService.ShowPairingFailedMessage(
                "Driver IPC is not available. Confirm the controller is still connected.");
            return;
        }

        PhysicalAddress? customHostAddress = null;
        if (PairingMode == BluetoothPairingMode.Custom)
        {
            try
            {
                customHostAddress = ParsePairingAddress();
            }
            catch (FormatException ex)
            {
                _appSnackbarMessagesService.ShowPairingFailedMessage(ex.Message);
                return;
            }
        }

        try
        {
            SetHostResult result = await Task.Run(() =>
            {
                using DsHidMiniInterop interop = new();
                return customHostAddress is not null
                    ? interop.SetHostAddress(deviceIndex, customHostAddress)
                    : interop.PairToCurrentHost(deviceIndex);
            });

            Log.Logger.Information(
                "Pairing for '{DeviceAddress}' slot {Slot}: {Result}",
                DeviceAddress,
                deviceIndex,
                result);

            if (result.Succeeded)
            {
                _appSnackbarMessagesService.ShowPairingSucceededMessage();
            }
            else
            {
                _appSnackbarMessagesService.ShowPairingFailedMessage(result.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Pairing failed for '{DeviceAddress}'", DeviceAddress);
            _appSnackbarMessagesService.ShowPairingFailedMessage(ex.Message);
        }

        OnPropertyChanged(nameof(HostAddress));
        OnPropertyChanged(nameof(LastPairingStatusIcon));
    }

    private PhysicalAddress ParsePairingAddress()
    {
        string normalized = MacAddressFormatter.Normalize(CustomPairingAddress);
        if (normalized.Length != 12)
        {
            throw new FormatException("Custom pairing address must be a 12-digit Bluetooth MAC.");
        }

        return PhysicalAddress.Parse(normalized);
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

                          ➤ Retro Fighters Defender Bluetooth Edition owners: plug the controller in via USB, then use the "Switch to PS3 mode" button on the Devices page (shown when it's detected in DualShock 4 mode) before pairing.
                          """,
                PrimaryButtonText = "I need more help!",
                CloseButtonText = "Close"
            }
        );
        return result;
    }
}