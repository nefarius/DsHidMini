namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums
{
    public enum HidDeviceMode
    {
        SDF,
        GPJ,
        SXS,
        DS4Windows,
        XInput,
    }

    public enum PressureMode
    {
        Digital,
        Analogue,
        Default,
    }

    public enum DPadExposureMode
    {
        HAT,
        IndividualButtons,
        Default,
    }

    public enum LEDsMode
    {
        BatteryIndicatorPlayerIndex,
        BatteryIndicatorBarGraph,
        CustomPattern,
    }

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
        Left,
    }

    public enum DSHM_LEDsAuthority
    {
        Automatic,
        Driver,
        Application,
    }
}
