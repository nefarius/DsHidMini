namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Diffs connected-device instance IDs so a refresh can keep live view-models
///     instead of disposing and recreating the whole list.
/// </summary>
internal static class DeviceListMerge
{
    public readonly record struct Result(
        IReadOnlyList<string> InstanceIdsToRemove,
        IReadOnlyList<string> InstanceIdsToAdd,
        IReadOnlyList<string> InstanceIdsToKeep);

    public static Result Compute(
        IReadOnlyList<string> currentInstanceIds,
        IReadOnlyList<string> nextInstanceIds)
    {
        HashSet<string> next = new(nextInstanceIds, StringComparer.OrdinalIgnoreCase);
        HashSet<string> current = new(currentInstanceIds, StringComparer.OrdinalIgnoreCase);

        List<string> remove = [];
        List<string> keep = [];
        foreach (string id in currentInstanceIds)
        {
            if (next.Contains(id))
            {
                keep.Add(id);
            }
            else
            {
                remove.Add(id);
            }
        }

        List<string> add = [];
        foreach (string id in nextInstanceIds)
        {
            if (!current.Contains(id))
            {
                add.Add(id);
            }
        }

        return new Result(remove, add, keep);
    }
}
