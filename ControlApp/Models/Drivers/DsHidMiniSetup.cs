using System.Globalization;

using Microsoft.Win32;

namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     Reads what the DsHidMini MSI recorded in its setup registry key.
/// </summary>
internal static class DsHidMiniSetup
{
    private const string SetupKeyPath =
        @"Software\Nefarius Software Solutions e.U.\Nefarius DsHidMini Driver";

    public static Version? InstalledDriverVersion => ReadVersion("DriverVersion");

    public static DriverRebootMarker ReadRebootMarker()
    {
        try
        {
            using RegistryKey? key = RegistryHelpers.GetRegistryKey(SetupKeyPath);
            if (key?.GetValue("RebootPending") is not int pending || pending == 0)
            {
                return new DriverRebootMarker(false, null, null);
            }

            DateTime? since = null;
            if (key.GetValue("RebootPendingSince") is string raw &&
                DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal, out DateTime parsed))
            {
                since = parsed;
            }

            return new DriverRebootMarker(true, since, key.GetValue("RebootPendingReason") as string);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to read DsHidMini reboot marker.");
            return new DriverRebootMarker(false, null, null);
        }
    }

    /// <summary>
    ///     Best effort; needs administrative rights.
    /// </summary>
    public static bool TryClearRebootMarker()
    {
        try
        {
            using RegistryKey? key = RegistryHelpers.GetRegistryKey(SetupKeyPath, true);
            if (key is null)
            {
                return true;
            }

            key.DeleteValue("RebootPending", false);
            key.DeleteValue("RebootPendingSince", false);
            key.DeleteValue("RebootPendingReason", false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Could not clear stale DsHidMini reboot marker.");
            return false;
        }
    }

    private static Version? ReadVersion(string valueName)
    {
        try
        {
            using RegistryKey? key = RegistryHelpers.GetRegistryKey(SetupKeyPath);
            return Version.TryParse(key?.GetValue(valueName)?.ToString(), out Version? version) ? version : null;
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to read DsHidMini setup registry value {ValueName}.", valueName);
            return null;
        }
    }
}
