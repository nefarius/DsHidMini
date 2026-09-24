using System.Reflection;

using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     Decides whether a bound DsHidMini driver is new enough to publish
///     <see cref="IPC.Models.Drivers.DsHidMiniDriver.IpcSlotIndexProperty" />.
/// </summary>
internal static class DsHidMiniDriverCompatibility
{
    /// <summary>
    ///     First driver version that publishes the IPC slot property (tag <c>v3.4.0</c>).
    /// </summary>
    public static readonly Version MinimumIpcDriverVersion = new(3, 4, 0);

    public const string MissingIpcSlotMessage =
        "The driver did not report an IPC slot for this device.";

    public static Version? TryParseDriverVersion(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string candidate = raw.Trim();
        int comma = candidate.LastIndexOf(',');
        if (comma >= 0)
        {
            candidate = candidate[(comma + 1)..].Trim();
        }

        return Version.TryParse(candidate, out Version? version) ? version : null;
    }

    public static Version? TryGetInstalledVersion(PnPDevice device)
    {
        try
        {
            return TryParseDriverVersion(device.GetProperty<string>(DevicePropertyKey.Device_DriverVersion));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     True when <paramref name="installed" /> is known and older than
    ///     <see cref="MinimumIpcDriverVersion" />. An unreadable version is not treated as outdated.
    /// </summary>
    public static bool IsOlderThanIpcMinimum(Version? installed)
    {
        return installed is not null && Normalize(installed) < Normalize(MinimumIpcDriverVersion);
    }

    public static string DescribeMissingIpcSlot(Version? installedDriverVersion)
    {
        if (!IsOlderThanIpcMinimum(installedDriverVersion))
        {
            return MissingIpcSlotMessage;
        }

        string appVersion = ControlAppVersion;
        string appLabel = string.IsNullOrWhiteSpace(appVersion)
            ? "this ControlApp"
            : $"this ControlApp ({appVersion})";

        return
            $"Driver {installedDriverVersion} is older than {appLabel}. " +
            $"Pairing and diagnostics need DsHidMini driver {MinimumIpcDriverVersion} or newer; " +
            "install the matching driver package and reconnect the controller.";
    }

    public static string DescribeMissingIpcSlot(PnPDevice device)
    {
        return DescribeMissingIpcSlot(TryGetInstalledVersion(device));
    }

    public static string FormatInstalledVersion(PnPDevice device)
    {
        return TryGetInstalledVersion(device)?.ToString() ?? "unknown";
    }

    private static string ControlAppVersion
    {
        get
        {
            string? informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                int metadataSeparator = informational.IndexOf('+');
                return metadataSeparator >= 0 ? informational[..metadataSeparator] : informational;
            }

            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    ///     <see cref="Version" /> stores an omitted build or revision as -1, which sorts below 0.
    ///     Treat those as 0 so <c>3.4</c> and <c>3.4.0</c> compare equal.
    /// </summary>
    private static Version Normalize(Version version)
    {
        return new Version(
            version.Major,
            version.Minor,
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }
}
