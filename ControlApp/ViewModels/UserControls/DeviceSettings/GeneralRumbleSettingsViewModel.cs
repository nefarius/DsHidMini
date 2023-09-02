using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings
{
    public class GeneralRumbleSettingsViewModel : DeviceSettingsViewModel
    {
        private GeneralRumbleSettings _tempBackingData = new();
        protected override IDeviceSettings _myInterface => _tempBackingData;

        public override SettingsModeGroups Group { get; } = SettingsModeGroups.RumbleGeneral;

        public bool IsAltModeEnabled
        {
            get => _tempBackingData.IsAltRumbleModeEnabled;
            set
            {
                _tempBackingData.IsAltRumbleModeEnabled = value;
                this.OnPropertyChanged(nameof(IsAltModeEnabled));
            }
        }

        public bool IsAltModeToggleButtonComboEnabled
        {
            get => _tempBackingData.AltModeToggleButtonCombo.IsEnabled;
            set
            {
                _tempBackingData.AltModeToggleButtonCombo.IsEnabled = value;
                this.OnPropertyChanged(nameof(IsAltModeToggleButtonComboEnabled));
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

        public Button AltModeToggleButttonCombo_Button1
        {
            get => _tempBackingData.AltModeToggleButtonCombo.Button1;
            set
            {
                _tempBackingData.AltModeToggleButtonCombo.Button1 = value;
                this.OnPropertyChanged(nameof(AltModeToggleButttonCombo_Button1));
            }
        }

        public Button AltModeToggleButttonCombo_Button2
        {
            get => _tempBackingData.AltModeToggleButtonCombo.Button2;
            set
            {
                _tempBackingData.AltModeToggleButtonCombo.Button2 = value;
                this.OnPropertyChanged(nameof(AltModeToggleButttonCombo_Button2));
            }
        }
        public Button AltModeToggleButttonCombo_Button3
        {
            get => _tempBackingData.AltModeToggleButtonCombo.Button3;
            set
            {
                _tempBackingData.AltModeToggleButtonCombo.Button3 = value;
                this.OnPropertyChanged(nameof(AltModeToggleButttonCombo_Button3));
            }
        }

        public int AltModeToggleComboHoldTime
        {
            get => _tempBackingData.AltModeToggleButtonCombo.HoldTime;
            set
            {
                _tempBackingData.AltModeToggleButtonCombo.HoldTime = value;
                this.OnPropertyChanged(nameof(AltModeToggleComboHoldTime));
            }
        }

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

        public GeneralRumbleSettingsViewModel() : base()
        {
        }
    }


}
