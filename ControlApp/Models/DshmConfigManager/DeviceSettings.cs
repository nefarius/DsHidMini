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

        private Button button1;
        private Button button2;
        private Button button3;
        public Button Button1
        {
            get => button1;
            set
            {
                if (value != button2 && value != button3)
                    button1 = value;
            }
        }
        public Button Button2
        {
            get => button2;
            set
            {
                if (value != button1 && value != button3)
                    button2 = value;
            }
        }
        public Button Button3
        {
            get => button3;
            set
            {
                if (value != button1 && value != button2)
                    button3 = value;
            }
        }

        public ButtonsCombo() { }

        public ButtonsCombo(ButtonsCombo comboToCopy)
        {
            copyCombo(comboToCopy);
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
    
    public class DeviceSettings : IDeviceSettings
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

        public void CopySettingsFromContainer(DeviceSettings container)
        {
            HidMode.CopySettingsFromContainer(container);
            LEDs.CopySettingsFromContainer(container);
            Wireless.CopySettingsFromContainer(container);
            Sticks.CopySettingsFromContainer(container);
            GeneralRumble.CopySettingsFromContainer(container);
            OutputReport.CopySettingsFromContainer(container);
            LeftMotorRescaling.CopySettingsFromContainer(container);
            AltRumbleAdjusts.CopySettingsFromContainer(container);
        }

        public void CopySettingsToContainer(DeviceSettings container)
        {
            HidMode.CopySettingsToContainer(container);
            LEDs.CopySettingsToContainer(container);
            Wireless.CopySettingsToContainer(container);
            Sticks.CopySettingsToContainer(container);
            GeneralRumble.CopySettingsToContainer(container);
            OutputReport.CopySettingsToContainer(container);
            LeftMotorRescaling.CopySettingsToContainer(container);
            AltRumbleAdjusts.CopySettingsToContainer(container);
        }

        public void ConvertAllToDSHM(DshmDeviceSettings dshm_data)
        {
            HidMode.SaveToDSHMSettings(dshm_data);
            LEDs.SaveToDSHMSettings(dshm_data);
            Wireless.SaveToDSHMSettings(dshm_data);
            Sticks.SaveToDSHMSettings(dshm_data);
            GeneralRumble.SaveToDSHMSettings(dshm_data);
            OutputReport.SaveToDSHMSettings(dshm_data);
            LeftMotorRescaling.SaveToDSHMSettings(dshm_data);
            AltRumbleAdjusts.SaveToDSHMSettings(dshm_data);

            if (HidMode.SettingsContext == Enums.SettingsContext.DS4W)
            {
                dshm_data.ContextSettings.DeadZoneLeft.Apply = false;
                dshm_data.ContextSettings.DeadZoneRight.Apply = false;
            }
        }
    }


    public interface IDeviceSettings
    {
        void ResetToDefault();

        void CopySettingsFromContainer(DeviceSettings container);

        void CopySettingsToContainer(DeviceSettings container);
    }

    public abstract class DeviceSubSettings : IDeviceSettings
    {
        public abstract void ResetToDefault();

        public abstract void CopySettingsFromContainer(DeviceSettings container);

        public abstract void CopySettingsToContainer(DeviceSettings container);

        public abstract void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings);

        
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


        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            if (SettingsContext != SettingsContext.General)
            {
                dshmContextSettings.HIDDeviceMode = DshmManagerToDriverConversion.HidDeviceMode[SettingsContext];
                dshmContextSettings.ContextSettings.HIDDeviceMode = dshmContextSettings.HIDDeviceMode;
            }

            dshmContextSettings.ContextSettings.PressureExposureMode = dshmContextSettings.ContextSettings.PressureExposureMode =
                (this.SettingsContext == SettingsContext.SDF
                || this.SettingsContext == SettingsContext.GPJ)
                ? DshmManagerToDriverConversion.DsPressureModeManagerToDriver[this.PressureExposureMode] : null;

            dshmContextSettings.ContextSettings.DPadExposureMode = dshmContextSettings.ContextSettings.DPadExposureMode =
                (this.SettingsContext == SettingsContext.SDF
                || this.SettingsContext == SettingsContext.GPJ)
                ? DshmManagerToDriverConversion.DPadExposureModeManagerToDriver[this.DPadExposureMode] : null;

            dshmContextSettings.ContextSettings.LEDSettings.Authority = this.PreventRemappingConflictsInDS4WMode ? DSHM_LEDsAuthority.Application : DSHM_LEDsAuthority.Driver;
        }

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.HidMode);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.HidMode, this); 
        }
    }

    public class LedsSettings : DeviceSubSettings
    {
        public LEDsMode LeDMode { get; set; } = LEDsMode.BatteryIndicatorPlayerIndex;
        public bool AllowExternalLedsControl { get; set; } = true;
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

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.LEDs);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.LEDs, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            DshmDeviceSettings.AllLEDSettings dshm_AllLEDsSettings = dshmContextSettings.ContextSettings.LEDSettings;

            dshm_AllLEDsSettings.Mode = DshmManagerToDriverConversion.LedModeManagerToDriver[this.LeDMode];
            dshm_AllLEDsSettings.Authority = AllowExternalLedsControl ? DSHM_LEDsAuthority.Automatic : DSHM_LEDsAuthority.Driver;

            var dshm_Customs = dshm_AllLEDsSettings.CustomPatterns;

            var dshm_singleLED = new DshmDeviceSettings.SingleLEDCustoms[]
            { dshm_Customs.Player1, dshm_Customs.Player2,dshm_Customs.Player3,dshm_Customs.Player4, };


            dshm_Customs.LEDFlags = 0;
            for (int i = 0; i < LEDsCustoms.LED_x_Customs.Length; i++)
            {
                All4LEDsCustoms.singleLEDCustoms singleLEDCustoms = this.LEDsCustoms.LED_x_Customs[i];

                if (singleLEDCustoms.IsLedEnabled)
                {
                    dshm_Customs.LEDFlags |= (byte)(1 << (1 + i));
                }

                if(this.LeDMode == LEDsMode.CustomPattern)
                {
                    dshm_singleLED[i].Duration = singleLEDCustoms.Duration;
                    dshm_singleLED[i].CycleDuration1 = (byte)(singleLEDCustoms.CycleDuration >> 4); // FIX THIS
                    dshm_singleLED[i].CycleDuration0 = (byte)(singleLEDCustoms.CycleDuration & 0xFF);
                    dshm_singleLED[i].OffPeriodCycles = singleLEDCustoms.OffPeriodCycles;
                    dshm_singleLED[i].OnPeriodCycles = singleLEDCustoms.OnPeriodCycles;
                }
                else
                {
                    dshm_singleLED[i] = null;
                }
            }
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
            destiny.QuickDisconnectCombo.copyCombo(source.QuickDisconnectCombo);
            destiny.WirelessIdleDisconnectTime = source.WirelessIdleDisconnectTime;
            destiny.QuickDisconnectCombo = source.QuickDisconnectCombo;
        }

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.Wireless);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.Wireless, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            dshmContextSettings.DisableWirelessIdleTimeout = !this.IsWirelessIdleDisconnectEnabled;
            dshmContextSettings.WirelessIdleTimeoutPeriodMs = this.WirelessIdleDisconnectTime * 60 * 1000;

            dshmContextSettings.QuickDisconnectCombo.IsEnabled = this.QuickDisconnectCombo.IsEnabled;
            dshmContextSettings.QuickDisconnectCombo.HoldTime = this.QuickDisconnectCombo.HoldTime;
            dshmContextSettings.QuickDisconnectCombo.Button1 = DshmManagerToDriverConversion.ButtonManagerToDriver[QuickDisconnectCombo.Button1];
            dshmContextSettings.QuickDisconnectCombo.Button2 = DshmManagerToDriverConversion.ButtonManagerToDriver[QuickDisconnectCombo.Button2];
            dshmContextSettings.QuickDisconnectCombo.Button3 = DshmManagerToDriverConversion.ButtonManagerToDriver[QuickDisconnectCombo.Button3];
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

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.Sticks);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.Sticks, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            DshmDeviceSettings.DeadZoneSettings dshmLeftDZSettings = dshmContextSettings.ContextSettings.DeadZoneLeft;
            DshmDeviceSettings.DeadZoneSettings dshmRightDZSettings = dshmContextSettings.ContextSettings.DeadZoneRight;
            DshmDeviceSettings.AxesFlipping axesFlipping = dshmContextSettings.ContextSettings.FlipAxis;


            dshmLeftDZSettings.Apply = this.LeftStickData.IsDeadZoneEnabled;
            dshmLeftDZSettings.PolarValue = (byte)(this.LeftStickData.DeadZone * 181 / 142);
            axesFlipping.LeftX = this.LeftStickData.InvertXAxis;
            axesFlipping.LeftY = this.LeftStickData.InvertYAxis;

            dshmRightDZSettings.Apply = this.RightStickData.IsDeadZoneEnabled;
            dshmRightDZSettings.PolarValue = (byte)(this.RightStickData.DeadZone * 181 / 142);
            axesFlipping.RightX = this.RightStickData.InvertXAxis;
            axesFlipping.RightY = this.RightStickData.InvertYAxis;
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
            Button2 = Button.SELECT,
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

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.GeneralRumble);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.GeneralRumble, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            DshmDeviceSettings.AllRumbleSettings dshmRumbleSettings = dshmContextSettings.ContextSettings.RumbleSettings;

            if(!IsAltRumbleModeEnabled)
            {
                dshmRumbleSettings.DisableBM = this.IsLeftMotorDisabled;
                dshmRumbleSettings.DisableSM = this.IsLeftMotorDisabled;
            }

            dshmRumbleSettings.SMToBMConversion.Enabled = AlwaysStartInNormalMode ? false : this.IsAltRumbleModeEnabled;

            // Disable toggle combo if alt Rumble Mode is not supposed to be used
            dshmRumbleSettings.SMToBMConversion.ToggleSMtoBMConversionCombo.IsEnabled = IsAltRumbleModeEnabled ? this.AltModeToggleButtonCombo.IsEnabled : false;
            dshmRumbleSettings.SMToBMConversion.ToggleSMtoBMConversionCombo.HoldTime = this.AltModeToggleButtonCombo.HoldTime;
            dshmRumbleSettings.SMToBMConversion.ToggleSMtoBMConversionCombo.Button1 = DshmManagerToDriverConversion.ButtonManagerToDriver[AltModeToggleButtonCombo.Button1];
            dshmRumbleSettings.SMToBMConversion.ToggleSMtoBMConversionCombo.Button2 = DshmManagerToDriverConversion.ButtonManagerToDriver[AltModeToggleButtonCombo.Button2];
            dshmRumbleSettings.SMToBMConversion.ToggleSMtoBMConversionCombo.Button3 = DshmManagerToDriverConversion.ButtonManagerToDriver[AltModeToggleButtonCombo.Button3];
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

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.OutputReport);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.OutputReport, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            dshmContextSettings.IsOutputRateControlEnabled = this.IsOutputReportRateControlEnabled;
            dshmContextSettings.OutputRateControlPeriodMs = (byte)this.MaxOutputRate;
            dshmContextSettings.IsOutputDeduplicatorEnabled = this.IsOutputReportDeduplicatorEnabled;
        }
    }

    public class LeftMotorRescalingSettings : DeviceSubSettings
    {
        private int leftMotorStrRescalingUpperRange = 255;
        private int leftMotorStrRescalingLowerRange = 64;

        public bool IsLeftMotorStrRescalingEnabled { get; set; } = true;
        public int LeftMotorStrRescalingUpperRange
        {
            get => leftMotorStrRescalingUpperRange;
            set
            {
                int tempInt = (value < leftMotorStrRescalingLowerRange) ? leftMotorStrRescalingLowerRange + 1 : value;
                leftMotorStrRescalingUpperRange = tempInt;

            }
        }
        public int LeftMotorStrRescalingLowerRange
        {
            get => leftMotorStrRescalingLowerRange;
            set
            {
                int tempInt = (value > leftMotorStrRescalingUpperRange) ? leftMotorStrRescalingUpperRange - 1 : value;
                leftMotorStrRescalingLowerRange = tempInt;
            }
        }

        public override void ResetToDefault()
        {
            CopySettings(this, new());
        }

        public static void CopySettings(LeftMotorRescalingSettings destiny, LeftMotorRescalingSettings source)
        {
            destiny.IsLeftMotorStrRescalingEnabled = source.IsLeftMotorStrRescalingEnabled;
            destiny.leftMotorStrRescalingLowerRange = source.LeftMotorStrRescalingLowerRange;
            destiny.leftMotorStrRescalingUpperRange = source.LeftMotorStrRescalingUpperRange;
        }

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.LeftMotorRescaling);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.LeftMotorRescaling, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            DshmDeviceSettings.BMStrRescaleSettings dshmLeftRumbleRescaleSettings = dshmContextSettings.ContextSettings.RumbleSettings.BMStrRescale;

            dshmLeftRumbleRescaleSettings.Enabled = this.IsLeftMotorStrRescalingEnabled;
            dshmLeftRumbleRescaleSettings.MinValue = (byte)this.LeftMotorStrRescalingLowerRange;
            dshmLeftRumbleRescaleSettings.MaxValue = (byte)this.LeftMotorStrRescalingUpperRange;
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

        public override void CopySettingsFromContainer(DeviceSettings container)
        {
            CopySettings(this, container.AltRumbleAdjusts);
        }

        public override void CopySettingsToContainer(DeviceSettings container)
        {
            CopySettings(container.AltRumbleAdjusts, this);
        }

        public override void SaveToDSHMSettings(DshmDeviceSettings dshmContextSettings)
        {
            DshmDeviceSettings.SMToBMConversionSettings dshmSMConversionSettings = dshmContextSettings.ContextSettings.RumbleSettings.SMToBMConversion;
            DshmDeviceSettings.ForcedSMSettings dshmForcedSMSettings = dshmContextSettings.ContextSettings.RumbleSettings.ForcedSM;

            // Right rumble conversion rescaling adjustment
            if(RightRumbleConversionLowerRange < RightRumbleConversionUpperRange)
            {
                dshmSMConversionSettings.RescaleMinValue = (byte)this.RightRumbleConversionLowerRange;
                dshmSMConversionSettings.RescaleMaxValue = (byte)this.RightRumbleConversionUpperRange;
            }
            else
            {

            }

            

            // Right rumble (light) threshold
            dshmForcedSMSettings.SMThresholdEnabled = this.IsForcedRightMotorLightThresholdEnabled;
            dshmForcedSMSettings.SMThresholdValue = (byte)this.ForcedRightMotorLightThreshold;

            // Left rumble (Heavy) threshold
            dshmForcedSMSettings.BMThresholdEnabled = this.IsForcedRightMotorHeavyThreasholdEnabled;
            dshmForcedSMSettings.BMThresholdValue = (byte)this.ForcedRightMotorHeavyThreshold;
        }
    }
}
