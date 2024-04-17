using System.Text.Json.Serialization;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;
using LEDsMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.LEDsMode;
using PressureMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.PressureMode;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager
{
    public class ButtonsCombo
    {
        private int holdTime;
     
        public bool IsEnabled { get; set; }
        public int HoldTime
        {
            get => holdTime;
            set => holdTime = (value >= 0) ? value : 0;
        }

        public Button Button1 { get; set; }
        public Button Button2 { get; set; }
        public Button Button3 { get; set; }

        public ButtonsCombo() { }

        public ButtonsCombo(ButtonsCombo comboToCopy)
        {
            copyCombo(comboToCopy);
        }

        public bool IsComboValid()
        {
            if (Button1 != Button2 && Button1 != Button3 && Button2 != Button3)
                return true;
            else return false;
        }

        public void copyCombo(ButtonsCombo comboToCopy)
        {
            IsEnabled = comboToCopy.IsEnabled;
            HoldTime = comboToCopy.HoldTime;
            Button1 = comboToCopy.Button1;
            Button2 = comboToCopy.Button2;
            Button3 = comboToCopy.Button3;
        }

    }
    
    public class DeviceSettings
    {

        public HidModeSettings HidMode { get; set; } = new();
        public LedsSettings LEDs { get; set; } = new();
        public WirelessSettings Wireless { get; set; } = new();
        public SticksSettings Sticks { get; set; } = new();
        public GeneralRumbleSettings GeneralRumble { get; set; } = new();
        public OutputReportSettings OutputReport { get; set; } = new();
        public LeftMotorRescalingSettings LeftMotorRescaling { get; set; } = new();
        public AltRumbleModeSettings AltRumbleAdjusts { get; set; } = new();

        public DeviceSettings()
        {
            this.ResetToDefault();
        }

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
            HidModeSettings.CopySettings(destiny.HidMode,source.HidMode);
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
        public bool IsLEDsAsXInputSlotEnabled { get; set; } = false;
        public bool PreventRemappingConflictsInSXSMode { get; set; } = false;
        public bool PreventRemappingConflictsInDS4WMode { get; set; } = false;
        public bool AllowAppsToOverrideLEDsInSXSMode { get; set; } = false;

        public override void ResetToDefault()
        {
            CopySettings(this, new());
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
        public bool AllowExternalLedsControl { get; set; } = false;
        public All4LEDsCustoms LEDsCustoms { get; set; } = new();


        public override void ResetToDefault()
        {
            CopySettings(this, new());
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
                    LED_x_Customs[i] = new(i);
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
                [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
                public int LEDIndex { get; }
                public bool IsLedEnabled { get; set; } = false;
                public bool UseLEDEffects { get; set; } = false;

                public byte Duration { get; set; } = 0x00;
                public int CycleDuration { get; set; } = 0x4000;
                public byte OnPeriodCycles { get; set; } = 0xFF;
                public byte OffPeriodCycles { get; set; } = 0xFF;
                public singleLEDCustoms(int ledIndex)
                {
                    this.LEDIndex = ledIndex;
                    IsLedEnabled = LEDIndex == 0 ? true : false;
                }

                internal void Reset()
                {
                    CopyCustoms(new(LEDIndex));
                }

                public void CopyCustoms(singleLEDCustoms copySource)
                {
                    this.IsLedEnabled = copySource.IsLedEnabled;
                    this.UseLEDEffects = copySource.UseLEDEffects;
                    this.Duration = copySource.Duration;
                    this.CycleDuration = copySource.CycleDuration;
                    this.OnPeriodCycles = copySource.OnPeriodCycles;
                }
            }
        }

    }

    public class WirelessSettings : DeviceSubSettings
    {
        public bool IsWirelessIdleDisconnectEnabled { get; set; } = true;
        public int WirelessIdleDisconnectTime { get; set; } = 5;
        public ButtonsCombo QuickDisconnectCombo { get; set; } = new()
        {
            IsEnabled = true,
            HoldTime = 3,
            Button1 = Button.PS,
            Button2 = Button.R1,
            Button3 = Button.L1,
        };

        public override void ResetToDefault()
        {
            CopySettings(this,new());
        }

        public static void CopySettings(WirelessSettings destiny, WirelessSettings source)
        {
            destiny.IsWirelessIdleDisconnectEnabled = source.IsWirelessIdleDisconnectEnabled;
            destiny.WirelessIdleDisconnectTime = source.WirelessIdleDisconnectTime;
            destiny.QuickDisconnectCombo.copyCombo(source.QuickDisconnectCombo);
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
            public int DeadZone { get; set; } = 0;

            public bool InvertXAxis { get; set; } = false;
            public bool InvertYAxis { get; set; } = false;

            public StickData()
            {
               
            }

            public void Reset()
            {
                CopyStickDataFromOtherStick(new());
            }

            public void CopyStickDataFromOtherStick(StickData copySource)
            {
                this.IsDeadZoneEnabled = copySource.IsDeadZoneEnabled;
                this.DeadZone = copySource.DeadZone;
                this.InvertXAxis = copySource.InvertXAxis;
                this.InvertYAxis = copySource.InvertYAxis;
            }

        }

    }

    public class GeneralRumbleSettings : DeviceSubSettings
    {

        // -------------------------------------------- DEFAULT SETTINGS END

        public bool IsLeftMotorDisabled { get; set; } = false;
        public bool IsRightMotorDisabled { get; set; } = false;

        public bool IsAltRumbleModeEnabled { get; set; } = false;
        public bool AlwaysStartInNormalMode { get; set; } = false;
        public bool IsAltModeToggleButtonComboEnabled { get; set; } = false;
        public ButtonsCombo AltModeToggleButtonCombo { get; set; } = new()
        {
            IsEnabled = false,
            HoldTime = 3,
            Button1 = Button.PS,
            Button2 = Button.Select,
            Button3 = Button.None,
        };

        public override void ResetToDefault()
        {
            CopySettings(this, new());
        }

        public static void CopySettings(GeneralRumbleSettings destiny, GeneralRumbleSettings source)
        {
            destiny.IsAltRumbleModeEnabled = source.IsAltRumbleModeEnabled;
            destiny.IsLeftMotorDisabled = source.IsLeftMotorDisabled;
            destiny.IsRightMotorDisabled = source.IsRightMotorDisabled;
            destiny.AlwaysStartInNormalMode = source.IsAltModeToggleButtonComboEnabled;
            destiny.AltModeToggleButtonCombo.copyCombo(source.AltModeToggleButtonCombo);
        }
    }

    public class OutputReportSettings : DeviceSubSettings
    {
        public bool IsOutputReportRateControlEnabled { get; set; } = true;
        public int MaxOutputRate { get; set; } = 150;
        public bool IsOutputReportDeduplicatorEnabled { get; set; } = false;

        public override void ResetToDefault()
        {
            CopySettings(this, new());
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
            CopySettings(this, new());
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
        public bool IsForcedRightMotorHeavyThreasholdEnabled { get; set; } = false;
        public bool IsForcedRightMotorLightThresholdEnabled { get; set; } = false;

        public int RightRumbleConversionUpperRange { get; set; } = 140;
        public int RightRumbleConversionLowerRange { get; set; } = 1;


        public override void ResetToDefault()
        {
            CopySettings(this, new());
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
}
