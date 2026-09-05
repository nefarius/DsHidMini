using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;
using LEDsMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.LEDsMode;
using PressureMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.PressureMode;

using static Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.LedsSettings;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

public class DshmManagerToDriverConversion
{
    public static Dictionary<SettingsContext, HidDeviceMode> HidDeviceMode = new()
    {
        { SettingsContext.Global, DshmConfig.Enums.HidDeviceMode.XInput },
        { SettingsContext.General, DshmConfig.Enums.HidDeviceMode.XInput },
        { SettingsContext.SDF, DshmConfig.Enums.HidDeviceMode.SDF },
        { SettingsContext.GPJ, DshmConfig.Enums.HidDeviceMode.GPJ },
        { SettingsContext.SXS, DshmConfig.Enums.HidDeviceMode.SXS },
        { SettingsContext.DS4W, DshmConfig.Enums.HidDeviceMode.DS4Windows },
        { SettingsContext.XInput, DshmConfig.Enums.HidDeviceMode.XInput }
    };

    public static Dictionary<HidDeviceMode, SettingsContext> HidDeviceModeDriverToManager = new()
    {
        { DshmConfig.Enums.HidDeviceMode.SDF, SettingsContext.SDF },
        { DshmConfig.Enums.HidDeviceMode.GPJ, SettingsContext.GPJ },
        { DshmConfig.Enums.HidDeviceMode.SXS, SettingsContext.SXS },
        { DshmConfig.Enums.HidDeviceMode.DS4Windows, SettingsContext.DS4W },
        { DshmConfig.Enums.HidDeviceMode.XInput, SettingsContext.XInput }
    };

    public static Dictionary<LEDsMode, DshmConfig.Enums.LEDsMode> LedModeManagerToDriver = new()
    {
        { LEDsMode.BatteryIndicatorPlayerIndex, DshmConfig.Enums.LEDsMode.BatteryIndicatorPlayerIndex },
        { LEDsMode.BatteryIndicatorBarGraph, DshmConfig.Enums.LEDsMode.BatteryIndicatorBarGraph },
        { LEDsMode.CustomStatic, DshmConfig.Enums.LEDsMode.CustomPattern },
        { LEDsMode.CustomPattern, DshmConfig.Enums.LEDsMode.CustomPattern }
    };

    public static Dictionary<DshmConfig.Enums.LEDsMode, LEDsMode> LedModeDriverToManager = new()
    {
        { DshmConfig.Enums.LEDsMode.BatteryIndicatorPlayerIndex, LEDsMode.BatteryIndicatorPlayerIndex },
        { DshmConfig.Enums.LEDsMode.BatteryIndicatorBarGraph, LEDsMode.BatteryIndicatorBarGraph },
        { DshmConfig.Enums.LEDsMode.CustomPattern, LEDsMode.CustomPattern }
    };

    public static Dictionary<DPadMode, DPadExposureMode> DPadExposureModeManagerToDriver = new()
    {
        { DPadMode.Default, DPadExposureMode.Default },
        { DPadMode.HAT, DPadExposureMode.HAT },
        { DPadMode.Buttons, DPadExposureMode.IndividualButtons }
    };

    public static Dictionary<DPadExposureMode, DPadMode> DPadExposureModeDriverToManager = new()
    {
        { DPadExposureMode.Default, DPadMode.Default },
        { DPadExposureMode.HAT, DPadMode.HAT },
        { DPadExposureMode.IndividualButtons, DPadMode.Buttons }
    };

    public static Dictionary<PressureMode, DshmConfig.Enums.PressureMode> DsPressureModeManagerToDriver = new()
    {
        { PressureMode.Default, DshmConfig.Enums.PressureMode.Default },
        { PressureMode.Analogue, DshmConfig.Enums.PressureMode.Analogue },
        { PressureMode.Digital, DshmConfig.Enums.PressureMode.Digital }
    };

    public static Dictionary<DshmConfig.Enums.PressureMode, PressureMode> DsPressureModeDriverToManager = new()
    {
        { DshmConfig.Enums.PressureMode.Default, PressureMode.Default },
        { DshmConfig.Enums.PressureMode.Analogue, PressureMode.Analogue },
        { DshmConfig.Enums.PressureMode.Digital, PressureMode.Digital }
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
        { Button.PS, 16 }
    };

    public static Dictionary<int, Button> ButtonDriverToManager =
        ButtonManagerToDriver.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static Dictionary<BluetoothPairingMode, DevicePairingMode> PairingModeManagerToDriver = new()
    {
        { BluetoothPairingMode.Auto, DevicePairingMode.Auto },
        { BluetoothPairingMode.Custom, DevicePairingMode.Custom },
        { BluetoothPairingMode.Disabled, DevicePairingMode.Disabled }
    };

    public static Dictionary<DevicePairingMode, BluetoothPairingMode> PairingModeDriverToManager = new()
    {
        { DevicePairingMode.Auto, BluetoothPairingMode.Auto },
        { DevicePairingMode.Custom, BluetoothPairingMode.Custom },
        { DevicePairingMode.Disabled, BluetoothPairingMode.Disabled }
    };

    public static void ConvertDeviceSettingsToDriverFormat(DeviceSettings appFormat, DshmDeviceSettings driverFormat)
    {
        HidModeSettings x_HidMode = appFormat.HidMode;
        if (appFormat.HidMode.SettingsContext != SettingsContext.General)
        {
            driverFormat.HidDeviceMode = HidDeviceMode[x_HidMode.SettingsContext];
            driverFormat.ContextSettings.HidDeviceMode = driverFormat.HidDeviceMode;
        }

        driverFormat.ContextSettings.PressureExposureMode =
            x_HidMode.SettingsContext is SettingsContext.SDF or SettingsContext.GPJ
                ? DsPressureModeManagerToDriver[x_HidMode.PressureExposureMode]
                : null;

        driverFormat.ContextSettings.DPadExposureMode =
            x_HidMode.SettingsContext is SettingsContext.SDF or SettingsContext.GPJ
                ? DPadExposureModeManagerToDriver[x_HidMode.DPadExposureMode]
                : null;

        LedsSettings x_Leds = appFormat.LEDs;
        DshmDeviceSettings.AllLEDSettings dshm_AllLEDsSettings = driverFormat.ContextSettings.LEDSettings;

        dshm_AllLEDsSettings.Mode = LedModeManagerToDriver[x_Leds.LeDMode];
        dshm_AllLEDsSettings.Authority =
            x_Leds.AllowExternalLedsControl ? DSHM_LEDsAuthority.Automatic : DSHM_LEDsAuthority.Driver;

        if (x_Leds.LeDMode is LEDsMode.CustomPattern or LEDsMode.CustomStatic)
        {
            ApplyCustomLedPatterns(x_Leds, dshm_AllLEDsSettings);
        }

        WirelessSettings x_Wireless = appFormat.Wireless;
        driverFormat.DisableWirelessIdleTimeout = !x_Wireless.IsWirelessIdleDisconnectEnabled;
        driverFormat.WirelessIdleTimeoutPeriodMs = x_Wireless.WirelessIdleDisconnectTime;
        ApplyButtonCombo(x_Wireless.QuickDisconnectCombo, driverFormat.QuickDisconnectCombo);

        ApplyStickSettings(appFormat.Sticks.LeftStickData, driverFormat.ContextSettings.DeadZoneLeft,
            driverFormat.ContextSettings.FlipAxis, left: true);
        ApplyStickSettings(appFormat.Sticks.RightStickData, driverFormat.ContextSettings.DeadZoneRight,
            driverFormat.ContextSettings.FlipAxis, left: false);

        GeneralRumbleSettings x_RumbleGeneral = appFormat.GeneralRumble;
        DshmDeviceSettings.AllRumbleSettings dshmRumbleSettings = driverFormat.ContextSettings.RumbleSettings;

        dshmRumbleSettings.DisableLeft = x_RumbleGeneral.IsLeftMotorDisabled;
        dshmRumbleSettings.DisableRight = x_RumbleGeneral.IsRightMotorDisabled;
        dshmRumbleSettings.AlternativeMode.IsEnabled = x_RumbleGeneral.AlwaysStartInNormalMode
            ? false
            : x_RumbleGeneral.IsAltRumbleModeEnabled;

        if (x_RumbleGeneral.IsAltRumbleModeEnabled)
        {
            ApplyButtonCombo(x_RumbleGeneral.AltModeToggleButtonCombo,
                dshmRumbleSettings.AlternativeMode.ToggleCombo ??= new DshmDeviceSettings.ButtonCombo());
            dshmRumbleSettings.AlternativeMode.ToggleCombo.IsEnabled =
                x_RumbleGeneral.IsAltModeToggleButtonComboEnabled
                || x_RumbleGeneral.AltModeToggleButtonCombo.IsEnabled;
        }
        else if (dshmRumbleSettings.AlternativeMode.ToggleCombo is not null)
        {
            dshmRumbleSettings.AlternativeMode.ToggleCombo.IsEnabled = false;
        }

        OutputReportSettings x_OutRep = appFormat.OutputReport;
        driverFormat.IsOutputRateControlEnabled = x_OutRep.IsOutputReportRateControlEnabled;
        driverFormat.OutputRateControlPeriodMs = (byte)x_OutRep.MaxOutputRate;

        LeftMotorRescalingSettings x_LeftMRescale = appFormat.LeftMotorRescaling;
        DshmDeviceSettings.HeavyRescaleSettings dshmLeftRumbleRescaleSettings =
            driverFormat.ContextSettings.RumbleSettings.HeavyRescale;
        dshmLeftRumbleRescaleSettings.IsEnabled = x_LeftMRescale.IsLeftMotorStrRescalingEnabled;
        dshmLeftRumbleRescaleSettings.RescaleMinRange = (byte)x_LeftMRescale.LeftMotorStrRescalingLowerRange;
        dshmLeftRumbleRescaleSettings.RescaleMaxRange = (byte)x_LeftMRescale.LeftMotorStrRescalingUpperRange;

        AltRumbleModeSettings x_AltRumbleAdjusts = appFormat.AltRumbleAdjusts;
        DshmDeviceSettings.AlternativeModeSettings dshmSMConversionSettings =
            driverFormat.ContextSettings.RumbleSettings.AlternativeMode;
        DshmDeviceSettings.ForcedRightAdjusts dshmForcedSMSettings =
            driverFormat.ContextSettings.RumbleSettings.AlternativeMode.ForcedRight;

        dshmSMConversionSettings.RescaleMinRange = (byte)x_AltRumbleAdjusts.RightRumbleConversionLowerRange;
        dshmSMConversionSettings.RescaleMaxRange = (byte)x_AltRumbleAdjusts.RightRumbleConversionUpperRange;
        dshmForcedSMSettings.IsLightThresholdEnabled = x_AltRumbleAdjusts.IsForcedRightMotorLightThresholdEnabled;
        dshmForcedSMSettings.LightThreshold = (byte)x_AltRumbleAdjusts.ForcedRightMotorLightThreshold;
        dshmForcedSMSettings.IsHeavyThresholdEnabled = x_AltRumbleAdjusts.IsForcedRightMotorHeavyThresholdEnabled;
        dshmForcedSMSettings.HeavyThreshold = (byte)x_AltRumbleAdjusts.ForcedRightMotorHeavyThreshold;

        if (appFormat.HidMode.SettingsContext == SettingsContext.DS4W &&
            appFormat.HidMode.PreventRemappingConflictsInDS4WMode)
        {
            driverFormat.ContextSettings.DeadZoneLeft.Apply = false;
            driverFormat.ContextSettings.DeadZoneRight.Apply = false;
        }
    }

    public static void ConvertDriverFormatToDeviceSettings(DshmDeviceSettings driverFormat, DeviceSettings appFormat)
    {
        appFormat.ResetToDefault();

        if (driverFormat.HidDeviceMode is { } hidMode &&
            HidDeviceModeDriverToManager.TryGetValue(hidMode, out SettingsContext context))
        {
            appFormat.HidMode.SettingsContext = context;
        }

        if (driverFormat.ContextSettings.PressureExposureMode is { } pressure &&
            DsPressureModeDriverToManager.TryGetValue(pressure, out PressureMode appPressure))
        {
            appFormat.HidMode.PressureExposureMode = appPressure;
        }

        if (driverFormat.ContextSettings.DPadExposureMode is { } dpad &&
            DPadExposureModeDriverToManager.TryGetValue(dpad, out DPadMode appDpad))
        {
            appFormat.HidMode.DPadExposureMode = appDpad;
        }

        if (driverFormat.ContextSettings.LEDSettings.Mode is { } ledMode &&
            LedModeDriverToManager.TryGetValue(ledMode, out LEDsMode appLedMode))
        {
            appFormat.LEDs.LeDMode = appLedMode;
        }

        if (driverFormat.ContextSettings.LEDSettings.Authority is { } authority)
        {
            appFormat.LEDs.AllowExternalLedsControl = authority != DSHM_LEDsAuthority.Driver;
        }

        ReadCustomLedPatterns(driverFormat.ContextSettings.LEDSettings, appFormat.LEDs);

        if (driverFormat.DisableWirelessIdleTimeout is { } disableIdle)
        {
            appFormat.Wireless.IsWirelessIdleDisconnectEnabled = !disableIdle;
        }

        if (driverFormat.WirelessIdleTimeoutPeriodMs is { } idleMs)
        {
            appFormat.Wireless.WirelessIdleDisconnectTime = idleMs;
        }

        ReadButtonCombo(driverFormat.QuickDisconnectCombo, appFormat.Wireless.QuickDisconnectCombo);

        ReadStickSettings(driverFormat.ContextSettings.DeadZoneLeft, driverFormat.ContextSettings.FlipAxis,
            appFormat.Sticks.LeftStickData, left: true);
        ReadStickSettings(driverFormat.ContextSettings.DeadZoneRight, driverFormat.ContextSettings.FlipAxis,
            appFormat.Sticks.RightStickData, left: false);

        DshmDeviceSettings.AllRumbleSettings rumble = driverFormat.ContextSettings.RumbleSettings;
        if (rumble.DisableLeft is { } disableLeft)
        {
            appFormat.GeneralRumble.IsLeftMotorDisabled = disableLeft;
        }

        if (rumble.DisableRight is { } disableRight)
        {
            appFormat.GeneralRumble.IsRightMotorDisabled = disableRight;
        }

        bool toggleEnabled = rumble.AlternativeMode.ToggleCombo?.IsEnabled == true;
        bool altEnabled = rumble.AlternativeMode.IsEnabled == true;
        appFormat.GeneralRumble.IsAltRumbleModeEnabled = altEnabled || toggleEnabled;
        appFormat.GeneralRumble.AlwaysStartInNormalMode = toggleEnabled && !altEnabled;
        appFormat.GeneralRumble.IsAltModeToggleButtonComboEnabled = toggleEnabled;
        if (rumble.AlternativeMode.ToggleCombo is { } toggleCombo)
        {
            ReadButtonCombo(toggleCombo, appFormat.GeneralRumble.AltModeToggleButtonCombo);
        }

        if (driverFormat.IsOutputRateControlEnabled is { } rateEnabled)
        {
            appFormat.OutputReport.IsOutputReportRateControlEnabled = rateEnabled;
        }

        if (driverFormat.OutputRateControlPeriodMs is { } rateMs)
        {
            appFormat.OutputReport.MaxOutputRate = rateMs;
        }

        if (rumble.HeavyRescale.IsEnabled is { } heavyEnabled)
        {
            appFormat.LeftMotorRescaling.IsLeftMotorStrRescalingEnabled = heavyEnabled;
        }

        if (rumble.HeavyRescale.RescaleMinRange is { } heavyMin)
        {
            appFormat.LeftMotorRescaling.LeftMotorStrRescalingLowerRange = heavyMin;
        }

        if (rumble.HeavyRescale.RescaleMaxRange is { } heavyMax)
        {
            appFormat.LeftMotorRescaling.LeftMotorStrRescalingUpperRange = heavyMax;
        }

        if (rumble.AlternativeMode.RescaleMinRange is { } altMin)
        {
            appFormat.AltRumbleAdjusts.RightRumbleConversionLowerRange = altMin;
        }

        if (rumble.AlternativeMode.RescaleMaxRange is { } altMax)
        {
            appFormat.AltRumbleAdjusts.RightRumbleConversionUpperRange = altMax;
        }

        DshmDeviceSettings.ForcedRightAdjusts forced = rumble.AlternativeMode.ForcedRight;
        if (forced.IsLightThresholdEnabled is { } lightEnabled)
        {
            appFormat.AltRumbleAdjusts.IsForcedRightMotorLightThresholdEnabled = lightEnabled;
        }

        if (forced.LightThreshold is { } lightThreshold)
        {
            appFormat.AltRumbleAdjusts.ForcedRightMotorLightThreshold = lightThreshold;
        }

        if (forced.IsHeavyThresholdEnabled is { } heavyThresholdEnabled)
        {
            appFormat.AltRumbleAdjusts.IsForcedRightMotorHeavyThresholdEnabled = heavyThresholdEnabled;
        }

        if (forced.HeavyThreshold is { } heavyThreshold)
        {
            appFormat.AltRumbleAdjusts.ForcedRightMotorHeavyThreshold = heavyThreshold;
        }
    }

    public static DshmDeviceSettings OverlayDeviceSettings(DshmDeviceSettings baseline, DshmDeviceSettings overlay) =>
        MergeDriverSettings(baseline, overlay);

    internal static DshmDeviceSettings MergeDriverSettings(DshmDeviceSettings baseline, DshmDeviceSettings overlay)
    {
        DshmDeviceSettings merged = CloneDriverSettings(baseline);
        merged.HidDeviceMode = overlay.HidDeviceMode ?? merged.HidDeviceMode;
        merged.AutoRestartOnHidModeMismatch =
            overlay.AutoRestartOnHidModeMismatch ?? merged.AutoRestartOnHidModeMismatch;
        merged.DevicePairingMode = overlay.DevicePairingMode ?? merged.DevicePairingMode;
        merged.PairOnHotReload = overlay.PairOnHotReload ?? merged.PairOnHotReload;
        merged.CustomPairingAddress = overlay.CustomPairingAddress ?? merged.CustomPairingAddress;
        merged.DisableWirelessIdleTimeout = overlay.DisableWirelessIdleTimeout ?? merged.DisableWirelessIdleTimeout;
        merged.IsOutputRateControlEnabled = overlay.IsOutputRateControlEnabled ?? merged.IsOutputRateControlEnabled;
        merged.OutputRateControlPeriodMs = overlay.OutputRateControlPeriodMs ?? merged.OutputRateControlPeriodMs;
        merged.WirelessIdleTimeoutPeriodMs = overlay.WirelessIdleTimeoutPeriodMs ?? merged.WirelessIdleTimeoutPeriodMs;
        OverlayButtonCombo(merged.QuickDisconnectCombo, overlay.QuickDisconnectCombo);

        if (overlay.HidDeviceMode is not null || overlay.ContextSettings.HidDeviceMode is not null
                                              || overlay.UnusedModeBlocks.Count == 0 && HasModeContent(overlay.ContextSettings))
        {
            if (HasModeContent(overlay.ContextSettings) || overlay.HidDeviceMode is not null)
            {
                merged.ContextSettings = CloneHidModeSettings(overlay.ContextSettings);
                merged.ContextSettings.HidDeviceMode = overlay.HidDeviceMode ?? merged.HidDeviceMode;
            }
        }

        merged.UnusedModeBlocks.Clear();
        merged.UnusedModeBlocks.AddRange(overlay.UnusedModeBlocks);
        return merged;
    }

    internal static bool HasDeviceSpecificSettings(DshmDeviceSettings settings)
    {
        return settings.HidDeviceMode is not null || HasModeContent(settings.ContextSettings);
    }

    private static bool HasModeContent(DshmHidModeSettings settings)
    {
        return settings.PressureExposureMode is not null
               || settings.DPadExposureMode is not null
               || settings.DeadZoneLeft.Apply is not null
               || settings.DeadZoneLeft.PolarValue is not null
               || settings.DeadZoneRight.Apply is not null
               || settings.DeadZoneRight.PolarValue is not null
               || settings.RumbleSettings.DisableLeft is not null
               || settings.RumbleSettings.DisableRight is not null
               || settings.RumbleSettings.HeavyRescale.IsEnabled is not null
               || settings.LEDSettings.Mode is not null
               || settings.FlipAxis.LeftX is not null
               || settings.FlipAxis.LeftY is not null
               || settings.FlipAxis.RightX is not null
               || settings.FlipAxis.RightY is not null;
    }

    private static void ApplyCustomLedPatterns(LedsSettings x_Leds,
        DshmDeviceSettings.AllLEDSettings dshm_AllLEDsSettings)
    {
        DshmDeviceSettings.LEDsCustoms dshm_Customs = dshm_AllLEDsSettings.CustomPatterns;
        DshmDeviceSettings.SingleLEDCustoms[] dshm_singleLED =
        [
            dshm_Customs.Player1, dshm_Customs.Player2, dshm_Customs.Player3, dshm_Customs.Player4
        ];

        dshm_Customs.LEDFlags = 0;
        for (int i = 0; i < x_Leds.LEDsCustoms.LED_x_Customs.Length; i++)
        {
            All4LEDsCustoms.singleLEDCustoms singleLEDCustoms = x_Leds.LEDsCustoms.LED_x_Customs[i];
            if (singleLEDCustoms.IsLedEnabled)
            {
                dshm_Customs.LEDFlags |= (byte)(1 << (1 + i));
                dshm_singleLED[i].TotalDuration = x_Leds.LeDMode == LEDsMode.CustomPattern
                    ? singleLEDCustoms.Duration
                    : (byte)0xFF;
                dshm_singleLED[i].BasePortionDuration = x_Leds.LeDMode == LEDsMode.CustomPattern
                    ? (ushort)singleLEDCustoms.CycleDuration
                    : (ushort)0x01;
                dshm_singleLED[i].OffPortionMultiplier = x_Leds.LeDMode == LEDsMode.CustomPattern
                    ? singleLEDCustoms.OffPeriodCycles
                    : (byte)0x00;
                dshm_singleLED[i].OnPortionMultiplier = x_Leds.LeDMode == LEDsMode.CustomPattern
                    ? singleLEDCustoms.OnPeriodCycles
                    : (byte)0x01;
            }
            else
            {
                dshm_singleLED[i].TotalDuration = 0x00;
                dshm_singleLED[i].BasePortionDuration = 0x00;
                dshm_singleLED[i].OffPortionMultiplier = 0x00;
                dshm_singleLED[i].OnPortionMultiplier = 0x00;
            }
        }

        if (dshm_Customs.LEDFlags == 0)
        {
            dshm_Customs.LEDFlags = 0x20;
        }
    }

    private static void ReadCustomLedPatterns(DshmDeviceSettings.AllLEDSettings leds, LedsSettings appLeds)
    {
        if (leds.Mode != DshmConfig.Enums.LEDsMode.CustomPattern)
        {
            return;
        }

        DshmDeviceSettings.SingleLEDCustoms[] players =
        [
            leds.CustomPatterns.Player1, leds.CustomPatterns.Player2, leds.CustomPatterns.Player3,
            leds.CustomPatterns.Player4
        ];

        byte flags = leds.CustomPatterns.LEDFlags ?? 0;
        for (int i = 0; i < appLeds.LEDsCustoms.LED_x_Customs.Length; i++)
        {
            All4LEDsCustoms.singleLEDCustoms dest = appLeds.LEDsCustoms.LED_x_Customs[i];
            dest.IsLedEnabled = (flags & (1 << (1 + i))) != 0;
            dest.Duration = players[i].TotalDuration ?? dest.Duration;
            dest.CycleDuration = players[i].BasePortionDuration ?? (ushort)dest.CycleDuration;
            dest.OffPeriodCycles = players[i].OffPortionMultiplier ?? dest.OffPeriodCycles;
            dest.OnPeriodCycles = players[i].OnPortionMultiplier ?? dest.OnPeriodCycles;
        }
    }

    private static void ApplyStickSettings(SticksSettings.StickData stick,
        DshmDeviceSettings.DeadZoneSettings deadZone, DshmDeviceSettings.AxesFlipping axesFlipping, bool left)
    {
        deadZone.Apply = stick.IsDeadZoneEnabled;
        deadZone.PolarValue = DshmDeadZoneConversion.ToPolarValue(stick.DeadZone);
        if (left)
        {
            axesFlipping.LeftX = stick.InvertXAxis;
            axesFlipping.LeftY = stick.InvertYAxis;
        }
        else
        {
            axesFlipping.RightX = stick.InvertXAxis;
            axesFlipping.RightY = stick.InvertYAxis;
        }
    }

    private static void ReadStickSettings(DshmDeviceSettings.DeadZoneSettings deadZone,
        DshmDeviceSettings.AxesFlipping axesFlipping, SticksSettings.StickData stick, bool left)
    {
        if (deadZone.Apply is { } apply)
        {
            stick.IsDeadZoneEnabled = apply;
        }

        if (deadZone.PolarValue is { } polar)
        {
            stick.DeadZone = DshmDeadZoneConversion.FromPolarValue(polar);
        }

        if (left)
        {
            stick.InvertXAxis = axesFlipping.LeftX ?? stick.InvertXAxis;
            stick.InvertYAxis = axesFlipping.LeftY ?? stick.InvertYAxis;
        }
        else
        {
            stick.InvertXAxis = axesFlipping.RightX ?? stick.InvertXAxis;
            stick.InvertYAxis = axesFlipping.RightY ?? stick.InvertYAxis;
        }
    }

    private static void ApplyButtonCombo(ButtonsCombo source, DshmDeviceSettings.ButtonCombo destination)
    {
        destination.IsEnabled = source.IsEnabled;
        destination.HoldTime = source.HoldTime;
        destination.Button1 = ButtonManagerToDriver[source.ButtonCombo[0]];
        destination.Button2 = ButtonManagerToDriver[source.ButtonCombo[1]];
        destination.Button3 = ButtonManagerToDriver[source.ButtonCombo[2]];
    }

    private static void ReadButtonCombo(DshmDeviceSettings.ButtonCombo source, ButtonsCombo destination)
    {
        if (source.IsEnabled is { } enabled)
        {
            destination.IsEnabled = enabled;
        }

        if (source.HoldTime is { } holdTime)
        {
            destination.HoldTime = holdTime;
        }

        destination.ButtonCombo[0] = MapButton(source.Button1, destination.ButtonCombo[0]);
        destination.ButtonCombo[1] = MapButton(source.Button2, destination.ButtonCombo[1]);
        destination.ButtonCombo[2] = MapButton(source.Button3, destination.ButtonCombo[2]);
    }

    private static Button MapButton(int? driverButton, Button fallback)
    {
        if (driverButton is { } value && ButtonDriverToManager.TryGetValue(value, out Button mapped))
        {
            return mapped;
        }

        return fallback;
    }

    private static void OverlayButtonCombo(DshmDeviceSettings.ButtonCombo dest,
        DshmDeviceSettings.ButtonCombo overlay)
    {
        dest.IsEnabled = overlay.IsEnabled ?? dest.IsEnabled;
        dest.HoldTime = overlay.HoldTime ?? dest.HoldTime;
        dest.Button1 = overlay.Button1 ?? dest.Button1;
        dest.Button2 = overlay.Button2 ?? dest.Button2;
        dest.Button3 = overlay.Button3 ?? dest.Button3;
    }

    private static DshmDeviceSettings CloneDriverSettings(DshmDeviceSettings source)
    {
        DeviceSettings app = new();
        ConvertDriverFormatToDeviceSettings(source, app);
        DshmDeviceSettings clone = new();
        ConvertDeviceSettingsToDriverFormat(app, clone);
        clone.HidDeviceMode = source.HidDeviceMode;
        clone.AutoRestartOnHidModeMismatch = source.AutoRestartOnHidModeMismatch;
        clone.DevicePairingMode = source.DevicePairingMode;
        clone.PairOnHotReload = source.PairOnHotReload;
        clone.CustomPairingAddress = source.CustomPairingAddress;
        return clone;
    }

    private static DshmHidModeSettings CloneHidModeSettings(DshmHidModeSettings source)
    {
        DshmDeviceSettings wrapper = new() { ContextSettings = source, HidDeviceMode = source.HidDeviceMode };
        DshmDeviceSettings clone = CloneDriverSettings(wrapper);
        return clone.ContextSettings;
    }
}
