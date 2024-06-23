using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nefarius.DsHidMini.ControlApp.Models.Enums
{
    public enum HidModeShort : byte
    {
        Unkown = 0x00,
        SDF = 0x01,
        GPJ = 0x02,
        SXS = 0x03,
        DS4W = 0x04,
        XInput = 0x05,
    }
}
