using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class CemuhookUdpSettingsViewModel : DeviceSettingsViewModel
{
    private readonly CemuhookUdpSettings _tempBackingData = new();
    protected override DeviceSubSettings _mySubSetting => _tempBackingData;
    public override SettingsModeGroups Group { get; } = SettingsModeGroups.CemuhookUdpBridge;

    public bool Enabled
    {
        get => _tempBackingData.Enabled;
        set
        {
            _tempBackingData.Enabled = value;
            OnPropertyChanged();
        }
    }

    public string IpAddress
    {
        get => _tempBackingData.IpAddress;
        set
        {
            _tempBackingData.IpAddress = value;
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get => _tempBackingData.Port;
        set
        {
            _tempBackingData.Port = Math.Clamp(value, 1, 65535);
            OnPropertyChanged();
        }
    }
}
