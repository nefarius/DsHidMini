using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;
using Nefarius.DsHidMini.IPC.Models.Drivers;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls;

public partial class SettingsEditorViewModel : ObservableObject
{
    private readonly List<DeviceSettingsViewModel> groupSettingsList = new();

    [ObservableProperty]
    public bool _allowEditing = false;

    [ObservableProperty]
    private AltRumbleModeSettingsViewModel _altRumbleSettingsVM = new();

    [ObservableProperty]
    private GeneralRumbleSettingsViewModel _generalRumbleSettingsVM = new();

    [ObservableProperty]
    private HidModeSettingsViewModel _hidModeVM = new();

    [ObservableProperty]
    private LedsSettingsViewModel _ledsSettingsVM = new();

    [ObservableProperty]
    private LeftMotorRescalingSettingsViewModel _leftMotorRescaleSettingsVM = new();

    [ObservableProperty]
    private OutputReportSettingsViewModel _outRepSettingsVM = new();

    [ObservableProperty]
    private SticksSettingsViewModel _sticksSettingsVM = new();

    [ObservableProperty]
    private WirelessSettingsViewModel _wirelessSettingsVM = new();

    public SettingsEditorViewModel() : this(null)
    {
    }

    public IReadOnlyList<DeviceSettingsViewModel> Groups => groupSettingsList;

    public IReadOnlyList<DeviceSettingsViewModel> PrimaryGroups { get; }

    public IReadOnlyList<DeviceSettingsViewModel> AdvancedGroups { get; }

    public SettingsEditorViewModel(Models.DshmConfigManager.DeviceSettings? dataContainer = null)
    {
        groupSettingsList.Add(HidModeVM);
        groupSettingsList.Add(LedsSettingsVM);
        groupSettingsList.Add(WirelessSettingsVM);
        groupSettingsList.Add(SticksSettingsVM);
        groupSettingsList.Add(GeneralRumbleSettingsVM);
        groupSettingsList.Add(OutRepSettingsVM);
        groupSettingsList.Add(LeftMotorRescaleSettingsVM);
        groupSettingsList.Add(AltRumbleSettingsVM);
        PrimaryGroups = [HidModeVM, LedsSettingsVM, WirelessSettingsVM, SticksSettingsVM, GeneralRumbleSettingsVM];
        AdvancedGroups = [OutRepSettingsVM, LeftMotorRescaleSettingsVM, AltRumbleSettingsVM];

        HidModeVM.PropertyChanged += ModeSettingsChanged;

        if (dataContainer != null)
        {
            LoadDatasToAllGroups(dataContainer);
        }

        UpdateLockStateOfGroups();
    }

    private DsDeviceType _deviceType = DsDeviceType.Unknown;

    public bool HideRumbleSettings { get; private set; }

    public string DeviceTypeDisplay => DsDeviceCapabilities.DisplayName(_deviceType);

    public string DeviceCapabilityNote { get; private set; } = string.Empty;

    public bool HasDeviceCapabilityNote => !string.IsNullOrEmpty(DeviceCapabilityNote);

    public const string Ds4wStickLockReason =
        "Stick dead zones and axis inversion stay off while \"Prevent remapping issues in DS4W mode\" is enabled. Turn that option off under HID Mode to let the driver apply these adjustments.";

    public const string SxsRumbleLockReason =
        "SXS passes rumble commands directly to the controller, so these driver-side rumble settings have no effect. Select another HID emulation mode to change them.";

    public void ApplyDeviceCapabilities(DsDeviceType deviceType)
    {
        _deviceType = deviceType;
        HideRumbleSettings = !DsDeviceCapabilities.HasRumble(deviceType);
        LedsSettingsVM.IsSingleLedDevice = DsDeviceCapabilities.HasSingleLed(deviceType);
        if (LedsSettingsVM.IsSingleLedDevice)
        {
            if (LedsSettingsVM.LEDMode == 1)
            {
                LedsSettingsVM.LEDMode = 0;
            }

            if (LedsSettingsVM.CurrentLEDCustomsIndex > 0)
            {
                LedsSettingsVM.CurrentLEDCustomsIndex = 0;
            }
        }

        DeviceCapabilityNote = DsDeviceCapabilities.HidModeGuidance(deviceType);
        OnPropertyChanged(nameof(HideRumbleSettings));
        OnPropertyChanged(nameof(DeviceTypeDisplay));
        OnPropertyChanged(nameof(DeviceCapabilityNote));
        OnPropertyChanged(nameof(HasDeviceCapabilityNote));
        UpdateLockStateOfGroups();
    }

    private void UpdateLockStateOfGroups()
    {
        foreach (DeviceSettingsViewModel group in groupSettingsList)
        {
            group.LockReason = string.Empty;
            group.IsGroupVisible = true;
        }

        if (HidModeVM.Context == SettingsContext.DS4W && HidModeVM.PreventRemappingConflictsInDS4WMode)
        {
            SticksSettingsVM.LockReason = Ds4wStickLockReason;
        }

        if (HidModeVM.Context == SettingsContext.SXS)
        {
            // Rumble related settings currently don't matter in SXS mode
            // rumble instructions are directly passthru to the controller
            GeneralRumbleSettingsVM.LockReason = SxsRumbleLockReason;
            LeftMotorRescaleSettingsVM.LockReason = SxsRumbleLockReason;
            AltRumbleSettingsVM.LockReason = SxsRumbleLockReason;
        }

        if (HideRumbleSettings)
        {
            GeneralRumbleSettingsVM.IsGroupVisible = false;
            LeftMotorRescaleSettingsVM.IsGroupVisible = false;
            AltRumbleSettingsVM.IsGroupVisible = false;
        }

        if (!DsDeviceCapabilities.HasLeds(_deviceType))
        {
            LedsSettingsVM.IsGroupVisible = false;
        }

        if (!DsDeviceCapabilities.SupportsBluetooth(_deviceType))
        {
            WirelessSettingsVM.IsGroupVisible = false;
        }
    }

    private void ModeSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "":
            case nameof(HidModeSettingsViewModel.Context):
            case nameof(HidModeSettingsViewModel.PreventRemappingConflictsInSXSMode):
            case nameof(HidModeSettingsViewModel.PreventRemappingConflictsInDS4WMode):
                UpdateLockStateOfGroups();
                break;
        }
    }

    public void SaveAllChangesToBackingData(Models.DshmConfigManager.DeviceSettings dataContainer)
    {
        foreach (DeviceSettingsViewModel group in groupSettingsList)
        {
            group.SaveSettingsToBackingDataContainer(dataContainer);
        }
    }

    public void LoadDatasToAllGroups(Models.DshmConfigManager.DeviceSettings dataContainer)
    {
        foreach (DeviceSettingsViewModel group in groupSettingsList)
        {
            group.LoadSettingsFromBackingDataContainer(dataContainer);
        }
    }
}