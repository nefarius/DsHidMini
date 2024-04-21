using System.Text.Json;
using System.Text.Json.Serialization;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;

using Serilog;

using static Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.DshmDeviceSettings;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig
{

    /// <summary>
    /// DsHidMini driver settings for a specific Device (or global)
    /// </summary>
    public class DshmDeviceSettings
    {
        public HidDeviceMode? HIDDeviceMode { get; set; }// = DSHM_HidDeviceModes.DS4Windows;
        public bool? DisableAutoPairing { get; set; }
        public string? PairingAddress { get; set; }
        public bool? DisableWirelessIdleTimeout { get; set; }// = false;
        public bool? IsOutputRateControlEnabled { get; set; }// = true;
        public byte? OutputRateControlPeriodMs { get; set; }// = 150;
        public bool? IsOutputDeduplicatorEnabled { get; set; }// = false;
        public double? WirelessIdleTimeoutPeriodMs { get; set; }// = 300000;
        public bool? IsQuickDisconnectComboEnabled { get; set; } = true;
        public ButtonCombo QuickDisconnectCombo { get; set; } = new();


        [JsonIgnore]
        public DshmHidModeSettings ContextSettings { get; set; } = new();
        public DshmHidModeSettings? SDF => HIDDeviceMode == HidDeviceMode.SDF ? ContextSettings : null;
        public DshmHidModeSettings? GPJ => HIDDeviceMode == HidDeviceMode.GPJ ? ContextSettings : null;
        public DshmHidModeSettings? SXS => HIDDeviceMode == HidDeviceMode.SXS ? ContextSettings : null;
        public DshmHidModeSettings? DS4Windows => HIDDeviceMode == HidDeviceMode.DS4Windows ? ContextSettings : null;
        public DshmHidModeSettings? XInput => HIDDeviceMode == HidDeviceMode.XInput ? ContextSettings : null;

            public DshmDeviceSettings()
        {

        }

        public class DeadZoneSettings
        {
            public bool? Apply
            {
                get;
                set;
            }
            public byte? PolarValue { get; set; }// = 10.0;

        }

        public class HeavyRescaleSettings
        {
            public bool? IsEnabled { get; set; }// = true;
            public byte? RescaleMinRange { get; set; }// = 64;
            public byte? RescaleMaxRange { get; set; }// = 255;
        }

        public class AlternativeModeSettings
        {
            public bool? IsEnabled { get; set; }// = false;
            public byte? RescaleMinRange { get; set; }// = 1;
            public byte? RescaleMaxRange { get; set; }// = 160;
            public ForcedRightAdjusts ForcedRight { get; set; } = new();
            public ButtonCombo? ToggleSMtoBMConversionCombo { get; set; } = new(); // = DSHM_QuickDisconnectCombo.PS_R1_L1
        }

        public class ButtonCombo
        {
            public bool? IsEnabled { get; set; }
            public double? HoldTime { get; set; }
            public int? Button1 { get; set; }
            public int? Button2 { get; set; }
            public int? Button3 { get; set; }
        }

        public class ForcedRightAdjusts
        {
            public bool? BMThresholdEnabled { get; set; }// = false;
            public byte? BMThresholdValue { get; set; }// = 230;
            public bool? SMThresholdEnabled { get; set; }// = false;
            public byte? SMThresholdValue { get; set; }// = 230;
        }

        public class AllRumbleSettings
        {
            public bool? DisableLeft { get; set; }// = false;
            public bool? DisableRight { get; set; }// = false;
            public HeavyRescaleSettings HeavyRescale { get; set; } = new();
            public AlternativeModeSettings AlternativeMode { get; set; } = new();
        }

        public class SingleLEDCustoms
        {
            public byte? TotalDuration { get; set; }// = 255;
            public ushort? BasePortionDuration { get; set; }// = 255;
            public byte? OffPortionMultiplier { get; set; }// = 0;
            public byte? OnPortionMultiplier { get; set; }// = 255;
        }

        public class AllLEDSettings
        {
            public LEDsMode? Mode { get; set; }// = DSHM_LEDsModes.BatteryIndicatorPlayerIndex;
            public DSHM_LEDsAuthority? Authority { get; set; }
            public LEDsCustoms CustomPatterns { get; set; } = new();
        }

        public class LEDsCustoms
        {
            public byte? LEDFlags { get; set; } // = 0x2;
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
    /// DsHidMini driver settings related only to a given Hid Device Mode
    /// </summary>
    public class DshmHidModeSettings
    {
        [JsonIgnore]
        public HidDeviceMode? HIDDeviceMode { get; set; }
        public PressureMode? PressureExposureMode { get; set; }// = DSHM_PressureModes.Default;
        public DPadExposureMode? DPadExposureMode { get; set; }// = DSHM_DPadExposureModes.Default;
        public DeadZoneSettings DeadZoneLeft { get; set; } = new();
        public DeadZoneSettings DeadZoneRight { get; set; } = new();
        public AllRumbleSettings RumbleSettings { get; set; } = new();
        public AllLEDSettings LEDSettings { get; set; } = new();
        public AxesFlipping FlipAxis { get; set; } = new();
    }

    /// <summary>
    /// A class representing the DsHidMini configuration disk file
    /// </summary>
    public class DshmConfiguration
    {
        public DshmDeviceSettings Global { get; set; } = new();
        public List<DshmDeviceData> Devices { get; set; } = new();

        /// <summary>
        /// Updates the DsHidMini configuration file on disk accordingly to this object's settings
        /// </summary>
        /// <returns>If the update was successfully</returns>
        public bool ApplyConfiguration()
        {
            Log.Logger.Debug("Converting DsHidMini configuration object to configuration file.");
            return DshmConfigSerialization.UpdateDsHidMiniConfigFile(this);

        }
    }

    /// <summary>
    /// class representing a DsHidMini specific device data, containing its MAC address and Settings
    /// </summary>
    public class DshmDeviceData
    {
        public string DeviceAddress { get; set; }
        public DshmDeviceSettings DeviceSettings { get; set; } = new();

    }



}

