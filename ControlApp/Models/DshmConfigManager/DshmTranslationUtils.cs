using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;
using LEDsMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.LEDsMode;
using PressureMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.PressureMode;
using static Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.LedsSettings;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager
{
    public class DshmManagerToDriverConversion
    {
        public static Dictionary<SettingsContext, DshmConfig.Enums.HidDeviceMode> HidDeviceMode = new()
        {
            {SettingsContext.Global , DshmConfig.Enums.HidDeviceMode.XInput},
            {SettingsContext.General , DshmConfig.Enums.HidDeviceMode.XInput},
            {SettingsContext.SDF , DshmConfig.Enums.HidDeviceMode.SDF},
            {SettingsContext.GPJ , DshmConfig.Enums.HidDeviceMode.GPJ},
            {SettingsContext.SXS , DshmConfig.Enums.HidDeviceMode.SXS},
            {SettingsContext.DS4W , DshmConfig.Enums.HidDeviceMode.DS4Windows},
            {SettingsContext.XInput , DshmConfig.Enums.HidDeviceMode.XInput},
        };

        //---------------------------------------------------- LEDsModes

        public static Dictionary<LEDsMode, DshmConfig.Enums.LEDsMode> LedModeManagerToDriver = new()
        {
            { LEDsMode.BatteryIndicatorPlayerIndex, DshmConfig.Enums.LEDsMode.BatteryIndicatorPlayerIndex },
            { LEDsMode.BatteryIndicatorBarGraph, DshmConfig.Enums.LEDsMode.BatteryIndicatorBarGraph },
            { LEDsMode.CustomStatic, DshmConfig.Enums.LEDsMode.CustomPattern },
            { LEDsMode.CustomPattern, DshmConfig.Enums.LEDsMode.CustomPattern },
        };

        //---------------------------------------------------- DPadModes

        public static Dictionary<DPadMode, DPadExposureMode> DPadExposureModeManagerToDriver = new()
        {
            { DPadMode.Default, DPadExposureMode.Default },
            { DPadMode.HAT, DPadExposureMode.HAT },
            { DPadMode.Buttons, DPadExposureMode.IndividualButtons },
        };

        //---------------------------------------------------- PressureModes

        public static Dictionary<PressureMode, DshmConfig.Enums.PressureMode> DsPressureModeManagerToDriver = new()
        {
            { PressureMode.Default, DshmConfig.Enums.PressureMode.Default },
            { PressureMode.Analogue, DshmConfig.Enums.PressureMode.Analogue },
            { PressureMode.Digital, DshmConfig.Enums.PressureMode.Digital },
        };

        public static Dictionary<Button, int> ButtonManagerToDriver = new()
        {
            { Button.Select, 0 },
            { Button.L3, 1 },
            { Button.R3, 2 },
            { Button.Start, 3 },
            { Button.Up, 4 },
            { Button.Right, 5 },
            { Button.Down, 6 },
            { Button.Left, 7 },
            { Button.L2, 8 },
            { Button.R2, 9 },
            { Button.L1, 10 },
            { Button.R1, 11 },
            { Button.Triangle, 12 },
            { Button.Circle, 13 },
            { Button.Cross, 14 },
            { Button.Square, 15 },
            { Button.PS, 16 },
        };
    
        public static void ConvertDeviceSettingsToDriverFormat(DeviceSettings appFormat, DshmDeviceSettings driverFormat)
        {
            ////////////////////////////////////////////////////////////////////////////////
            // HID MODE
            ////////////////////////////////////////////////////////////////////////////////
            var x_HidMode = appFormat.HidMode;
            if (appFormat.HidMode.SettingsContext != SettingsContext.General)
            {
                driverFormat.HIDDeviceMode = DshmManagerToDriverConversion.HidDeviceMode[x_HidMode.SettingsContext];
                driverFormat.ContextSettings.HIDDeviceMode = driverFormat.HIDDeviceMode;
            }

            driverFormat.ContextSettings.PressureExposureMode =
                (x_HidMode.SettingsContext == SettingsContext.SDF
                || x_HidMode.SettingsContext == SettingsContext.GPJ)
                ? DshmManagerToDriverConversion.DsPressureModeManagerToDriver[x_HidMode.PressureExposureMode] : null;

            driverFormat.ContextSettings.DPadExposureMode =
                (x_HidMode.SettingsContext == SettingsContext.SDF
                || x_HidMode.SettingsContext == SettingsContext.GPJ)
                ? DshmManagerToDriverConversion.DPadExposureModeManagerToDriver[x_HidMode.DPadExposureMode] : null;

            ////////////////////////////////////////////////////////////////////////////////
            // LEDS 
            ////////////////////////////////////////////////////////////////////////////////
            var x_Leds = appFormat.LEDs;
            DshmDeviceSettings.AllLEDSettings dshm_AllLEDsSettings = driverFormat.ContextSettings.LEDSettings;

            dshm_AllLEDsSettings.Mode = DshmManagerToDriverConversion.LedModeManagerToDriver[x_Leds.LeDMode];
            dshm_AllLEDsSettings.Authority = x_Leds.AllowExternalLedsControl ? DSHM_LEDsAuthority.Automatic : DSHM_LEDsAuthority.Driver;


            if (x_Leds.LeDMode == LEDsMode.CustomPattern || x_Leds.LeDMode == LEDsMode.CustomStatic)
            {
                var dshm_Customs = dshm_AllLEDsSettings.CustomPatterns;

                var dshm_singleLED = new DshmDeviceSettings.SingleLEDCustoms[]
                { dshm_Customs.Player1, dshm_Customs.Player2,dshm_Customs.Player3,dshm_Customs.Player4, };

                dshm_Customs.LEDFlags = 0;
                for (int i = 0; i < x_Leds.LEDsCustoms.LED_x_Customs.Length; i++)
                {
                    All4LEDsCustoms.singleLEDCustoms singleLEDCustoms = x_Leds.LEDsCustoms.LED_x_Customs[i];

                    if (singleLEDCustoms.IsLedEnabled)
                    {
                        dshm_Customs.LEDFlags |= (byte)(1 << (1 + i));
                        dshm_singleLED[i].TotalDuration = (x_Leds.LeDMode == LEDsMode.CustomPattern) ? singleLEDCustoms.Duration : (byte)0xFF;
                        dshm_singleLED[i].BasePortionDuration = (x_Leds.LeDMode == LEDsMode.CustomPattern) ? (ushort)(singleLEDCustoms.CycleDuration) : (byte)0x01;
                        dshm_singleLED[i].OffPortionMultiplier = (x_Leds.LeDMode == LEDsMode.CustomPattern) ? singleLEDCustoms.OffPeriodCycles : (byte)0x00;
                        dshm_singleLED[i].OnPortionMultiplier = (x_Leds.LeDMode == LEDsMode.CustomPattern) ? singleLEDCustoms.OnPeriodCycles : (byte)0x01;
                    }
                    else
                    {
                        dshm_singleLED[i].TotalDuration = (byte)0x00;
                        dshm_singleLED[i].BasePortionDuration = (byte)0x00;
                        dshm_singleLED[i].OffPortionMultiplier = (byte)0x00;
                        dshm_singleLED[i].OnPortionMultiplier = (byte)0x00;
                    }
                }
                if(dshm_Customs.LEDFlags == 0) dshm_Customs.LEDFlags = (byte)0x20; // Turn off all LEDs with 0x20 if none has been enabled
            }
            ////////////////////////////////////////////////////////////////////////////////
            // WIRELESS
            ////////////////////////////////////////////////////////////////////////////////
            var x_Wireless = appFormat.Wireless;

            driverFormat.DisableWirelessIdleTimeout = !x_Wireless.IsWirelessIdleDisconnectEnabled;
            driverFormat.WirelessIdleTimeoutPeriodMs = x_Wireless.WirelessIdleDisconnectTime;

            if (x_Wireless.QuickDisconnectCombo.IsComboValid())
            {
                driverFormat.QuickDisconnectCombo.IsEnabled = x_Wireless.QuickDisconnectCombo.IsEnabled;
                driverFormat.QuickDisconnectCombo.HoldTime = x_Wireless.QuickDisconnectCombo.HoldTime;
                driverFormat.QuickDisconnectCombo.Button1 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_Wireless.QuickDisconnectCombo.ButtonCombo[0]];
                driverFormat.QuickDisconnectCombo.Button2 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_Wireless.QuickDisconnectCombo.ButtonCombo[1]];
                driverFormat.QuickDisconnectCombo.Button3 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_Wireless.QuickDisconnectCombo.ButtonCombo[2]];
            }
            else
            {
                driverFormat.QuickDisconnectCombo.IsEnabled = false;
            }

            ////////////////////////////////////////////////////////////////////////////////
            // Sticks
            ////////////////////////////////////////////////////////////////////////////////
            var x_Sticks = appFormat.Sticks;
            DshmDeviceSettings.DeadZoneSettings dshmLeftDZSettings = driverFormat.ContextSettings.DeadZoneLeft;
            DshmDeviceSettings.DeadZoneSettings dshmRightDZSettings = driverFormat.ContextSettings.DeadZoneRight;
            DshmDeviceSettings.AxesFlipping axesFlipping = driverFormat.ContextSettings.FlipAxis;

            dshmLeftDZSettings.Apply = x_Sticks.LeftStickData.IsDeadZoneEnabled;
            dshmLeftDZSettings.PolarValue = (byte)(x_Sticks.LeftStickData.DeadZone * 181 / 142);
            axesFlipping.LeftX = x_Sticks.LeftStickData.InvertXAxis;
            axesFlipping.LeftY = x_Sticks.LeftStickData.InvertYAxis;

            dshmRightDZSettings.Apply = x_Sticks.RightStickData.IsDeadZoneEnabled;
            dshmRightDZSettings.PolarValue = (byte)(x_Sticks.RightStickData.DeadZone * 181 / 142);
            axesFlipping.RightX = x_Sticks.RightStickData.InvertXAxis;
            axesFlipping.RightY = x_Sticks.RightStickData.InvertYAxis;

            ////////////////////////////////////////////////////////////////////////////////
            // General Rumble
            ////////////////////////////////////////////////////////////////////////////////
            var x_RumbleGeneral = appFormat.GeneralRumble;
            DshmDeviceSettings.AllRumbleSettings dshmRumbleSettings = driverFormat.ContextSettings.RumbleSettings;

            if(!x_RumbleGeneral.IsAltRumbleModeEnabled)
            {
                dshmRumbleSettings.DisableLeft = x_RumbleGeneral.IsLeftMotorDisabled;
                dshmRumbleSettings.DisableRight = x_RumbleGeneral.IsRightMotorDisabled;
            }

            dshmRumbleSettings.AlternativeMode.IsEnabled = x_RumbleGeneral.AlwaysStartInNormalMode ? false : x_RumbleGeneral.IsAltRumbleModeEnabled;

            // Disable toggle combo if alt Rumble Mode is not supposed to be used
            if (x_RumbleGeneral.AltModeToggleButtonCombo.IsComboValid())
            {
                dshmRumbleSettings.AlternativeMode.ToggleCombo.IsEnabled = x_RumbleGeneral.IsAltRumbleModeEnabled ? x_RumbleGeneral.AltModeToggleButtonCombo.IsEnabled : false;
                dshmRumbleSettings.AlternativeMode.ToggleCombo.HoldTime = x_RumbleGeneral.AltModeToggleButtonCombo.HoldTime;
                dshmRumbleSettings.AlternativeMode.ToggleCombo.Button1 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_RumbleGeneral.AltModeToggleButtonCombo.ButtonCombo[0]];
                dshmRumbleSettings.AlternativeMode.ToggleCombo.Button2 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_RumbleGeneral.AltModeToggleButtonCombo.ButtonCombo[1]];
                dshmRumbleSettings.AlternativeMode.ToggleCombo.Button3 = DshmManagerToDriverConversion.ButtonManagerToDriver[x_RumbleGeneral.AltModeToggleButtonCombo.ButtonCombo[2]];

            }
            else
            {
                dshmRumbleSettings.AlternativeMode.ToggleCombo.IsEnabled = false;
            }

            ////////////////////////////////////////////////////////////////////////////////
            //  Output Report
            ////////////////////////////////////////////////////////////////////////////////
            var x_OutRep = appFormat.OutputReport;

            driverFormat.IsOutputRateControlEnabled = x_OutRep.IsOutputReportRateControlEnabled;
            driverFormat.OutputRateControlPeriodMs = (byte)x_OutRep.MaxOutputRate;
            driverFormat.IsOutputDeduplicatorEnabled = x_OutRep.IsOutputReportDeduplicatorEnabled;

            ////////////////////////////////////////////////////////////////////////////////
            ///  Left Motor Rescaling
            //////////////////////////////////////////////////////////////////////////////// 
            var x_LeftMRescale = appFormat.LeftMotorRescaling;
            DshmDeviceSettings.HeavyRescaleSettings dshmLeftRumbleRescaleSettings = driverFormat.ContextSettings.RumbleSettings.HeavyRescale;
            
            dshmLeftRumbleRescaleSettings.IsEnabled = x_LeftMRescale.IsLeftMotorStrRescalingEnabled;
            dshmLeftRumbleRescaleSettings.RescaleMinRange = (byte)x_LeftMRescale.LeftMotorStrRescalingLowerRange;
            dshmLeftRumbleRescaleSettings.RescaleMaxRange = (byte)x_LeftMRescale.LeftMotorStrRescalingUpperRange;
            
            ////////////////////////////////////////////////////////////////////////////////
            //  Alt Mode Adjuster
            ////////////////////////////////////////////////////////////////////////////////
            var x_AltRumbleAdjusts = appFormat.AltRumbleAdjusts;
            DshmDeviceSettings.AlternativeModeSettings dshmSMConversionSettings = driverFormat.ContextSettings.RumbleSettings.AlternativeMode;
            DshmDeviceSettings.ForcedRightAdjusts dshmForcedSMSettings = driverFormat.ContextSettings.RumbleSettings.AlternativeMode.ForcedRight;

            dshmSMConversionSettings.RescaleMinRange = (byte)x_AltRumbleAdjusts.RightRumbleConversionLowerRange;
            dshmSMConversionSettings.RescaleMaxRange = (byte)x_AltRumbleAdjusts.RightRumbleConversionUpperRange;

            // Right rumble (light) threshold
            dshmForcedSMSettings.IsLightThresholdEnabled = x_AltRumbleAdjusts.IsForcedRightMotorLightThresholdEnabled;
            dshmForcedSMSettings.LightThreshold = (byte)x_AltRumbleAdjusts.ForcedRightMotorLightThreshold;

            // Left rumble (Heavy) threshold
            dshmForcedSMSettings.IsHeavyThresholdEnabled = x_AltRumbleAdjusts.IsForcedRightMotorHeavyThreasholdEnabled;
            dshmForcedSMSettings.HeavyThreshold = (byte)x_AltRumbleAdjusts.ForcedRightMotorHeavyThreshold;

            ////////////////////////////////////////////////////////////////////////////////
            //  Fine tweaking
            ////////////////////////////////////////////////////////////////////////////////
            if (appFormat.HidMode.SettingsContext == Enums.SettingsContext.DS4W)
            {
                if (appFormat.HidMode.PreventRemappingConflictsInDS4WMode)
                {
                    driverFormat.ContextSettings.DeadZoneLeft.Apply = false;
                    driverFormat.ContextSettings.DeadZoneRight.Apply = false;
                }
            }
        }
    }
}
