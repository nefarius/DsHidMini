namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HidDeviceMode
{
    SDF,
    GPJ,
    SXS,
    DS4Windows,
    XInput,
    CGP,
    CGS
}

public enum DevicePairingMode
{
    Auto,
    Custom,
    Disabled
}

/// <summary>
///     Which USB transport is used to send output reports (LEDs/rumble). See issue #321.
/// </summary>
public enum UsbOutputReportTransport
{
    Auto,
    InterruptOut,
    ControlEndpoint
}

/// <summary>
///     Which Bluetooth HID channel is used to send output reports (LEDs/rumble).
///     Control is the historical default; Interrupt is the PR 460 candidate.
/// </summary>
public enum BluetoothOutputReportTransport
{
    Control,
    Interrupt
}

public enum PressureMode
{
    Digital,
    Analogue,
    Default
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DPadExposureMode
{
    HAT,
    IndividualButtons,
    Default
}

/// <summary>
///     Coordinate frame of the HID Sensor collections in CGS mode (<c>CGS.MotionSensorFrame</c>).
/// </summary>
public enum MotionSensorFrame
{
    /// <summary>
    ///     GameInput/SDL/DS4 gamepad frame: Y points out of the face, flat face-up reads (0, +1 g, 0).
    /// </summary>
    Gamepad,

    /// <summary>
    ///     Windows tablet screen frame: Z points out of the face, flat face-up reads (0, 0, -1 g).
    /// </summary>
    Windows
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum LEDsMode
{
    BatteryIndicatorPlayerIndex,
    BatteryIndicatorBarGraph,
    CustomPattern
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum Button
{
    None,
    PS,
    START,
    SELECT,
    R1,
    L1,
    R2,
    L2,
    R3,
    L3,
    Triangle,
    Circle,
    Cross,
    Square,
    Up,
    Right,
    Down,
    Left
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DSHM_LEDsAuthority
{
    Automatic,
    Driver,
    Application
}