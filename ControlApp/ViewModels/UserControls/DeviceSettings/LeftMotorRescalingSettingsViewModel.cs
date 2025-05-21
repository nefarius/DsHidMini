using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class LeftMotorRescalingSettingsViewModel : DeviceSettingsViewModel
{
    private readonly LeftMotorRescalingSettings _tempBackingData = new();
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.RumbleLeftStrRescale;

    public bool IsLeftMotorStrRescalingEnabled
    {
        get => _tempBackingData.IsLeftMotorStrRescalingEnabled;
        set
        {
            _tempBackingData.IsLeftMotorStrRescalingEnabled = value;
            OnPropertyChanged();
        }
    }

    public int LeftMotorStrRescalingUpperRange
    {
        get => _tempBackingData.LeftMotorStrRescalingUpperRange;
        set
        {
            _tempBackingData.LeftMotorStrRescalingUpperRange = value < _tempBackingData.LeftMotorStrRescalingLowerRange
                ? _tempBackingData.LeftMotorStrRescalingLowerRange + 1
                : value;
            OnPropertyChanged();
        }
    }

    public int LeftMotorStrRescalingLowerRange
    {
        get => _tempBackingData.LeftMotorStrRescalingLowerRange;
        set
        {
            _tempBackingData.LeftMotorStrRescalingLowerRange = value > _tempBackingData.LeftMotorStrRescalingUpperRange
                ? _tempBackingData.LeftMotorStrRescalingUpperRange - 1
                : value;
            OnPropertyChanged();
        }
    }

    //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_LeftRumbleRescale.CopySettings(dataContainerSource.LeftMotorRescaling, _tempBackingData);
    //}

    //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_LeftRumbleRescale.CopySettings(_tempBackingData, dataContainerSource.LeftMotorRescaling);
    //    NotifyAllPropertiesHaveChanged();
    //}
}