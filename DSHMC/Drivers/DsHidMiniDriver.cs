using System;
using System.ComponentModel;
using Nefarius.DsHidMini.Util;
using Nefarius.DsHidMini.Util.WPF;

namespace Nefarius.DsHidMini.Drivers
{
    public static class DsHidMiniDriver
    {
        /// <summary>
        ///     Interface GUID common to all devices the DsHidMini driver supports.
        /// </summary>
        public static Guid DeviceInterfaceGuid => Guid.Parse("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");

        #region Read-only properties

        /// <summary>
        ///     Unified Device Property exposing current battery status.
        /// </summary>
        public static DevicePropertyKey BatteryStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 2,
            typeof(byte));

        public static DevicePropertyKey LastPairingStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 3,
            typeof(int));

        #endregion

        #region Boot configuration properties

        /// <summary>
        ///     Unified Device Property exposing current HID device emulation mode.
        /// </summary>
        public static DevicePropertyKey HidDeviceModeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 2,
            typeof(byte));

        public static DevicePropertyKey IsOutputRateControlEnabledProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 3,
            typeof(byte));

        public static DevicePropertyKey OutputRateControlPeriodMsProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 4,
            typeof(byte));

        public static DevicePropertyKey IsOutputDeduplicatorEnabledProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 5,
            typeof(byte));

        public static DevicePropertyKey WirelessIdleTimeoutPeriodMsProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 6,
            typeof(uint));

        #endregion

        #region Hot-reload changeable properties

        /// <summary>
        ///     Unified Device Property exposing if digital button states are suppressed.
        /// </summary>
        public static DevicePropertyKey MuteDigitalPressureButtonsProperty =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{99ACCB6C-D709-49BB-90EE-278B1B564A4B}"), 2,
                typeof(byte));

        #endregion

        #region Common device properties

        public static DevicePropertyKey HostAddressProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0xa92f26ca, 0xeda7, 0x4b1d, {0x9d, 0xb2, 0x27, 0xb6, 0x8a, 0xa5, 0xa2, 0xeb}}"), 1,
            typeof(ulong));

        public static DevicePropertyKey DeviceAddressProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0x2bd67d8b, 0x8beb, 0x48d5, {0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a}}"), 1,
            typeof(string));

        public static DevicePropertyKey BluetoothLastConnectedTimeProperty =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{0x2bd67d8b, 0x8beb, 0x48d5, {0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a}}"), 11,
                typeof(DateTimeOffset));

        #endregion
    }

    /// <summary>
    ///     Battery status values.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum DsBatteryStatus : byte
    {
        [Description("Unknown")] Unknown = 0x00,
        [Description("Dying")] Dying = 0x01,
        [Description("Low")] Low = 0x02,
        [Description("Medium")] Medium = 0x03,
        [Description("High")] High = 0x04,
        [Description("Full")] Full = 0x05,
        [Description("Charging")] Charging = 0xEE,
        [Description("Charged")] Charged = 0xEF
    }

    /// <summary>
    ///     HID device emulation modes.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum DsHidDeviceMode : byte
    {
        [Description("SDF (PCSX2)")] Single = 0x01,

        [Description("GPJ (Generic DirectInput)")]
        Multi = 0x02,
        [Description("SXS (Steam, RPCS3)")] SixaxisCompatible = 0x03,
        [Description("DS4 (DS4Windows)")] DualShock4Rev1Compatible = 0x04
    }
}