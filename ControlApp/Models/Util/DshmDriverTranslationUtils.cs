using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;
internal class DshmDriverTranslationUtils
{
    public static Dictionary<int, SettingsContext> HidDeviceMode = new()
        {
            { 0x01 , SettingsContext.SDF},
            { 0x02 , SettingsContext.GPJ},
            { 0x03 , SettingsContext.SXS},
            { 0x04 , SettingsContext.DS4W},
            { 0x05 , SettingsContext.XInput},
        };
}
