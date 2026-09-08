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
    public static Guid DeviceInterfaceGuid => Guid.Parse("{16F3FE42-B710-4F67-B6EE-9A8D249C9CE5}");

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

    /// <summary>
    ///     Raw 64-byte <c>GET Feature 0x01</c> identification blob. USB only; see issue #50.
    /// </summary>
    public static DevicePropertyKey IdentificationDataProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 4,
        typeof(byte[]));

    public static DevicePropertyKey LastHostRequestStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 5,
        typeof(int));

    /// <summary>
    ///     One-based driver IPC slot index (<c>deviceIndex</c> for shared memory, per-slot events, and IPC <c>TargetIndex</c>).
    /// </summary>
    public static DevicePropertyKey IpcSlotIndexProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 6,
        typeof(uint));

    /// <summary>
    ///     <see langword="true"/> if <see cref="DeviceAddressProperty"/> was not reported by the hardware (device
    ///     did not answer <c>GET Feature 0xF2</c>) and was synthesized by the driver instead. Bluetooth pairing is
    ///     unavailable for such a device. See issue #321.
    /// </summary>
    public static DevicePropertyKey DeviceAddressSynthesizedProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 7,
        typeof(bool));

    /// <summary>
    ///     Feature 0x01 firmware/board revision packed as <c>b2&lt;&lt;16 | b3&lt;&lt;8 | b4</c>.
    /// </summary>
    public static DevicePropertyKey IdentificationFirmwareProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 8,
        typeof(uint));

    /// <summary>
    ///     Feature 0x01 pad/sensor type byte (offset 8). Informational only; not a gyro-path test.
    /// </summary>
    public static DevicePropertyKey IdentificationPadTypeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 9,
        typeof(byte));

    /// <summary>
    ///     Feature 0x01 motion path derived from the calibration field list.
    /// </summary>
    public static DevicePropertyKey IdentificationMotionPathProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 10,
        typeof(byte));

    /// <summary>
    ///     <see langword="true"/> if Feature 0x01 matches the clone heuristic (field list
    ///     <c>01 02</c> and byte <c>0x29 == 0x64</c>). Heuristic, not a verdict.
    /// </summary>
    public static DevicePropertyKey IdentificationCloneHeuristicProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 11,
        typeof(bool));

    /// <summary>
    ///     Hardware family (<see cref="DsDeviceType"/>). Navigation has one LED and no rumble.
    /// </summary>
    public static DevicePropertyKey DeviceTypeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
        Guid.Parse("{3FECF510-CC94-4FBE-8839-738201F84D59}"), 12,
        typeof(byte));

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
///     Gyro / motion code path derived from the Feature 0x01 calibration field list.
///     Type bytes 8-11 must not be used to pick this path (SIXAXIS-2 reports 0x18).
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum DsIdentificationMotionPath : byte
{
    /// <summary>
    ///     Report missing or field list could not be parsed.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    ///     Software zero against the EEPROM gyro zero. Field list starts <c>01 02</c>
    ///     at index 0 or 1 and does not contain field <c>0x07</c>.
    /// </summary>
    [Description("Software zero")]
    PlainZero = 1,

    /// <summary>
    ///     Hardware-calibrated gyro. Field list contains <c>0x07</c>.
    /// </summary>
    [Description("Hardware-calibrated gyro")]
    HwCal = 2,

    /// <summary>
    ///     Original SIXAXIS path. <c>PLAIN_ZERO</c> is clear (typically a single field <c>06</c>).
    /// </summary>
    [Description("SIXAXIS")]
    Sixaxis = 3
}

/// <summary>
///     Hardware family derived from USB/Bluetooth VID and PID. Matches
///     <c>DS_DEVICE_TYPE</c> in the driver.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum DsDeviceType : byte
{
    /// <summary>
    ///     Unknown or unclassified device.
    /// </summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>
    ///     Sony DualShock 3 / SIXAXIS.
    /// </summary>
    [Description("DualShock 3 / SIXAXIS")]
    Sixaxis = 1,

    /// <summary>
    ///     Sony Navigation Controller (CECH-ZCS1, PID 0x042F). One LED, no rumble.
    /// </summary>
    [Description("Navigation Controller")]
    Navigation = 2,

    /// <summary>
    ///     Sony PlayStation Move Motion Controller. Not supported.
    /// </summary>
    [Description("Motion Controller")]
    Motion = 3,

    /// <summary>
    ///     Sony DualShock 4. Not supported as a DsHidMini target.
    /// </summary>
    [Description("DualShock 4")]
    Wireless = 4
}

/// <summary>
///     HID device emulation modes.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public enum DsHidDeviceMode : byte
{
    /// <summary>
    ///     Single Device with Force Feedback mode.
    /// </summary>
    [Description("SDF (PCSX2 Non-Qt-Edition)")]
    SDF = 0x01,

    /// <summary>
    ///     Gamepad plus Joystick mode.
    /// </summary>
    [Description("GPJ (Separated pressure)")]
    GPJ = 0x02,

    /// <summary>
    ///     SIXAXIS.SYS mode.
    /// </summary>
    [Description("SXS (Steam, RPCS3, PCSX2 Qt-Edition)")]
    SXS = 0x03,

    /// <summary>
    ///     DS4Windows DualShock 4 emulation mode.
    /// </summary>
    [Description("DS4Windows")]
    DS4W = 0x04,

    /// <summary>
    ///     Xbox One Controller mode.
    /// </summary>
    [Description("XInput (Xbox One)")]
    XInput = 0x05
}