﻿using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

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

        HidModeVM.PropertyChanged += ModeSettingsChanged;

        if (dataContainer != null)
        {
            LoadDatasToAllGroups(dataContainer);
        }

        UpdateLockStateOfGroups();
    }

    private void UpdateLockStateOfGroups()
    {
        foreach (DeviceSettingsViewModel group in groupSettingsList)
        {
            group.IsGroupLocked = false;
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