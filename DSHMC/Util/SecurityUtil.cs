using System.Security.Principal;

namespace Nefarius.DsHidMini.Util
{
    public static class SecurityUtil
    {
        public static bool IsElevated
        {
            get
            {
                var securityIdentifier = WindowsIdentity.GetCurrent().Owner;
                return !(securityIdentifier is null) && securityIdentifier
                    .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }
    }
}
