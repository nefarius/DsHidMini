using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

internal static class MotionStatusFormatter
{
    public static string PathLabel(DsIdentificationMotionPath path)
    {
        return path switch
        {
            DsIdentificationMotionPath.HwCal => "Hardware-calibrated gyro",
            DsIdentificationMotionPath.PlainZero => "Software zero",
            DsIdentificationMotionPath.Sixaxis => "SIXAXIS",
            _ => "Unknown"
        };
    }

    public static string StatusText(bool ipcAvailable, bool telemetryMapped, bool gotSnapshot, bool isFallback)
    {
        if (!ipcAvailable)
        {
            return "Driver IPC is not available.";
        }

        if (!telemetryMapped)
        {
            return "This driver build has no motion telemetry region.";
        }

        if (!gotSnapshot)
        {
            return "Waiting for a motion sample…";
        }

        return isFallback
            ? "Using nominal calibration (Bluetooth or EEPROM read fallback)."
            : "Live USB calibration.";
    }
}
