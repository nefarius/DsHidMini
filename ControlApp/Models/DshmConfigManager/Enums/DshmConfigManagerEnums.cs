namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums
{
    public enum SettingsModes
    {
        Global,
        Profile,
        Custom,
    }
    public enum SettingsContext
    {
        Unknown,
        SDF,
        GPJ,
        SXS,
        DS4W,
        XInput,
        General,
        Global,
    }

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
        Unique_XInput,
    }

    public enum LEDsMode
    {
        BatteryIndicatorPlayerIndex,
        BatteryIndicatorBarGraph,
        CustomStatic,
        CustomPattern,
    }

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
        Left,
    }

    public enum PressureMode
    {
        Digital,
        Analogue,
        Default,
    }

    public enum DPadMode
    {
        Default,
        HAT,
        Buttons,
    }

    public enum BluetoothPairingMode
    {
        Auto,
        Custom,
        Disabled,
    }
}
