using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class GeneralRumbleSettingsViewModel : DeviceSettingsViewModel
{
    private readonly GeneralRumbleSettings _tempBackingData = new();

    public GeneralRumbleSettingsViewModel()
    {
        AltModeToggleButtonCombo = new ButtonComboViewModel(_tempBackingData.AltModeToggleButtonCombo, 1);
    }

    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.RumbleGeneral;

    public bool IsLeftMotorDisabled
    {
        get => _tempBackingData.IsLeftMotorDisabled;

        set
        {
            _tempBackingData.IsLeftMotorDisabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsRightMotorDisabled
    {
        get => _tempBackingData.IsRightMotorDisabled;

        set
        {
            _tempBackingData.IsRightMotorDisabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsAltModeEnabled
    {
        get => _tempBackingData.IsAltRumbleModeEnabled;
        set
        {
            _tempBackingData.IsAltRumbleModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool AlwaysStartInNormalRumbleMode
    {
        get => _tempBackingData.AlwaysStartInNormalMode;
        set
        {
            _tempBackingData.AlwaysStartInNormalMode = value;
            OnPropertyChanged();
        }
    }

    public ButtonComboViewModel AltModeToggleButtonCombo { get; set; }

    public override void NotifyAllPropertiesHaveChanged()
    {
        base.NotifyAllPropertiesHaveChanged();
        AltModeToggleButtonCombo.NotifyAllPropertiesChanged();
    }
}