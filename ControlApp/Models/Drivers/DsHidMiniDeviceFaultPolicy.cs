namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     A present HID devnode, including one whose driver failed before it exposed an interface.
/// </summary>
internal sealed record DsHidMiniDevNodeSnapshot(
    string InstanceId,
    string? DisplayName,
    string? DriverInfSection,
    uint? ProblemCode,
    uint? ProblemStatus);

/// <summary>
///     User-facing summary of one or more DsHidMini devnodes that Windows could not start.
/// </summary>
internal sealed record DsHidMiniDeviceFaultNotice(string Title, string Message, int DeviceCount);

/// <summary>
///     Decides which enumerated devnodes are failed DsHidMini devices and how to describe them.
/// </summary>
internal static class DsHidMiniDeviceFaultPolicy
{
    private static readonly Dictionary<uint, string> StatusNames = new()
    {
        [0xC0000001] = "STATUS_UNSUCCESSFUL",
        [0xC000000E] = "STATUS_NO_SUCH_DEVICE",
        [0xC000009C] = "STATUS_DEVICE_DATA_ERROR",
        [0xC000009D] = "STATUS_DEVICE_NOT_CONNECTED",
        [0xC000009E] = "STATUS_DEVICE_POWER_FAILURE",
        [0xC00000A3] = "STATUS_DEVICE_NOT_READY",
        [0xC00000B5] = "STATUS_IO_TIMEOUT",
        [0xC00000BB] = "STATUS_NOT_SUPPORTED",
        [0xC0000184] = "STATUS_INVALID_DEVICE_STATE",
        [0xC00002B6] = "STATUS_DEVICE_REMOVED"
    };

    public static bool IsDsHidMiniDriverSection(string? section)
    {
        return !string.IsNullOrWhiteSpace(section) &&
               section.Trim().StartsWith("dshidmini.", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<DsHidMiniDevNodeSnapshot> SelectFaulted(
        IEnumerable<DsHidMiniDevNodeSnapshot> nodes)
    {
        return nodes
            .Where(node => IsDsHidMiniDriverSection(node.DriverInfSection) && node.ProblemCode is > 0)
            .OrderBy(node => node.DisplayName ?? node.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static DsHidMiniDeviceFaultNotice? Describe(IReadOnlyList<DsHidMiniDevNodeSnapshot> faulted)
    {
        if (faulted.Count == 0)
        {
            return null;
        }

        DsHidMiniDevNodeSnapshot first = faulted[0];
        string name = string.IsNullOrWhiteSpace(first.DisplayName) ? first.InstanceId : first.DisplayName.Trim();
        string detail = $"{DescribeProblem(first.ProblemCode!.Value)} {DescribeStatus(first.ProblemStatus)}";

        if (faulted.Count == 1)
        {
            return new DsHidMiniDeviceFaultNotice(
                $"{name} could not start",
                $"{detail} This controller stays out of the device list until Windows can start its driver.",
                1);
        }

        int others = faulted.Count - 1;
        string extra = others == 1
            ? "1 other controller also failed to start."
            : $"{others} other controllers also failed to start.";

        return new DsHidMiniDeviceFaultNotice(
            $"{faulted.Count} controllers could not start",
            $"{name}: {detail} {extra}",
            faulted.Count);
    }

    public static string DescribeProblem(uint problemCode)
    {
        return problemCode switch
        {
            10 => "This device cannot start. (Code 10)",
            14 => "This device cannot work properly until you restart your computer. (Code 14)",
            22 => "This device is disabled. (Code 22)",
            28 => "The drivers for this device are not installed. (Code 28)",
            31 =>
                "This device is not working properly because Windows cannot load the drivers required for this device. (Code 31)",
            43 => "Windows has stopped this device because it has reported problems. (Code 43)",
            _ => $"This device has a problem. (Code {problemCode})"
        };
    }

    public static string DescribeStatus(uint? problemStatus)
    {
        if (problemStatus is null or 0)
        {
            return "Windows did not report a failure status.";
        }

        string hex = $"0x{problemStatus.Value:X8}";
        return StatusNames.TryGetValue(problemStatus.Value, out string? name)
            ? $"Windows reported {name} ({hex})."
            : $"Windows reported NTSTATUS {hex}.";
    }
}
