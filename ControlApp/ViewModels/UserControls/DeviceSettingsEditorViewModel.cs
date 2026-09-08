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

    public bool HideRumbleSettings { get; private set; }

    public string DeviceCapabilityNote { get; private set; } = string.Empty;

    public bool HasDeviceCapabilityNote => !string.IsNullOrEmpty(DeviceCapabilityNote);

    public void ApplyDeviceCapabilities(DsDeviceType deviceType)
    {
        HideRumbleSettings = !DsDeviceCapabilities.HasRumble(deviceType);
        LedsSettingsVM.IsSingleLedDevice = DsDeviceCapabilities.HasSingleLed(deviceType);
        DeviceCapabilityNote = DsDeviceCapabilities.HidModeGuidance(deviceType);
        OnPropertyChanged(nameof(HideRumbleSettings));
        OnPropertyChanged(nameof(DeviceCapabilityNote));
        OnPropertyChanged(nameof(HasDeviceCapabilityNote));
        UpdateLockStateOfGroups();
    }

    private void UpdateLockStateOfGroups()
    {
        foreach (DeviceSettingsViewModel group in groupSettingsList)
        {
            group.IsGroupLocked = false;
            group.IsGroupVisible = true;
        }

        if (HidModeVM.Context == SettingsContext.DS4W)
        {
            SticksSettingsVM.IsGroupLocked = HidModeVM.PreventRemappingConflictsInDS4WMode;
        }

        if (HidModeVM.Context == SettingsContext.SXS)
        {
            // Rumble related settings currently don't matter in SXS mode
            // rumble instructions are directly passthru to the controller
            GeneralRumbleSettingsVM.IsGroupLocked = true;
            LeftMotorRescaleSettingsVM.IsGroupLocked = true;
            AltRumbleSettingsVM.IsGroupLocked = true;
        }

        if (HideRumbleSettings)
        {
            GeneralRumbleSettingsVM.IsGroupVisible = false;
            LeftMotorRescaleSettingsVM.IsGroupVisible = false;
            AltRumbleSettingsVM.IsGroupVisible = false;
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