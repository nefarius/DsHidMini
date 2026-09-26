using System.Text.Json;

using Nefarius.Utilities.ETW;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Live <see cref="ITraceCapture" /> backed by an in-process real-time ETW session covering the
///     BthPS3, BthPS3PSM, and DsHidMini provider GUIDs. Events are decoded via TDH from the drivers'
///     registered instrumentation manifests, so no symbol server is required for classification;
///     WPP text is intentionally not resolved here (see the support bundle writer for that).
/// </summary>
public sealed class EtwDiagnosticTraceCapture : ITraceCapture
{
    /// <summary>
    ///     Fixed, recognizable session name so a crashed previous run can be cleaned up on next start.
    /// </summary>
    public const string SessionName = "DsHidMini-BluetoothDiagnostics";

    private CancellationTokenSource? _cts;
    private Task? _pumpTask;
    private EtwRealtimeSession? _session;

    public event Action<DiagnosticEventRecord>? EventCaptured;
    public event Action<Exception>? CaptureFaulted;

    public bool IsRunning => _session is not null;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        try
        {
            // Best-effort: removes a session orphaned by a previous crash/kill. Safe no-op
            // when nothing is running under this name.
            EtwUtil.StopOrphanSession(SessionName);
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "StopOrphanSession for '{SessionName}' failed (likely nothing to clean up).",
                SessionName);
        }

        // Keep the new session local until every provider is enabled. If any EnableProvider call
        // throws, '_session' must stay null (not a half-configured session) so IsRunning correctly
        // reports 'not running' and a subsequent StartAsync retry actually creates a fresh session
        // instead of short-circuiting on a broken one that was never disposed.
        EtwRealtimeSession session = EtwRealtimeSession.Create(SessionName, options =>
        {
            // System time (FILETIME) instead of the QPC default so captured timestamps are
            // directly usable in the human-facing timeline and support bundle.
            options.ClockResolution = EtwClockResolution.SystemTime;
            options.ReportError = message => Log.Logger.Warning("ETW session '{SessionName}': {Message}",
                SessionName, message);
        });

        try
        {
            session.EnableProvider(KnownDiagnosticProviders.BthPS3, TraceEventLevel.Verbose);
            session.EnableProvider(KnownDiagnosticProviders.BthPS3Psm, TraceEventLevel.Verbose);
            session.EnableProvider(KnownDiagnosticProviders.DsHidMini, TraceEventLevel.Verbose);
        }
        catch
        {
            session.Dispose();
            throw;
        }

        _session = session;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pumpTask = Task.Run(() => PumpAsync(_cts.Token));

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        if (_pumpTask is not null)
        {
            try
            {
                await _pumpTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Logger.Debug(ex, "Diagnostic trace pump ended with an exception during stop.");
            }
        }

        _session?.Dispose();
        _session = null;
        _cts?.Dispose();
        _cts = null;
        _pumpTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private async Task PumpAsync(CancellationToken token)
    {
        try
        {
            await foreach (ReadOnlyMemory<byte> json in EtwUtil
                               .EnumerateRealtimeEventsAsync(SessionName, cancellationToken: token)
                               .ConfigureAwait(false))
            {
                DiagnosticEventRecord? record;
                try
                {
                    record = ParseEvent(json.Span);
                }
                catch (Exception ex)
                {
                    Log.Logger.Debug(ex, "Failed to decode one diagnostic ETW event, skipping it.");
                    continue;
                }

                if (record is not null)
                {
                    EventCaptured?.Invoke(record);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on StopAsync.
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Diagnostic ETW capture pump terminated unexpectedly.");
            CaptureFaulted?.Invoke(ex);
        }
    }

    /// <summary>
    ///     Parses one self-contained <c>{"Event":{...}}</c> JSON object as produced by
    ///     <see cref="EtwUtil.EnumerateRealtimeEventsAsync" /> into a <see cref="DiagnosticEventRecord" />.
    ///     Internal (not private) so unit tests can validate parsing against fixture payloads without
    ///     a live ETW session.
    /// </summary>
    internal static DiagnosticEventRecord? ParseEvent(ReadOnlySpan<byte> json)
    {
        using JsonDocument document = JsonDocument.Parse(json.ToArray());
        if (!document.RootElement.TryGetProperty("Event", out JsonElement evt))
        {
            return null;
        }

        long timestamp = evt.TryGetProperty("Timestamp", out JsonElement tsEl) ? tsEl.GetInt64() : 0;
        Guid providerGuid = evt.TryGetProperty("ProviderGuid", out JsonElement guidEl) &&
                             Guid.TryParse(guidEl.GetString(), out Guid parsedGuid)
            ? parsedGuid
            : Guid.Empty;
        int id = evt.TryGetProperty("Id", out JsonElement idEl) ? idEl.GetInt32() : 0;
        string name = evt.TryGetProperty("Name", out JsonElement nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;

        Dictionary<string, object?> properties = new(StringComparer.Ordinal);
        if (evt.TryGetProperty("Properties", out JsonElement propsArray) &&
            propsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement obj in propsArray.EnumerateArray())
            {
                if (obj.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (JsonProperty property in obj.EnumerateObject())
                {
                    properties[property.Name] = ConvertJsonValue(property.Value);
                }
            }
        }

        string providerName = ResolveProviderName(providerGuid);

        DateTimeOffset ts;
        try
        {
            ts = timestamp > 0 ? DateTimeOffset.FromFileTime(timestamp) : DateTimeOffset.UtcNow;
        }
        catch (ArgumentOutOfRangeException)
        {
            ts = DateTimeOffset.UtcNow;
        }

        return new DiagnosticEventRecord(ts, providerGuid, providerName, id, name, properties);
    }

    private static string ResolveProviderName(Guid providerGuid)
    {
        if (providerGuid == KnownDiagnosticProviders.BthPS3)
        {
            return "BthPS3";
        }

        if (providerGuid == KnownDiagnosticProviders.BthPS3Psm)
        {
            return "BthPS3PSM";
        }

        if (providerGuid == KnownDiagnosticProviders.DsHidMini)
        {
            return "DsHidMini";
        }

        return providerGuid.ToString();
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetUInt64(out ulong asUInt64) ? asUInt64 : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}
