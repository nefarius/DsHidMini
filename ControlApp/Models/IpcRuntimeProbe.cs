using Nefarius.DsHidMini.IPC;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Checks that the loaded driver is actually serving IPC, not only that the configuration says it should.
/// </summary>
public static class IpcRuntimeProbe
{
    public static bool TryPingDriver() =>
        TryPingDriver(
            static () => DsHidMiniInterop.IsAvailable,
            static () =>
            {
                using DsHidMiniInterop interop = new(subscribeToDeviceChanges: false);
                interop.SendPing();
            });

    internal static bool TryPingDriver(Func<bool> isAvailable, Action ping)
    {
        try
        {
            if (!isAvailable())
            {
                return false;
            }

            ping();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Driver IPC ping failed.");
            return false;
        }
    }
}
