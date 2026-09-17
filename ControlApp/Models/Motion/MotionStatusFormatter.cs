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

    public static string StatusText(
        bool ipcAvailable,
        bool telemetryMapped,
        bool gotSnapshot,
        bool isFallback,
        DsMotionCalibrationSource calibrationSource = DsMotionCalibrationSource.LiveUsb)
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

        if (isFallback)
        {
            // Bluetooth never queries the pad for this; a cache miss (never
            // seen over USB) is the expected reason wireless stays nominal.
            // See issue #217.
            return calibrationSource == DsMotionCalibrationSource.None
                ? "Using nominal calibration. Connect this controller over USB once to cache its factory calibration."
                : "Using nominal calibration (EEPROM page 0xA0 was not loaded).";
        }

        return calibrationSource == DsMotionCalibrationSource.CachedFromUsb
            ? "Using factory EEPROM calibration (cached from USB)."
            : "Using factory EEPROM calibration.";
    }
}
