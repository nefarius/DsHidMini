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

        /// <summary>
        ///     Unified Device Property exposing current battery status.
        /// </summary>
        public static DevicePropertyKey BatteryStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 2,
            typeof(byte));

        /// <summary>
        ///     Unified Device Property exposing current HID device emulation mode.
        /// </summary>
        public static DevicePropertyKey HidDeviceModeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 3,
            typeof(byte));

        /// <summary>
        ///      Unified Device Property exposing output report timer delay (in ms).
        /// </summary>
        public static DevicePropertyKey OutputReportTimerPeriodMsProperty =>
            CustomDeviceProperty.CreateCustomDeviceProperty(
                Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 4,
                typeof(uint));

        public static DevicePropertyKey MuteDigitalPressureButtonsProperty =>
           CustomDeviceProperty.CreateCustomDeviceProperty(
               Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 5,
               typeof(byte));

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
    }

    /// <summary>
    ///     Battery status values.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum DsBatteryStatus : byte
    {
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
        [Description("PCSX2 compatible mode")] Single = 0x01,
        [Description("DirectInput compatible mode")] Multi = 0x02,

        [Description("SIXAXIS.SYS compatible mode")]
        SixaxisCompatible = 0x03,
        [Description("DS4Windows compatible mode")]
        DualShock4Rev1Compatible = 0x04
    }
}