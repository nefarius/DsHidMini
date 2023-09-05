using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;
using LEDsMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.LEDsMode;
using PressureMode = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.PressureMode;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager
{
    public class DshmManagerToDriverConversion
    {
        public static Dictionary<SettingsContext, DshmConfig.Enums.HidDeviceMode> HidDeviceMode = new()
        {
            {SettingsContext.Global , DshmConfig.Enums.HidDeviceMode.XInput},
            {SettingsContext.General , DshmConfig.Enums.HidDeviceMode.XInput},
            {SettingsContext.SDF , DshmConfig.Enums.HidDeviceMode.SDF},
            {SettingsContext.GPJ , DshmConfig.Enums.HidDeviceMode.GPJ},
            {SettingsContext.SXS , DshmConfig.Enums.HidDeviceMode.SXS},
            {SettingsContext.DS4W , DshmConfig.Enums.HidDeviceMode.DS4Windows},
            {SettingsContext.XInput , DshmConfig.Enums.HidDeviceMode.XInput},
        };

        //---------------------------------------------------- LEDsModes

        public static Dictionary<LEDsMode, DshmConfig.Enums.LEDsMode> LedModeManagerToDriver = new()
        {
            { LEDsMode.BatteryIndicatorPlayerIndex, DshmConfig.Enums.LEDsMode.BatteryIndicatorPlayerIndex },
            { LEDsMode.BatteryIndicatorBarGraph, DshmConfig.Enums.LEDsMode.BatteryIndicatorBarGraph },
            { LEDsMode.CustomStatic, DshmConfig.Enums.LEDsMode.CustomPattern },
            { LEDsMode.CustomPattern, DshmConfig.Enums.LEDsMode.CustomPattern },
        };

        //---------------------------------------------------- DPadModes

        public static Dictionary<DPadMode, DPadExposureMode> DPadExposureModeManagerToDriver = new()
        {
            { DPadMode.Default, DPadExposureMode.Default },
            { DPadMode.HAT, DPadExposureMode.HAT },
            { DPadMode.Buttons, DPadExposureMode.IndividualButtons },
        };

        //---------------------------------------------------- PressureModes

        public static Dictionary<PressureMode, DshmConfig.Enums.PressureMode> DsPressureModeManagerToDriver = new()
        {
            { PressureMode.Default, DshmConfig.Enums.PressureMode.Default },
            { PressureMode.Analogue, DshmConfig.Enums.PressureMode.Analogue },
            { PressureMode.Digital, DshmConfig.Enums.PressureMode.Digital },
        };

        public static Dictionary<Button, DshmConfig.Enums.Button> ButtonManagerToDriver = new()
        {
            { Button.None, DshmConfig.Enums.Button.None },
            { Button.PS, DshmConfig.Enums.Button.PS },
            { Button.START, DshmConfig.Enums.Button.START },
            { Button.SELECT, DshmConfig.Enums.Button.SELECT },
            { Button.R1, DshmConfig.Enums.Button.R1 },
            { Button.L1, DshmConfig.Enums.Button.L1 },
            { Button.R2, DshmConfig.Enums.Button.R2 },
            { Button.L2, DshmConfig.Enums.Button.L2 },
            { Button.R3, DshmConfig.Enums.Button.R3 },
            { Button.L3, DshmConfig.Enums.Button.L3 },
            { Button.Triangle, DshmConfig.Enums.Button.Triangle },
            { Button.Circle, DshmConfig.Enums.Button.Circle },
            { Button.Cross, DshmConfig.Enums.Button.Cross },
            { Button.Square, DshmConfig.Enums.Button.Square },
            { Button.Up, DshmConfig.Enums.Button.Up },
            { Button.Right, DshmConfig.Enums.Button.Right },
            { Button.Down, DshmConfig.Enums.Button.Dowm },
            { Button.Left, DshmConfig.Enums.Button.Left },
        };
    }
}
