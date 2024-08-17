using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Nefarius.DsHidMini.IPC.Util.Converters;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.IPC.Models.Drivers;

/// <summary>
///     Interface and property information about the DsHidMini driver.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class DsHidMiniDriver
{
    /// <summary>
    ///     Interface GUID common to all devices the DsHidMini driver supports.
    /// </summary>
    public static Guid DeviceInterfaceGuid => Guid.Parse("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");

    #region Read-only properties

    /// <summary>
    ///     The last reported <see cref="DsBatteryStatus"/> of the device.
    /// </summary>
    public static DevicePropertyKey BatteryStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 2,
        typeof(byte));

    public static DevicePropertyKey LastPairingStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 3,
        typeof(int));

    public static DevicePropertyKey LastHostRequestStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 5,
        typeof(int));

    #endregion

    #region Common device properties

    /// <summary>
    ///     The currently active <see cref="DsHidDeviceMode"/>.
    /// </summary>
    public static DevicePropertyKey HidDeviceModeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{6D293077-C3D6-4062-9597-BE4389404C02}"), 2,
        typeof(byte));

    /// <summary>
    ///     The Bluetooth MAC address the device is currently paired to.
    /// </summary>
    public static DevicePropertyKey HostAddressProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{0xa92f26ca, 0xeda7, 0x4b1d, {0x9d, 0xb2, 0x27, 0xb6, 0x8a, 0xa5, 0xa2, 0xeb}}"), 1,
        typeof(ulong));

    /// <summary>
    ///     The Bluetooth MAC address of the device itself.
    /// </summary>
    public static DevicePropertyKey DeviceAddressProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{0x2bd67d8b, 0x8beb, 0x48d5, {0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a}}"), 1,
        typeof(string));

    /// <summary>
    ///     Timestamp of last wireless connection.
    /// </summary>
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
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum DsBatteryStatus : byte
{
    /// <summary>
    ///     Unknown/not yet reported.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0x00,

    /// <summary>
    ///     Dying. Battery is so low the device is barely being kept on.
    /// </summary>
    [Description("Dying")]
    Dying = 0x01,

    /// <summary>
    ///     Low. Device should be charged soon.
    /// </summary>
    [Description("Low")]
    Low = 0x02,

    /// <summary>
    ///     Medium. Will last for a while but should be charged soon.
    /// </summary>
    [Description("Medium")]
    Medium = 0x03,

    /// <summary>
    ///     High. Will last for a few sessions.
    /// </summary>
    [Description("High")]
    High = 0x04,

    /// <summary>
    ///     Full. Status right after successful charging.
    /// </summary>
    [Description("Full")]
    Full = 0x05,

    /// <summary>
    ///     Charging. The default state while wired until <see cref="Charged" /> is reached.
    /// </summary>
    [Description("Charging")]
    Charging = 0xEE,

    /// <summary>
    ///     Charged. While wired synonymous to <see cref="Full" /> while wireless.
    /// </summary>
    [Description("Charged")]
    Charged = 0xEF
}

/// <summary>
///     HID device emulation modes.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum DsHidDeviceMode : byte
{
    /// <summary>
    ///     Single Device with Force Feedback mode.
    /// </summary>
    [Description("SDF (PCSX2)")]
    SDF = 0x01,

    /// <summary>
    ///     Gamepad plus Joystick mode.
    /// </summary>
    [Description("GPJ (Separated pressure)")]
    GPJ = 0x02,

    /// <summary>
    ///     SIXAXIS.SYS mode.
    /// </summary>
    [Description("SXS (Steam, RPCS3)")]
    SXS = 0x03,

    /// <summary>
    ///     DS4Windows DualShock 4 emulation mode.
    /// </summary>
    [Description("DS4Windows")]
    DS4W = 0x04,

    /// <summary>
    ///     Xbox One Controller mode.
    /// </summary>
    [Description("XInput")]
    XInput = 0x05
}