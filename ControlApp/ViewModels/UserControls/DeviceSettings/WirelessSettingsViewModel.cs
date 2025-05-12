using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public class WirelessSettingsViewModel : DeviceSettingsViewModel
{
    private readonly WirelessSettings _tempBackingData = new();

    public WirelessSettingsViewModel()
    {
        QuickDisconnectButtonCombo = new ButtonComboViewModel(_tempBackingData.QuickDisconnectCombo, 0);
    }

    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.WirelessSettings;

    public bool IsWirelessIdleDisconnectEnabled
    {
        get => _tempBackingData.IsWirelessIdleDisconnectEnabled;
        set
        {
            _tempBackingData.IsWirelessIdleDisconnectEnabled = value;
            OnPropertyChanged();
        }
    }

    public int WirelessIdleDisconnectTime
    {
        get => _tempBackingData.WirelessIdleDisconnectTime / 60000;
        set
        {
            _tempBackingData.WirelessIdleDisconnectTime = value * 60000;
            OnPropertyChanged();
        }
    }

    public ButtonComboViewModel QuickDisconnectButtonCombo { get; set; }

    public override void NotifyAllPropertiesHaveChanged()
    {
        base.NotifyAllPropertiesHaveChanged();
        QuickDisconnectButtonCombo.NotifyAllPropertiesChanged();
    }
}