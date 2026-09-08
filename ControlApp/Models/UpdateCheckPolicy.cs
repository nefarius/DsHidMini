namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Pure rules for the ControlApp startup update check. Kept separate so daily
///     gating and version comparison can be unit-tested without HTTP or WPF.
/// </summary>
internal static class UpdateCheckPolicy
{
    public static bool ShouldPerformNetworkCheck(
        bool isUpdateCheckEnabled,
        DateOnly today,
        DateOnly? lastUpdateCheckDate)
    {
        return isUpdateCheckEnabled && lastUpdateCheckDate != today;
    }

    public static bool TryParseFileVersion(string? value, out Version? version)
    {
        if (!string.IsNullOrWhiteSpace(value) && Version.TryParse(value.Trim(), out Version parsed))
        {
            version = parsed;
            return true;
        }

        version = null;
        return false;
    }

    public static bool IsRemoteNewer(Version remote, Version local)
    {
        return remote.CompareTo(local) > 0;
    }
}
