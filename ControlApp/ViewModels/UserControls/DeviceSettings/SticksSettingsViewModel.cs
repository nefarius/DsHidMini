using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class SticksSettingsViewModel : DeviceSettingsViewModel
{
    // -------------------------------------------- STICKS DEADZONE GROUP
    private readonly SticksSettings _tempBackingData = new();
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.SticksDeadzone;

    public bool ApplyLeftStickDeadZone
    {
        get => _tempBackingData.LeftStickData.IsDeadZoneEnabled;
        set
        {
            _tempBackingData.LeftStickData.IsDeadZoneEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool ApplyRightStickDeadZone
    {
        get => _tempBackingData.RightStickData.IsDeadZoneEnabled;
        set
        {
            _tempBackingData.RightStickData.IsDeadZoneEnabled = value;
            OnPropertyChanged();
        }
    }

    public int LeftStickDeadZone
    {
        get => _tempBackingData.LeftStickData.DeadZone;
        set
        {
            _tempBackingData.LeftStickData.DeadZone = value;
            OnPropertyChanged();
        }
    }

    public int RightStickDeadZone
    {
        get => _tempBackingData.RightStickData.DeadZone;
        set
        {
            _tempBackingData.RightStickData.DeadZone = value;
            OnPropertyChanged();
        }
    }

    public bool InvertLSX
    {
        get => _tempBackingData.LeftStickData.InvertXAxis;
        set
        {
            _tempBackingData.LeftStickData.InvertXAxis = value;
            OnPropertyChanged();
        }
    }

    public bool InvertLSY
    {
        get => _tempBackingData.LeftStickData.InvertYAxis;
        set
        {
            _tempBackingData.LeftStickData.InvertYAxis = value;
            OnPropertyChanged();
        }
    }

    public bool InvertRSX
    {
        get => _tempBackingData.RightStickData.InvertXAxis;
        set
        {
            _tempBackingData.RightStickData.InvertXAxis = value;
            OnPropertyChanged();
        }
    }

    public bool InvertRSY
    {
        get => _tempBackingData.RightStickData.InvertYAxis;
        set
        {
            _tempBackingData.RightStickData.InvertYAxis = value;
            OnPropertyChanged();
        }
    }


    //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_Sticks.CopySettings(dataContainerSource.Sticks, _tempBackingData);
    //}

    //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_Sticks.CopySettings(_tempBackingData, dataContainerSource.Sticks);
    //    NotifyAllPropertiesHaveChanged();
    //}
}