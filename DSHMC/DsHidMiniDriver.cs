using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.Devcon;

namespace DSHMC
{
    public static class DsHidMiniDriver
    {
        public static Guid DeviceInterfaceGuid => Guid.Parse("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");

        public static DevicePropertyKey BatteryStatusProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 2,
            typeof(byte));

        public static DevicePropertyKey HidDeviceModeProperty => CustomDeviceProperty.CreateCustomDeviceProperty(
            Guid.Parse("{0x52fac1da, 0x5a52, 0x40f5, {0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2}}"), 3,
            typeof(byte));

        public enum DsBatteryStatus : byte
        {
            None = 0x00,
            Dying = 0x01,
            Low = 0x02,
            Medium = 0x03,
            High = 0x04,
            Full = 0x05,
            Charging = 0xEE,
            Charged = 0xEF
        }

        public enum DsHidDeviceMode : byte
        {
            Unknown = 0x00,
            Single,
            Multi,
            SixaxisCompatible
        }
    }
}
