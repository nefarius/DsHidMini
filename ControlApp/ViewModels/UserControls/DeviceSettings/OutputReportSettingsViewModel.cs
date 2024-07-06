using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings
{
    public class OutputReportSettingsViewModel : DeviceSettingsViewModel
    {
        private OutputReportSettings _tempBackingData = new();
        protected override DeviceSubSettings _mySubSetting => _tempBackingData;
        public override SettingsModeGroups Group { get; } = SettingsModeGroups.OutputReportControl;
        public bool IsOutputReportRateControlEnabled
        {
            get => _tempBackingData.IsOutputReportRateControlEnabled;
            set
            {
                _tempBackingData.IsOutputReportRateControlEnabled = value;
                this.OnPropertyChanged(nameof(IsOutputReportRateControlEnabled));
            }
        }

        public int MaxOutputRate
        {
            get => _tempBackingData.MaxOutputRate;
            set
            {
                _tempBackingData.MaxOutputRate = value;
                this.OnPropertyChanged(nameof(MaxOutputRate));
            }
        }

        public bool IsOutputReportDeduplicatorEnabled
        {
            get => _tempBackingData.IsOutputReportDeduplicatorEnabled;
            set
            {
                _tempBackingData.IsOutputReportDeduplicatorEnabled = value;
                this.OnPropertyChanged(nameof(IsOutputReportDeduplicatorEnabled));
            }
        }

        public OutputReportSettingsViewModel() : base()
        {
        }

        //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
        //{
        //    BackingData_OutRepControl.CopySettings(dataContainerSource.OutputReport, _tempBackingData);
        //}

        //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
        //{
        //    BackingData_OutRepControl.CopySettings(_tempBackingData, dataContainerSource.OutputReport);
        //    NotifyAllPropertiesHaveChanged();
        //}
    }


}
