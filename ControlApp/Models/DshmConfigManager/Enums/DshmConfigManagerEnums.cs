using System.Diagnostics.CodeAnalysis;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

public enum SettingsModes
{
    Global,
    Profile,
    Custom
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum SettingsContext
{
    Unknown,
    SDF,
    GPJ,
    SXS,
    DS4W,
    XInput,
    General,
    Global
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum SettingsModeGroups
{
    LEDsControl,
    WirelessSettings,
    OutputReportControl,
    SticksDeadzone,
    RumbleGeneral,
    RumbleLeftStrRescale,
    RumbleRightConversion,
    Unique_All,
    Unique_Global,
    Unique_General,
    Unique_SDF,
    Unique_GPJ,
    Unique_SXS,
    Unique_DS4W,
    Unique_XInput
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum LEDsMode
{
    BatteryIndicatorPlayerIndex,
    BatteryIndicatorBarGraph,
    CustomStatic,
    CustomPattern
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Button
{
    PS,
    Start,
    Select,
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

public enum PressureMode
{
    Digital,
    Analogue,
    Default
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DPadMode
{
    Default,
    HAT,
    Buttons
}

public enum BluetoothPairingMode
{
    Auto,
    Custom,
    Disabled
}