#nullable enable
using System;
using System.Collections.Generic;

namespace Nefarius.DsHidMini.Setup;

/// <summary>
///     Per-devnode facts gathered after the driver install.
/// </summary>
internal sealed class DevNodeRebootInfo
{
    public string? InstanceId { get; set; }
    public bool IsRebootRequired { get; set; }
    public uint? ProblemCode { get; set; }
    public uint? DevNodeStatus { get; set; }
    public Version? BoundDriverVersion { get; set; }
}

/// <summary>
///     Decides whether a reboot is needed to finish a driver install/upgrade. Pure so tests can link it.
/// </summary>
internal static class RebootPendingDecision
{
    public const uint ProblemNeedRestart = 14;
    public const uint DnNeedRestart = 0x00000100;

    internal static bool Decide(
        bool installerRequestedReboot,
        Version? installedPackageVersion,
        IEnumerable<DevNodeRebootInfo> devices,
        out string reason)
    {
        List<string> reasons = new();

        if (installerRequestedReboot)
        {
            reasons.Add("the driver installer requested a reboot");
        }

        foreach (DevNodeRebootInfo device in devices)
        {
            string id = device.InstanceId ?? "unknown device";

            if (device.IsRebootRequired)
            {
                reasons.Add($"{id} reports a pending reboot");
            }

            if (device.ProblemCode == ProblemNeedRestart)
            {
                reasons.Add($"{id} has problem code 14 (restart required)");
            }

            if (device.DevNodeStatus is { } status && (status & DnNeedRestart) != 0)
            {
                reasons.Add($"{id} is flagged DN_NEED_RESTART");
            }

            if (installedPackageVersion != null && device.BoundDriverVersion != null &&
                Normalize(device.BoundDriverVersion) < Normalize(installedPackageVersion))
            {
                reasons.Add(
                    $"{id} still runs driver {device.BoundDriverVersion} (installed package {installedPackageVersion})");
            }
        }

        reason = string.Join("; ", reasons);
        return reasons.Count > 0;
    }

    private static Version Normalize(Version v)
    {
        return new Version(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
    }
}
