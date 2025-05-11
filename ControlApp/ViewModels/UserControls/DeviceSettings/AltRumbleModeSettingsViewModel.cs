﻿using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class AltRumbleModeSettingsViewModel : DeviceSettingsViewModel
{
    // -------------------------------------------- RIGHT MOTOR CONVERSION GROUP

    public AltRumbleModeSettings _tempBackingData = new();
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.RumbleRightConversion;

    public int RightRumbleConversionUpperRange
    {
        get => _tempBackingData.RightRumbleConversionUpperRange;
        set
        {
            int tempInt = value < _tempBackingData.RightRumbleConversionLowerRange
                ? _tempBackingData.RightRumbleConversionLowerRange + 1
                : value;
            _tempBackingData.RightRumbleConversionUpperRange = tempInt;
            OnPropertyChanged();
        }
    }

    public int RightRumbleConversionLowerRange
    {
        get => _tempBackingData.RightRumbleConversionLowerRange;
        set
        {
            int tempInt = value > _tempBackingData.RightRumbleConversionUpperRange
                ? (byte)(_tempBackingData.RightRumbleConversionUpperRange - 1)
                : value;
            _tempBackingData.RightRumbleConversionLowerRange = tempInt;
            OnPropertyChanged();
        }
    }

    public bool IsForcedRightMotorLightThresholdEnabled
    {
        get => _tempBackingData.IsForcedRightMotorLightThresholdEnabled;
        set
        {
            _tempBackingData.IsForcedRightMotorLightThresholdEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsForcedRightMotorHeavyThresholdEnabled
    {
        get => _tempBackingData.IsForcedRightMotorHeavyThreasholdEnabled;
        set
        {
            _tempBackingData.IsForcedRightMotorHeavyThreasholdEnabled = value;
            OnPropertyChanged();
        }
    }

    public int ForcedRightMotorLightThreshold
    {
        get => _tempBackingData.ForcedRightMotorLightThreshold;
        set
        {
            _tempBackingData.ForcedRightMotorLightThreshold = value;
            OnPropertyChanged();
        }
    }

    public int ForcedRightMotorHeavyThreshold
    {
        get => _tempBackingData.ForcedRightMotorHeavyThreshold;
        set
        {
            _tempBackingData.ForcedRightMotorHeavyThreshold = value;
            OnPropertyChanged();
        }
    }

    //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_VariablaRightRumbleEmulAdjusts.CopySettings(dataContainerSource.AltRumbleAdjusts, _tempBackingData);
    //}

    //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_VariablaRightRumbleEmulAdjusts.CopySettings(_tempBackingData, dataContainerSource.AltRumbleAdjusts);
    //    NotifyAllPropertiesHaveChanged();
    //}
}