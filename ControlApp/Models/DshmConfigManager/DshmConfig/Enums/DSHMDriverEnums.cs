using System.Diagnostics.CodeAnalysis;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HidDeviceMode
{
    SDF,
    GPJ,
    SXS,
    DS4Windows,
    XInput
}

public enum DevicePairingMode
{
    Auto,
    Custom,
    Disabled
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