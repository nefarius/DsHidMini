using System.Text.Json.Serialization;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;

/// <summary>
///     DsHidMini driver settings for a specific device (or Global). Property names match the native parser in
///     <c>driver/Configuration.c</c>.
/// </summary>
public class DshmDeviceSettings
{
    public HidDeviceMode? HidDeviceMode { get; set; }
    public bool? AutoRestartOnHidModeMismatch { get; set; }

    public DevicePairingMode? DevicePairingMode { get; set; }
    public bool? PairOnHotReload { get; set; }
    public string? CustomPairingAddress { get; set; }
    public bool? DisableWirelessIdleTimeout { get; set; }
    public bool? IsOutputRateControlEnabled { get; set; }
    public byte? OutputRateControlPeriodMs { get; set; }
    public int? WirelessIdleTimeoutPeriodMs { get; set; }
    public ButtonCombo QuickDisconnectCombo { get; set; } = new();

    [JsonIgnore]
    public DshmHidModeSettings ContextSettings { get; set; } = new();

    /// <summary>
    ///     Mode blocks present in the source JSON that were not the active <see cref="HidDeviceMode" />.
    ///     Used only during import; never serialized.
    /// </summary>
    [JsonIgnore]
    public List<string> UnusedModeBlocks { get; } = new();

    public DshmHidModeSettings? SDF => HidDeviceMode == Enums.HidDeviceMode.SDF ? ContextSettings : null;
    public DshmHidModeSettings? GPJ => HidDeviceMode == Enums.HidDeviceMode.GPJ ? ContextSettings : null;
    public DshmHidModeSettings? SXS => HidDeviceMode == Enums.HidDeviceMode.SXS ? ContextSettings : null;
    public DshmHidModeSettings? DS4Windows => HidDeviceMode == Enums.HidDeviceMode.DS4Windows ? ContextSettings : null;
    public DshmHidModeSettings? XInput => HidDeviceMode == Enums.HidDeviceMode.XInput ? ContextSettings : null;

    public class DeadZoneSettings
    {
        public bool? Apply { get; set; }

        public double? PolarValue { get; set; }
    }

    public class HeavyRescaleSettings
    {
        public bool? IsEnabled { get; set; }
        public byte? RescaleMinRange { get; set; }
        public byte? RescaleMaxRange { get; set; }
    }

    public class AlternativeModeSettings
    {
        public bool? IsEnabled { get; set; }
        public byte? RescaleMinRange { get; set; }
        public byte? RescaleMaxRange { get; set; }
        public ForcedRightAdjusts ForcedRight { get; set; } = new();
        public ButtonCombo? ToggleCombo { get; set; } = new();
    }

    public class ButtonCombo
    {
        public bool? IsEnabled { get; set; }
        public int? HoldTime { get; set; }
        public int? Button1 { get; set; }
        public int? Button2 { get; set; }
        public int? Button3 { get; set; }
    }

    public class ForcedRightAdjusts
    {
        public bool? IsHeavyThresholdEnabled { get; set; }
        public byte? HeavyThreshold { get; set; }
        public bool? IsLightThresholdEnabled { get; set; }
        public byte? LightThreshold { get; set; }
    }

    public class AllRumbleSettings
    {
        public bool? DisableLeft { get; set; }
        public bool? DisableRight { get; set; }
        public HeavyRescaleSettings HeavyRescale { get; set; } = new();
        public AlternativeModeSettings AlternativeMode { get; set; } = new();
    }

    public class SingleLEDCustoms
    {
        public byte? TotalDuration { get; set; }
        public ushort? BasePortionDuration { get; set; }
        public byte? OffPortionMultiplier { get; set; }
        public byte? OnPortionMultiplier { get; set; }
    }

    public class AllLEDSettings
    {
        public LEDsMode? Mode { get; set; }
        public DSHM_LEDsAuthority? Authority { get; set; }
        public LEDsCustoms CustomPatterns { get; set; } = new();
    }

    public class LEDsCustoms
    {
        public byte? LEDFlags { get; set; }
        public SingleLEDCustoms Player1 { get; set; } = new();
        public SingleLEDCustoms Player2 { get; set; } = new();
        public SingleLEDCustoms Player3 { get; set; } = new();
        public SingleLEDCustoms Player4 { get; set; } = new();
    }

    public class AxesFlipping
    {
        public bool? LeftX { get; set; }
        public bool? LeftY { get; set; }
        public bool? RightX { get; set; }
        public bool? RightY { get; set; }
    }
}

/// <summary>
///     DsHidMini driver settings related only to a given HID device mode.
/// </summary>
public class DshmHidModeSettings
{
    [JsonIgnore]
    public HidDeviceMode? HidDeviceMode { get; set; }

    public PressureMode? PressureExposureMode { get; set; }
    public DPadExposureMode? DPadExposureMode { get; set; }
    public DshmDeviceSettings.DeadZoneSettings DeadZoneLeft { get; set; } = new();
    public DshmDeviceSettings.DeadZoneSettings DeadZoneRight { get; set; } = new();
    public DshmDeviceSettings.AllRumbleSettings RumbleSettings { get; set; } = new();
    public DshmDeviceSettings.AllLEDSettings LEDSettings { get; set; } = new();
    public DshmDeviceSettings.AxesFlipping FlipAxis { get; set; } = new();
}

/// <summary>
///     A class representing the DsHidMini configuration disk file.
/// </summary>
public class DshmConfiguration
{
    public DshmDeviceSettings Global { get; set; } = new();
    public List<DshmDeviceData> Devices { get; set; } = new();

    /// <summary>
    ///     Updates the DsHidMini configuration file on disk accordingly to this object's settings
    /// </summary>
    public bool ApplyConfiguration(string? directory = null)
    {
        Log.Logger.Debug("Converting DsHidMini configuration object to configuration file.");
        return DshmConfigSerialization.UpdateDsHidMiniConfigFile(this, directory);
    }
}

/// <summary>
///     A DsHidMini specific device entry, containing its MAC address and settings.
/// </summary>
public class DshmDeviceData
{
    public string DeviceAddress { get; set; } = string.Empty;
    public DshmDeviceSettings DeviceSettings { get; set; } = new();
}
