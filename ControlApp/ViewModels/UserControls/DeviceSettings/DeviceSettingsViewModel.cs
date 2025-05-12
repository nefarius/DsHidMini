using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public abstract partial class DeviceSettingsViewModel : ObservableObject
{
    /// Replace with LexLoc
    private static readonly Dictionary<SettingsModeGroups, string> DictGroupHeader = new()
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
        { SettingsModeGroups.Unique_XInput, "GPJ mode specific settings" }
    };

    [ObservableProperty]
    private List<string> _controlAppProfiles = new() { "profile 1", "profile 2", "profile 3" };

    [ObservableProperty]
    private bool _isGroupLocked;

    public DeviceSettingsViewModel()
    {
        if (DictGroupHeader.TryGetValue(Group, out string groupHeader))
        {
            Header = groupHeader;
        }
        //LoadSettingsFromBackingDataContainer(backingDataContainer);
    }

    public abstract SettingsModeGroups Group { get; }

    protected abstract DeviceSubSettings _mySubSetting { get; }

    public string? Header { get; }

    [RelayCommand]
    public void ResetGroupToOriginalDefaults()
    {
        _mySubSetting.ResetToDefault();
        NotifyAllPropertiesHaveChanged();
    }

    public virtual void NotifyAllPropertiesHaveChanged()
    {
        OnPropertyChanged(string.Empty);
    }

    public void LoadSettingsFromBackingDataContainer(Models.DshmConfigManager.DeviceSettings dataContainerSource)
    {
        if (_mySubSetting is HidModeSettings)
        {
            HidModeSettings.CopySettings((HidModeSettings)_mySubSetting, dataContainerSource.HidMode);
        }

        if (_mySubSetting is LedsSettings)
        {
            LedsSettings.CopySettings((LedsSettings)_mySubSetting, dataContainerSource.LEDs);
        }

        if (_mySubSetting is WirelessSettings)
        {
            WirelessSettings.CopySettings((WirelessSettings)_mySubSetting, dataContainerSource.Wireless);
        }

        if (_mySubSetting is SticksSettings)
        {
            SticksSettings.CopySettings((SticksSettings)_mySubSetting, dataContainerSource.Sticks);
        }

        if (_mySubSetting is GeneralRumbleSettings)
        {
            GeneralRumbleSettings.CopySettings((GeneralRumbleSettings)_mySubSetting, dataContainerSource.GeneralRumble);
        }

        if (_mySubSetting is OutputReportSettings)
        {
            OutputReportSettings.CopySettings((OutputReportSettings)_mySubSetting, dataContainerSource.OutputReport);
        }

        if (_mySubSetting is LeftMotorRescalingSettings)
        {
            LeftMotorRescalingSettings.CopySettings((LeftMotorRescalingSettings)_mySubSetting,
                dataContainerSource.LeftMotorRescaling);
        }

        if (_mySubSetting is AltRumbleModeSettings)
        {
            AltRumbleModeSettings.CopySettings((AltRumbleModeSettings)_mySubSetting,
                dataContainerSource.AltRumbleAdjusts);
        }

        NotifyAllPropertiesHaveChanged();
    }

    public void SaveSettingsToBackingDataContainer(Models.DshmConfigManager.DeviceSettings dataContainerSource)
    {
        switch (_mySubSetting)
        {
            case HidModeSettings setting:
                HidModeSettings.CopySettings(dataContainerSource.HidMode, setting);
                break;
            case LedsSettings setting:
                LedsSettings.CopySettings(dataContainerSource.LEDs, setting);
                break;
            case WirelessSettings setting:
                WirelessSettings.CopySettings(dataContainerSource.Wireless, setting);
                break;
            case SticksSettings setting:
                SticksSettings.CopySettings(dataContainerSource.Sticks, setting);
                break;
            case GeneralRumbleSettings setting:
                GeneralRumbleSettings.CopySettings(dataContainerSource.GeneralRumble, setting);
                break;
            case OutputReportSettings setting:
                OutputReportSettings.CopySettings(dataContainerSource.OutputReport, setting);
                break;
            case LeftMotorRescalingSettings setting:
                LeftMotorRescalingSettings.CopySettings(dataContainerSource.LeftMotorRescaling, setting);
                break;
            case AltRumbleModeSettings setting:
                AltRumbleModeSettings.CopySettings(dataContainerSource.AltRumbleAdjusts, setting);
                break;
        }
    }
}

public class ButtonComboViewModel : ObservableObject
{
    private readonly ButtonsCombo _buttonCombo;

    public ButtonComboViewModel(ButtonsCombo buttonsCombo, int minHoldTime)
    {
        _buttonCombo = buttonsCombo;
        MinHoldTime = minHoldTime;
    }

    public bool IsEnabled
    {
        get => _buttonCombo.IsEnabled;
        set
        {
            _buttonCombo.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    public int HoldTime
    {
        get => _buttonCombo.HoldTime / 1000;
        set
        {
            _buttonCombo.HoldTime = value * 1000;
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public int MinHoldTime { get; }

    public Button Button1
    {
        get => _buttonCombo.ButtonCombo[0];
        set
        {
            _buttonCombo.ButtonCombo[0] = value;
            OnPropertyChanged();
        }
    }

    public Button Button2
    {
        get => _buttonCombo.ButtonCombo[1];
        set
        {
            _buttonCombo.ButtonCombo[1] = value;
            OnPropertyChanged();
        }
    }

    public Button Button3
    {
        get => _buttonCombo.ButtonCombo[2];
        set
        {
            _buttonCombo.ButtonCombo[2] = value;
            OnPropertyChanged();
        }
    }

    public void NotifyAllPropertiesChanged()
    {
        OnPropertyChanged(nameof(string.Empty));
    }
}