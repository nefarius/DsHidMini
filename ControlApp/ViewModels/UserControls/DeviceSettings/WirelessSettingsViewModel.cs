using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings
{
    public class WirelessSettingsViewModel : DeviceSettingsViewModel
    {
        private WirelessSettings _tempBackingData = new();
        protected override IDeviceSettings _myInterface => _tempBackingData;

        public override SettingsModeGroups Group { get; } = SettingsModeGroups.WirelessSettings;

        public bool IsWirelessIdleDisconnectEnabled
        {
            get => _tempBackingData.IsWirelessIdleDisconnectEnabled;
            set
            {
                _tempBackingData.IsWirelessIdleDisconnectEnabled = value;
                this.OnPropertyChanged(nameof(IsWirelessIdleDisconnectEnabled));
            }
        }

        public int WirelessIdleDisconnectTime
        {
            get => _tempBackingData.WirelessIdleDisconnectTime;
            set
            {
                _tempBackingData.WirelessIdleDisconnectTime = value;
                this.OnPropertyChanged(nameof(WirelessIdleDisconnectTime));
            }
        }

        public ButtonComboViewModel QuickDisconnectButtonCombo { get; set; }

        public WirelessSettingsViewModel() : base()
        {
            QuickDisconnectButtonCombo = new(_tempBackingData.QuickDisconnectCombo, 0);
        }

        public override void NotifyAllPropertiesHaveChanged()
        {
            base.NotifyAllPropertiesHaveChanged();
            QuickDisconnectButtonCombo.NotifyAllPropertiesChanged();
        }
    }


}
