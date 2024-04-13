using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public abstract partial class DeviceSettingsViewModel : ObservableObject
{
    /// Replace with LexLoc
    private static Dictionary<SettingsModeGroups, string> DictGroupHeader = new()
    {
        { SettingsModeGroups.LEDsControl, "LEDs control" },
        { SettingsModeGroups.WirelessSettings, "Wireless" },
        { SettingsModeGroups.SticksDeadzone, "Sticks DeadZone" },
        { SettingsModeGroups.RumbleGeneral, "Rumble" },
        { SettingsModeGroups.OutputReportControl, "Output report control" },
        { SettingsModeGroups.RumbleLeftStrRescale, "Left motor rescale" },
        { SettingsModeGroups.RumbleRightConversion, "Alternative rumble mode adjuster" },
        { SettingsModeGroups.Unique_All, "HID Mode" },
        { SettingsModeGroups.Unique_Global, "Default settings" },
        { SettingsModeGroups.Unique_General, "General settings" },
        { SettingsModeGroups.Unique_SDF, "SDF mode specific settings" },
        { SettingsModeGroups.Unique_GPJ, "GPJ mode specific settings" },
        { SettingsModeGroups.Unique_SXS, "SXS mode specific settings" },
        { SettingsModeGroups.Unique_DS4W, "DS4W mode specific settings" },
        { SettingsModeGroups.Unique_XInput, "GPJ mode specific settings" },
    };

    [ObservableProperty] private List<string> _controlAppProfiles = new List<string>() { "profile 1", "profile 2", "profile 3" };

    public abstract SettingsModeGroups Group { get; }

    protected abstract DeviceSubSettings _mySubSetting { get; }

    [ObservableProperty] private bool _isGroupLocked = false;

    public string? Header { get; }

    [RelayCommand]
    public void ResetGroupToOriginalDefaults()
    {
        _mySubSetting.ResetToDefault();
        NotifyAllPropertiesHaveChanged();
    }

    public virtual void NotifyAllPropertiesHaveChanged()
    {
        this.OnPropertyChanged(string.Empty);
    }

    public void LoadSettingsFromBackingDataContainer(Models.DshmConfigManager.DeviceSettings dataContainerSource)
    {
        if (_mySubSetting is HidModeSettings tempHid) HidModeSettings.CopySettings(tempHid, dataContainerSource.HidMode);
        if (_mySubSetting is LedsSettings tempLeds) LedsSettings.CopySettings(tempLeds, dataContainerSource.LEDs);
        if (_mySubSetting is WirelessSettings tempWireless) WirelessSettings.CopySettings(tempWireless, dataContainerSource.Wireless);
        if (_mySubSetting is SticksSettings tempSticks) SticksSettings.CopySettings(tempSticks, dataContainerSource.Sticks);
        if (_mySubSetting is GeneralRumbleSettings tempGenRumble) GeneralRumbleSettings.CopySettings(tempGenRumble, dataContainerSource.GeneralRumble);
        if (_mySubSetting is OutputReportSettings tempOut) OutputReportSettings.CopySettings(tempOut, dataContainerSource.OutputReport);
        if (_mySubSetting is LeftMotorRescalingSettings tempLeft) LeftMotorRescalingSettings.CopySettings(tempLeft, dataContainerSource.LeftMotorRescaling);
        if (_mySubSetting is AltRumbleModeSettings tempAlt) AltRumbleModeSettings.CopySettings(tempAlt, dataContainerSource.AltRumbleAdjusts);
        NotifyAllPropertiesHaveChanged();
    }

    public void SaveSettingsToBackingDataContainer(Models.DshmConfigManager.DeviceSettings dataContainerSource)
    {
        if (_mySubSetting is HidModeSettings tempHid) HidModeSettings.CopySettings(dataContainerSource.HidMode, tempHid);
        if (_mySubSetting is LedsSettings tempLeds) LedsSettings.CopySettings(dataContainerSource.LEDs, tempLeds);
        if (_mySubSetting is WirelessSettings tempWireless) WirelessSettings.CopySettings(dataContainerSource.Wireless, tempWireless);
        if (_mySubSetting is SticksSettings tempSticks) SticksSettings.CopySettings(dataContainerSource.Sticks, tempSticks);
        if (_mySubSetting is GeneralRumbleSettings tempGenRumble) GeneralRumbleSettings.CopySettings(dataContainerSource.GeneralRumble, tempGenRumble);
        if (_mySubSetting is OutputReportSettings tempOut) OutputReportSettings.CopySettings(dataContainerSource.OutputReport, tempOut);
        if (_mySubSetting is LeftMotorRescalingSettings tempLeft) LeftMotorRescalingSettings.CopySettings(dataContainerSource.LeftMotorRescaling, tempLeft);
        if (_mySubSetting is AltRumbleModeSettings tempAlt) AltRumbleModeSettings.CopySettings(dataContainerSource.AltRumbleAdjusts, tempAlt);
    }

    public DeviceSettingsViewModel()
    {
        if (DictGroupHeader.TryGetValue(Group, out string groupHeader)) Header = groupHeader;
        //LoadSettingsFromBackingDataContainer(backingDataContainer);
    }
}

public partial class ButtonComboViewModel : ObservableObject
{
    private ButtonsCombo _buttonCombo;

    public bool IsEnabled
    {
        get => _buttonCombo.IsEnabled;
        set
        {
            _buttonCombo.IsEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public int HoldTime
    {
        get => _buttonCombo.HoldTime;
        set
        {
            _buttonCombo.HoldTime = value;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public int MinHoldTime { get; }

    public Button Button1
    {
        get => _buttonCombo.Button1;
        set
        {
            if (value != _buttonCombo.Button2 && value != _buttonCombo.Button3)
                _buttonCombo.Button1 = value;
            OnPropertyChanged(nameof(Button1));
        }
    }
    public Button Button2
    {
        get => _buttonCombo.Button2;
        set
        {
            if (value != _buttonCombo.Button1 && value != _buttonCombo.Button3)
                _buttonCombo.Button2 = value;
            OnPropertyChanged(nameof(Button2));
        }
    }
    public Button Button3
    {
        get => _buttonCombo.Button3;
        set
        {
            if (value != _buttonCombo.Button1 && value != _buttonCombo.Button2)
                _buttonCombo.Button3 = value;
            OnPropertyChanged(nameof(Button3));
        }
    }

    public ButtonComboViewModel(ButtonsCombo buttonsCombo, int minHoldTime)
    {
        _buttonCombo = buttonsCombo;
        MinHoldTime = minHoldTime;
    }

    public void NotifyAllPropertiesChanged()
    {
        this.OnPropertyChanged(nameof(string.Empty));
    }
}