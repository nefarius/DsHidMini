using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using DSHM_LEDsAuthority = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums.DSHM_LEDsAuthority;

using static Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.LedsSettings.All4LEDsCustoms;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

public partial class LedsSettingsViewModel : DeviceSettingsViewModel
{
    private readonly LedsSettings _tempBackingData = new();

    [ObservableProperty]
    private int _currentLEDCustomsIndex;

    [ObservableProperty]
    private LED_VM[]? _leds_VM = { new(1), new(2), new(3), new(4) };

    [ObservableProperty]
    private LED_VM? _selectedLED_VM;

    [ObservableProperty]
    private bool _isSingleLedDevice;

    public LedsSettingsViewModel()
    {
        Leds_VM[0].singleLEDCustoms = _tempBackingData.LEDsCustoms.LED_x_Customs[0];
        Leds_VM[1].singleLEDCustoms = _tempBackingData.LEDsCustoms.LED_x_Customs[1];
        Leds_VM[2].singleLEDCustoms = _tempBackingData.LEDsCustoms.LED_x_Customs[2];
        Leds_VM[3].singleLEDCustoms = _tempBackingData.LEDsCustoms.LED_x_Customs[3];
        SelectedLED_VM = Leds_VM[0];
    }

    public override SettingsModeGroups Group { get; } = SettingsModeGroups.LEDsControl;

    public int LEDMode
    {
        get => (int)_tempBackingData.LeDMode;
        set
        {
            _tempBackingData.LeDMode = (LEDsMode)value;
            OnPropertyChanged();
        }
    }

    protected override DeviceSubSettings _mySubSetting => _tempBackingData;

    public DSHM_LEDsAuthority LedAuthority
    {
        get => _tempBackingData.LedAuthority;
        set
        {
            _tempBackingData.LedAuthority = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<LedAuthorityOption> LedAuthorityOptions { get; } =
    [
        new(DSHM_LEDsAuthority.Automatic, "Automatic — games can take over"),
        new(DSHM_LEDsAuthority.Driver, "Driver — keep the configured LEDs"),
        new(DSHM_LEDsAuthority.Application, "Application — only games control the LEDs")
    ];

    partial void OnCurrentLEDCustomsIndexChanged(int value)
    {
        SelectedLED_VM = Leds_VM[value];
    }

    public override void NotifyAllPropertiesHaveChanged()
    {
        base.NotifyAllPropertiesHaveChanged();
        foreach (LED_VM ledVM in Leds_VM)
        {
            ledVM.RaisePropertyChangedForAll();
        }
    }


    //public override void SaveSettingsToBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_LEDs.CopySettings(dataContainerSource.LEDs, _tempBackingData);
    //}

    //public override void LoadSettingsFromBackingDataContainer(BackingDataContainer dataContainerSource)
    //{
    //    BackingData_LEDs.CopySettings(_tempBackingData, dataContainerSource.LEDs);
    //    NotifyAllPropertiesHaveChanged();
    //}


    public partial class LED_VM : ObservableObject
    {
        [ObservableProperty]
        private int _ledOrder;

        public singleLEDCustoms singleLEDCustoms;

        public LED_VM(int ledOrder)
        {
            _ledOrder = ledOrder;
        }

        public bool IsEnabled
        {
            get => singleLEDCustoms.IsLedEnabled;
            set
            {
                singleLEDCustoms.IsLedEnabled = value;
                OnPropertyChanged();
            }
        }

        public byte Duration
        {
            get => singleLEDCustoms.Duration;
            set
            {
                singleLEDCustoms.Duration = value;
                OnPropertyChanged();
            }
        }

        public int IntervalDuration
        {
            get => singleLEDCustoms.CycleDuration;
            set
            {
                singleLEDCustoms.CycleDuration = value;

                OnPropertyChanged();
            }
        }

        public int IntervalPortionON
        {
            get => singleLEDCustoms.OnPeriodCycles;
            set
            {
                singleLEDCustoms.OnPeriodCycles = (byte)value;
                OnPropertyChanged();
            }
        }

        public int IntervalPortionOFF
        {
            get => singleLEDCustoms.OffPeriodCycles;
            set
            {
                singleLEDCustoms.OffPeriodCycles = (byte)value;
                OnPropertyChanged();
            }
        }

        public void RaisePropertyChangedForAll()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}

public sealed class LedAuthorityOption(DSHM_LEDsAuthority authority, string label)
{
    public DSHM_LEDsAuthority Authority { get; } = authority;

    public string Label { get; } = label;
}