namespace Nefarius.DsHidMini.ControlApp.Models.Input;

internal static class InputTesterStatusFormatter
{
    public static string StatusText(bool ipcAvailable, bool gotReport)
    {
        if (!ipcAvailable)
        {
            return "Driver IPC is not available.";
        }

        if (!gotReport)
        {
            return "Waiting for a raw input report…";
        }

        return "Showing the raw DualShock 3 report from driver IPC. HID mode does not change this mapping.";
    }
}
