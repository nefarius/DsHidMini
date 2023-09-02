using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings
{
    public class SticksSettingsViewModel : DeviceSettingsViewModel
    {
        // -------------------------------------------- STICKS DEADZONE GROUP
        private SticksSettings _tempBackingData = new();
        protected override IDeviceSettings _myInterface => _tempBackingData;

        public override SettingsModeGroups Group { get; } = SettingsModeGroups.SticksDeadzone;
        
        public bool ApplyLeftStickDeadZone
        {
            get => _tempBackingData.LeftStickData.IsDeadZoneEnabled;
            set
            {
                _tempBackingData.LeftStickData.IsDeadZoneEnabled = value;
                this.OnPropertyChanged(nameof(ApplyLeftStickDeadZone));
            }
        }

        public bool ApplyRightStickDeadZone
        {
            get => _tempBackingData.RightStickData.IsDeadZoneEnabled;
            set
            {
                _tempBackingData.RightStickData.IsDeadZoneEnabled = value;
                this.OnPropertyChanged(nameof(ApplyRightStickDeadZone));
            }
        }

        public int LeftStickDeadZone
        {
            get => _tempBackingData.LeftStickData.DeadZone;
            set
            {
                _tempBackingData.LeftStickData.DeadZone = value;
                this.OnPropertyChanged(nameof(LeftStickDeadZone));
            }
        }

        public int RightStickDeadZone
        {
            get => _tempBackingData.RightStickData.DeadZone;
            set
            {
                _tempBackingData.RightStickData.DeadZone = value;
                this.OnPropertyChanged(nameof(RightStickDeadZone));
            }
        }

        public bool InvertLSX
        {
            get => _tempBackingData.LeftStickData.InvertXAxis;
            set
            {
                _tempBackingData.LeftStickData.InvertXAxis = value;
                this.OnPropertyChanged(nameof(InvertLSX));
            }
        }

        public bool InvertLSY
        {
            get => _tempBackingData.LeftStickData.InvertYAxis;
            set
            {
                _tempBackingData.LeftStickData.InvertYAxis = value;
                this.OnPropertyChanged(nameof(InvertLSY));
            }
        }

        public bool InvertRSX
        {
            get => _tempBackingData.RightStickData.InvertXAxis;
            set
            {
                _tempBackingData.RightStickData.InvertXAxis = value;
                this.OnPropertyChanged(nameof(InvertRSX));
            }
        }

        public bool InvertRSY
        {
            get => _tempBackingData.RightStickData.InvertYAxis;
            set
            {
                _tempBackingData.RightStickData.InvertYAxis = value;
                this.OnPropertyChanged(nameof(InvertRSY));
            }
        }


        public SticksSettingsViewModel() : base()
        {
        }

        //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
        //{
        //    BackingData_Sticks.CopySettings(dataContainerSource.sticksData, _tempBackingData);
        //}

        //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
        //{
        //    BackingData_Sticks.CopySettings(_tempBackingData, dataContainerSource.sticksData);
        //    NotifyAllPropertiesHaveChanged();
        //}
    }
}
