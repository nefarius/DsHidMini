using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings
{
    public class GeneralRumbleSettingsViewModel : DeviceSettingsViewModel
    {
        private GeneralRumbleSettings _tempBackingData = new();
        protected override IDeviceSettings _myInterface => _tempBackingData;

        public override SettingsModeGroups Group { get; } = SettingsModeGroups.RumbleGeneral;

        public bool IsLeftMotorDisabled
        {
            get => _tempBackingData.IsLeftMotorDisabled;

            set
            {
                _tempBackingData.IsLeftMotorDisabled = value;
                this.OnPropertyChanged(nameof(IsLeftMotorDisabled));
            }
        }
        public bool IsRightMotorDisabled
        {
            get => _tempBackingData.IsRightMotorDisabled;

            set
            {
                _tempBackingData.IsRightMotorDisabled = value;
                this.OnPropertyChanged(nameof(IsRightMotorDisabled));
            }
        }

        public bool IsAltModeEnabled
        {
            get => _tempBackingData.IsAltRumbleModeEnabled;
            set
            {
                _tempBackingData.IsAltRumbleModeEnabled = value;
                this.OnPropertyChanged(nameof(IsAltModeEnabled));
            }
        }

        public bool AlwaysStartInNormalRumbleMode
        {
            get => _tempBackingData.AlwaysStartInNormalMode;
            set
            {
                _tempBackingData.AlwaysStartInNormalMode = value;
                this.OnPropertyChanged(nameof(AlwaysStartInNormalRumbleMode));
            }
        }

        public ButtonComboViewModel AltModeToggleButtonCombo { get; set; }

        public GeneralRumbleSettingsViewModel() : base()
        {
            AltModeToggleButtonCombo = new(_tempBackingData.AltModeToggleButtonCombo, 1);
        }

        public override void NotifyAllPropertiesHaveChanged()
        {
            base.NotifyAllPropertiesHaveChanged();
            AltModeToggleButtonCombo.NotifyAllPropertiesChanged();
        }
    }


}
