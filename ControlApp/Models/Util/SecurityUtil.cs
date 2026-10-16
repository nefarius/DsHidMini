using System.Security.Principal;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

public static class SecurityUtil
{
    /// <summary>
    ///     True when this process token has the Administrators group enabled.
    ///     A filtered UAC token is not treated as elevated, even when the account belongs to Administrators.
    /// </summary>
    public static bool IsElevated
    {
        get
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return IsAdministrator(identity);
        }
    }

    internal static bool IsAdministrator(WindowsIdentity identity) =>
        new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
}
