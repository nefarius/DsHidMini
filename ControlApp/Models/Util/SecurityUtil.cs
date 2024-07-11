using System.Security.Principal;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

public static class SecurityUtil
{
    public static bool IsElevated
    {
        get
        {
            SecurityIdentifier? securityIdentifier = WindowsIdentity.GetCurrent().Owner;
            return securityIdentifier is not null && securityIdentifier
                .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
        }
    }
}