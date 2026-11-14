using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class MotionControlsSettingsViewModel : DeviceSettingsViewModel
{
    private readonly MotionControlsSettings _tempBackingData = new();
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;
    public override SettingsModeGroups Group { get; } = SettingsModeGroups.MotionControls;

    public double KalmanFilterStrength
    {
        get => _tempBackingData.KalmanFilterStrength;
        set
        {
            _tempBackingData.KalmanFilterStrength = Math.Clamp(value, 0.0, 1.0);
            OnPropertyChanged();
            OnPropertyChanged(nameof(KalmanFilterStrengthDisplay));
        }
    }

    public double GyroSensitivity
    {
        get => _tempBackingData.GyroSensitivity;
        set
        {
            _tempBackingData.GyroSensitivity = Math.Clamp(value, 0.1, 1.5);
            OnPropertyChanged();
            OnPropertyChanged(nameof(GyroSensitivityDisplay));
        }
    }

    public double AccelSensitivity
    {
        get => _tempBackingData.AccelSensitivity;
        set
        {
            _tempBackingData.AccelSensitivity = Math.Clamp(value, 0.1, 1.5);
            OnPropertyChanged();
            OnPropertyChanged(nameof(AccelSensitivityDisplay));
        }
    }

    public string KalmanFilterStrengthDisplay => $"{KalmanFilterStrength:P0}";
    public string GyroSensitivityDisplay => $"{GyroSensitivity:F1}x";
    public string AccelSensitivityDisplay => $"{AccelSensitivity:F1}x";
}
