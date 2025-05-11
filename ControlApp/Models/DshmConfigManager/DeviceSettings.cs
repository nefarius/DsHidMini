using System.Text.Json.Serialization;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;
using LEDsMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.LEDsMode;
using PressureMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.PressureMode;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

public class ButtonsCombo
{
    private int _holdTime;

    public ButtonsCombo() { }

    public ButtonsCombo(ButtonsCombo comboToCopy)
    {
        CopyCombo(comboToCopy);
    }

    public bool IsEnabled { get; set; }

    public int HoldTime
    {
        get => _holdTime;
        set => _holdTime = value >= 0 ? value : 0;
    }

    public Button[] ButtonCombo { get; init; } = new Button[3];

    public void CopyCombo(ButtonsCombo comboToCopy)
    {
        IsEnabled = comboToCopy.IsEnabled;
        HoldTime = comboToCopy.HoldTime;
        for (int i = 0; i < ButtonCombo.Length; i++)
        {
            ButtonCombo[i] = comboToCopy.ButtonCombo[i];
        }
    }
}

public class DeviceSettings
{
    public DeviceSettings()
    {
        ResetToDefault();
    }

    public HidModeSettings HidMode { get; set; } = new();
    public LedsSettings LEDs { get; set; } = new();
    public WirelessSettings Wireless { get; set; } = new();
    public SticksSettings Sticks { get; set; } = new();
    public GeneralRumbleSettings GeneralRumble { get; set; } = new();
    public OutputReportSettings OutputReport { get; set; } = new();
    public LeftMotorRescalingSettings LeftMotorRescaling { get; set; } = new();
    public AltRumbleModeSettings AltRumbleAdjusts { get; set; } = new();

    public void ResetToDefault()
    {
        HidMode.ResetToDefault();
        LEDs.ResetToDefault();
        Wireless.ResetToDefault();
        Sticks.ResetToDefault();
        GeneralRumble.ResetToDefault();
        OutputReport.ResetToDefault();
        LeftMotorRescaling.ResetToDefault();
        AltRumbleAdjusts.ResetToDefault();
    }

    public static void CopySettings(DeviceSettings destiny, DeviceSettings source)
    {
        HidModeSettings.CopySettings(destiny.HidMode, source.HidMode);
        LedsSettings.CopySettings(destiny.LEDs, source.LEDs);
        WirelessSettings.CopySettings(destiny.Wireless, source.Wireless);
        SticksSettings.CopySettings(destiny.Sticks, source.Sticks);
        GeneralRumbleSettings.CopySettings(destiny.GeneralRumble, source.GeneralRumble);
        OutputReportSettings.CopySettings(destiny.OutputReport, source.OutputReport);
        LeftMotorRescalingSettings.CopySettings(destiny.LeftMotorRescaling, source.LeftMotorRescaling);
        AltRumbleModeSettings.CopySettings(destiny.AltRumbleAdjusts, source.AltRumbleAdjusts);
    }
}

public abstract class DeviceSubSettings
{
    public abstract void ResetToDefault();
}

public class HidModeSettings : DeviceSubSettings
{
    public SettingsContext SettingsContext { get; set; } = SettingsContext.XInput;
    public PressureMode PressureExposureMode { get; set; } = PressureMode.Default;
    public DPadMode DPadExposureMode { get; set; } = DPadMode.HAT;
    public bool IsLEDsAsXInputSlotEnabled { get; set; }
    public bool PreventRemappingConflictsInSXSMode { get; set; }
    public bool PreventRemappingConflictsInDS4WMode { get; set; }
    public bool AllowAppsToOverrideLEDsInSXSMode { get; set; }

    public override void ResetToDefault()
    {
        CopySettings(this, new HidModeSettings());
    }

    public static void CopySettings(HidModeSettings destiny, HidModeSettings source)
    {
        destiny.SettingsContext = source.SettingsContext;
        destiny.PressureExposureMode = source.PressureExposureMode;
        destiny.DPadExposureMode = source.DPadExposureMode;
        destiny.IsLEDsAsXInputSlotEnabled = source.IsLEDsAsXInputSlotEnabled;
        destiny.PreventRemappingConflictsInDS4WMode = source.PreventRemappingConflictsInDS4WMode;
        destiny.PreventRemappingConflictsInSXSMode = source.PreventRemappingConflictsInSXSMode;
        destiny.AllowAppsToOverrideLEDsInSXSMode = source.AllowAppsToOverrideLEDsInSXSMode;
    }
}

public class LedsSettings : DeviceSubSettings
{
    public LEDsMode LeDMode { get; set; } = LEDsMode.BatteryIndicatorPlayerIndex;
    public bool AllowExternalLedsControl { get; set; }
    public All4LEDsCustoms LEDsCustoms { get; set; } = new();


    public override void ResetToDefault()
    {
        CopySettings(this, new LedsSettings());
    }

    public static void CopySettings(LedsSettings destiny, LedsSettings source)
    {
        destiny.LeDMode = source.LeDMode;
        destiny.AllowExternalLedsControl = source.AllowExternalLedsControl;
        destiny.LEDsCustoms.CopyLEDsCustoms(source.LEDsCustoms);
    }

    public class All4LEDsCustoms
    {
        public singleLEDCustoms[] LED_x_Customs = new singleLEDCustoms[4];

        public All4LEDsCustoms()
        {
            for (int i = 0; i < LED_x_Customs.Length; i++)
            {
                LED_x_Customs[i] = new singleLEDCustoms(i);
            }
        }

        public void CopyLEDsCustoms(All4LEDsCustoms customsToCopy)
        {
            for (int i = 0; i < LED_x_Customs.Length; i++)
            {
                LED_x_Customs[i].CopyCustoms(customsToCopy.LED_x_Customs[i]);
            }
        }

        public void ResetLEDsCustoms()
        {
            for (int i = 0; i < LED_x_Customs.Length; i++)
            {
                LED_x_Customs[i].Reset();
            }
        }


        public class singleLEDCustoms
        {
            public singleLEDCustoms(int ledIndex)
            {
                LEDIndex = ledIndex;
                IsLedEnabled = LEDIndex == 0 ? true : false;
            }

            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public int LEDIndex { get; }

            public bool IsLedEnabled { get; set; }
            public bool UseLEDEffects { get; set; }

            public byte Duration { get; set; }
            public int CycleDuration { get; set; } = 0x4000;
            public byte OnPeriodCycles { get; set; } = 0xFF;
            public byte OffPeriodCycles { get; set; } = 0xFF;

            internal void Reset()
            {
                CopyCustoms(new singleLEDCustoms(LEDIndex));
            }

            public void CopyCustoms(singleLEDCustoms copySource)
            {
                IsLedEnabled = copySource.IsLedEnabled;
                UseLEDEffects = copySource.UseLEDEffects;
                Duration = copySource.Duration;
                CycleDuration = copySource.CycleDuration;
                OnPeriodCycles = copySource.OnPeriodCycles;
                OffPeriodCycles = copySource.OffPeriodCycles;
            }
        }
    }
}

public class WirelessSettings : DeviceSubSettings
{
    public bool IsWirelessIdleDisconnectEnabled { get; set; } = true;
    public int WirelessIdleDisconnectTime { get; set; } = 300000; // 5 minutes

    public ButtonsCombo QuickDisconnectCombo { get; set; } = new()
    {
        IsEnabled = true, HoldTime = 1000, ButtonCombo = new[] { Button.PS, Button.R1, Button.L1 }
    };

    public override void ResetToDefault()
    {
        CopySettings(this, new WirelessSettings());
    }

    public static void CopySettings(WirelessSettings destiny, WirelessSettings source)
    {
        destiny.IsWirelessIdleDisconnectEnabled = source.IsWirelessIdleDisconnectEnabled;
        destiny.WirelessIdleDisconnectTime = source.WirelessIdleDisconnectTime;
        destiny.QuickDisconnectCombo.CopyCombo(source.QuickDisconnectCombo);
    }
}

public class SticksSettings : DeviceSubSettings
{
    public StickData LeftStickData { get; set; } = new();
    public StickData RightStickData { get; set; } = new();

    public override void ResetToDefault()
    {
        LeftStickData.Reset();
        RightStickData.Reset();
    }

    public static void CopySettings(SticksSettings destiny, SticksSettings source)
    {
        destiny.LeftStickData.CopyStickDataFromOtherStick(source.LeftStickData);
        destiny.RightStickData.CopyStickDataFromOtherStick(source.RightStickData);
    }

    public class StickData
    {
        public bool IsDeadZoneEnabled { get; set; } = true;
        public int DeadZone { get; set; }

        public bool InvertXAxis { get; set; }
        public bool InvertYAxis { get; set; }

        public void Reset()
        {
            CopyStickDataFromOtherStick(new StickData());
        }

        public void CopyStickDataFromOtherStick(StickData copySource)
        {
            IsDeadZoneEnabled = copySource.IsDeadZoneEnabled;
            DeadZone = copySource.DeadZone;
            InvertXAxis = copySource.InvertXAxis;
            InvertYAxis = copySource.InvertYAxis;
        }
    }
}

public class GeneralRumbleSettings : DeviceSubSettings
{
    // -------------------------------------------- DEFAULT SETTINGS END

    public bool IsLeftMotorDisabled { get; set; }
    public bool IsRightMotorDisabled { get; set; }

    public bool IsAltRumbleModeEnabled { get; set; }
    public bool AlwaysStartInNormalMode { get; set; }
    public bool IsAltModeToggleButtonComboEnabled { get; set; } = false;

    public ButtonsCombo AltModeToggleButtonCombo { get; set; } = new()
    {
        IsEnabled = false, HoldTime = 1000, ButtonCombo = new[] { Button.PS, Button.Select, Button.Select }
    };

    public override void ResetToDefault()
    {
        CopySettings(this, new GeneralRumbleSettings());
    }

    public static void CopySettings(GeneralRumbleSettings destiny, GeneralRumbleSettings source)
    {
        destiny.IsAltRumbleModeEnabled = source.IsAltRumbleModeEnabled;
        destiny.IsLeftMotorDisabled = source.IsLeftMotorDisabled;
        destiny.IsRightMotorDisabled = source.IsRightMotorDisabled;
        destiny.AlwaysStartInNormalMode = source.IsAltModeToggleButtonComboEnabled;
        destiny.AltModeToggleButtonCombo.CopyCombo(source.AltModeToggleButtonCombo);
    }
}

public class OutputReportSettings : DeviceSubSettings
{
    public bool IsOutputReportRateControlEnabled { get; set; } = true;
    public int MaxOutputRate { get; set; } = 150;
    public bool IsOutputReportDeduplicatorEnabled { get; set; }

    public override void ResetToDefault()
    {
        CopySettings(this, new OutputReportSettings());
    }

    public static void CopySettings(OutputReportSettings destiny, OutputReportSettings source)
    {
        destiny.IsOutputReportDeduplicatorEnabled = source.IsOutputReportDeduplicatorEnabled;
        destiny.IsOutputReportRateControlEnabled = source.IsOutputReportRateControlEnabled;
        destiny.MaxOutputRate = source.MaxOutputRate;
    }
}

public class LeftMotorRescalingSettings : DeviceSubSettings
{
    public bool IsLeftMotorStrRescalingEnabled { get; set; } = true;
    public int LeftMotorStrRescalingUpperRange { get; set; } = 255;
    public int LeftMotorStrRescalingLowerRange { get; set; } = 64;


    public override void ResetToDefault()
    {
        CopySettings(this, new LeftMotorRescalingSettings());
    }

    public static void CopySettings(LeftMotorRescalingSettings destiny, LeftMotorRescalingSettings source)
    {
        destiny.IsLeftMotorStrRescalingEnabled = source.IsLeftMotorStrRescalingEnabled;
        destiny.LeftMotorStrRescalingLowerRange = source.LeftMotorStrRescalingLowerRange;
        destiny.LeftMotorStrRescalingUpperRange = source.LeftMotorStrRescalingUpperRange;
    }
}

public class AltRumbleModeSettings : DeviceSubSettings
{
    public int ForcedRightMotorHeavyThreshold { get; set; } = 230;
    public int ForcedRightMotorLightThreshold { get; set; } = 230;
    public bool IsForcedRightMotorHeavyThreasholdEnabled { get; set; }
    public bool IsForcedRightMotorLightThresholdEnabled { get; set; }

    public int RightRumbleConversionUpperRange { get; set; } = 140;
    public int RightRumbleConversionLowerRange { get; set; } = 1;


    public override void ResetToDefault()
    {
        CopySettings(this, new AltRumbleModeSettings());
    }

    public static void CopySettings(AltRumbleModeSettings destiny, AltRumbleModeSettings source)
    {
        destiny.RightRumbleConversionLowerRange = source.RightRumbleConversionLowerRange;
        destiny.RightRumbleConversionUpperRange = source.RightRumbleConversionUpperRange;
        // Right rumble (light) threshold
        destiny.IsForcedRightMotorLightThresholdEnabled = source.IsForcedRightMotorLightThresholdEnabled;
        destiny.ForcedRightMotorLightThreshold = source.ForcedRightMotorLightThreshold;
        // Left rumble (Heavy) threshold
        destiny.IsForcedRightMotorHeavyThreasholdEnabled = source.IsForcedRightMotorHeavyThreasholdEnabled;
        destiny.ForcedRightMotorHeavyThreshold = source.ForcedRightMotorHeavyThreshold;
    }
}