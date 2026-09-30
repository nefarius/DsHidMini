namespace DualController.Core;

public static class DeviceOrigin
{
    // DsHidMini's DS4 emulation and ViGEm devices can have Sony's VID/PID.
    // An ancestry check is mandatory to avoid mapping an emulated controller
    // again, generating duplicate input or a feedback loop.
    public static bool IsPhysicalDs4(IEnumerable<string> ancestors,
        ushort vendor, ushort product, out bool bluetooth)
    {
        bluetooth = false;
        if (vendor != 0x054C || product is not (0x05C4 or 0x09CC)) return false;
        string[] ids = ancestors.Select(id => id.ToUpperInvariant()).ToArray();
        if (ids.Any(id => id.Contains("VIGEM") || id.Contains("BTHPS3")
            || id.Contains("DSHIDMINI"))) return false;
        // Require the matching physical USB controller, not merely a USB root.
        if (ids.Any(id => id.StartsWith($"USB\\VID_054C&PID_{product:X4}")))
            return true;
        bluetooth = ids.Any(id => id.StartsWith("BTHENUM\\")
            || id.StartsWith("BTHLEDEVICE\\"));
        return bluetooth;
    }
}
