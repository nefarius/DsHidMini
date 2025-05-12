using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class HidModeSettingsViewModel : DeviceSettingsViewModel
{
    public static readonly List<PressureMode> listOfPressureModes = new()
    {
        PressureMode.Digital, PressureMode.Analogue, PressureMode.Default
    };

    public static readonly List<DPadMode> listOfDPadModes = new() { DPadMode.HAT, DPadMode.Buttons };

    private readonly HidModeSettings _tempBackingData = new();

    public readonly List<SettingsContext> hidDeviceModesList = new()
    {
        SettingsContext.SDF,
        SettingsContext.GPJ,
        SettingsContext.SXS,
        SettingsContext.DS4W,
        SettingsContext.XInput
    };

    public List<SettingsContext> HIDDeviceModesList => hidDeviceModesList;
    public static List<PressureMode> ListOfPressureModes => listOfPressureModes;
    public static List<DPadMode> ListOfDPadModes => listOfDPadModes;
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.Unique_All;

    public SettingsContext Context
    {
        get => _tempBackingData.SettingsContext;
        set
        {
            _tempBackingData.SettingsContext = value;
            OnPropertyChanged();
        }
    }

    public PressureMode PressureExposureMode
    {
        get => _tempBackingData.PressureExposureMode;
        set
        {
            _tempBackingData.PressureExposureMode = value;
            OnPropertyChanged();
        }
    }

    public DPadMode DPadExposureMode
    {
        get => _tempBackingData.DPadExposureMode;
        set
        {
            _tempBackingData.DPadExposureMode = value;
            OnPropertyChanged();
        }
    }

    // SXS 
    public bool PreventRemappingConflictsInSXSMode
    {
        get => _tempBackingData.PreventRemappingConflictsInSXSMode;
        set
        {
            _tempBackingData.PreventRemappingConflictsInSXSMode = value;
            OnPropertyChanged();
        }
    }

    public bool AllowAppsToControlLEDsInSXSMode
    {
        get => _tempBackingData.AllowAppsToOverrideLEDsInSXSMode;
        set
        {
            _tempBackingData.AllowAppsToOverrideLEDsInSXSMode = value;
            OnPropertyChanged();
        }
    }

    // XInput
    public bool IsLEDsAsXInputSlotEnabled
    {
        get => _tempBackingData.IsLEDsAsXInputSlotEnabled;
        set
        {
            _tempBackingData.IsLEDsAsXInputSlotEnabled = value;
            OnPropertyChanged();
        }
    }

    // DS4Windows
    public bool PreventRemappingConflictsInDS4WMode
    {
        get => _tempBackingData.PreventRemappingConflictsInDS4WMode;
        set
        {
            _tempBackingData.PreventRemappingConflictsInDS4WMode = value;
            OnPropertyChanged();
        }
    }


    //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_ModesUnique.CopySettings(_tempBackingData, dataContainerSource.SettingsContext);
    //    NotifyAllPropertiesHaveChanged();
    //}

    //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_ModesUnique.CopySettings(dataContainerSource.SettingsContext, _tempBackingData);
    //}
}