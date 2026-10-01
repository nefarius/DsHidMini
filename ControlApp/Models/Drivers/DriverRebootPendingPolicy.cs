namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     Per-device facts relevant to a pending driver reboot.
/// </summary>
internal sealed record DriverDeviceSnapshot(
    string InstanceId,
    bool IsRebootRequired,
    uint? ProblemCode,
    uint? DevNodeStatus,
    Version? BoundDriverVersion);

/// <summary>
///     Marker written by the MSI when it detected that a reboot is needed.
/// </summary>
internal sealed record DriverRebootMarker(bool IsSet, DateTime? SinceUtc, string? Reason);

internal sealed record DriverRebootPendingState(
    bool IsPending,
    IReadOnlyList<string> Reasons,
    int AffectedDeviceCount,
    bool MarkerIsStale)
{
    public static readonly DriverRebootPendingState None = new(false, [], 0, false);
}

/// <summary>
///     Decides whether the system still needs a reboot to finish a DsHidMini driver install or upgrade.
/// </summary>
internal static class DriverRebootPendingPolicy
{
    public const uint ProblemNeedRestart = 14;
    public const uint DnNeedRestart = 0x00000100;

    public static readonly TimeSpan BootTolerance = TimeSpan.FromMinutes(1);

    public static DateTime GetLastBootUtc()
    {
        return DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    public static DriverRebootPendingState Evaluate(
        DriverRebootMarker marker,
        DateTime lastBootUtc,
        Version? installedPackageVersion,
        IReadOnlyList<DriverDeviceSnapshot> devices)
    {
        List<string> reasons = [];
        bool markerIsStale = false;

        if (marker.IsSet)
        {
            // A marker without a timestamp can't be proven stale, so it counts as outstanding.
            if (marker.SinceUtc is { } since && since < lastBootUtc - BootTolerance)
            {
                markerIsStale = true;
            }
            else
            {
                reasons.Add(string.IsNullOrWhiteSpace(marker.Reason)
                    ? "Setup reported that a restart is required."
                    : $"Setup reported that a restart is required ({marker.Reason}).");
            }
        }

        HashSet<string> affected = new(StringComparer.OrdinalIgnoreCase);
        foreach (DriverDeviceSnapshot device in devices)
        {
            if (device.IsRebootRequired)
            {
                reasons.Add($"{device.InstanceId} is waiting for a restart.");
                affected.Add(device.InstanceId);
            }

            if (device.ProblemCode == ProblemNeedRestart ||
                (device.DevNodeStatus is { } status && (status & DnNeedRestart) != 0))
            {
                reasons.Add($"{device.InstanceId} needs a restart to start its driver.");
                affected.Add(device.InstanceId);
            }

            if (installedPackageVersion is not null && device.BoundDriverVersion is not null &&
                Normalize(device.BoundDriverVersion) < Normalize(installedPackageVersion))
            {
                reasons.Add(
                    $"{device.InstanceId} still uses driver {device.BoundDriverVersion}; " +
                    $"version {installedPackageVersion} is installed.");
                affected.Add(device.InstanceId);
            }
        }

        return new DriverRebootPendingState(reasons.Count > 0, reasons, affected.Count, markerIsStale);
    }

    private static Version Normalize(Version version)
    {
        return new Version(
            version.Major,
            Math.Max(version.Minor, 0),
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }
}
