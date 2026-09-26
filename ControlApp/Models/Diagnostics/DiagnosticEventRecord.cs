namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     A single structured ETW event captured (or replayed) for Bluetooth connection diagnostics.
///     Deliberately decoupled from the raw ETW/JSON wire format so the classifier and its tests
///     never need a live trace session.
/// </summary>
public sealed record DiagnosticEventRecord(
    DateTimeOffset Timestamp,
    Guid ProviderGuid,
    string ProviderName,
    int EventId,
    string EventName,
    IReadOnlyDictionary<string, object?> Properties)
{
    public ulong? GetUInt64(string name) => TryGet(name, out ulong value) ? value : null;

    public uint? GetUInt32(string name) => TryGet(name, out uint value) ? value : null;

    public bool? GetBool(string name)
    {
        if (!Properties.TryGetValue(name, out object? raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            bool b => b,
            byte b => b != 0,
            uint u => u != 0,
            ulong u => u != 0,
            int i => i != 0,
            _ => null
        };
    }

    public string? GetString(string name) =>
        Properties.TryGetValue(name, out object? raw) ? raw as string : null;

    private bool TryGet<T>(string name, out T value) where T : struct
    {
        value = default;
        if (!Properties.TryGetValue(name, out object? raw) || raw is null)
        {
            return false;
        }

        try
        {
            value = (T)Convert.ChangeType(raw, typeof(T));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
