using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <inheritdoc cref="IDiagnosticBundleWriter" />
public sealed class DiagnosticBundleWriter : IDiagnosticBundleWriter
{
    private static readonly string[] RedactedPropertyNameFragments = { "Address", "InstanceId", "HardwareId" };

    public async Task WriteAsync(
        DiagnosticBundleContent content,
        string destinationZipPath,
        bool redact = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(destinationZipPath);

        byte[] salt = RandomNumberGenerator.GetBytes(16);

        object summary = new
        {
            SchemaVersion = 1,
            content.ControlAppVersion,
            content.DsHidMiniDriverVersion,
            content.BthPS3Version,
            content.StartedAt,
            content.FinishedAt,
            Redacted = redact,
            RedactionNote = redact
                ? "Bluetooth addresses and instance IDs are hashed per export. The same device always " +
                  "hashes to the same token within this file, but the token cannot be reversed to the " +
                  "original value. The ControlApp log excerpt is omitted from redacted exports because " +
                  "its free-text lines may contain addresses or instance IDs that cannot be reliably " +
                  "redacted the same way; export without redaction if you need it."
                : null,
            Verdict = content.Verdict is { } verdict
                ? new
                {
                    verdict.Code,
                    verdict.Confidence,
                    verdict.LastSuccessfulMilestone,
                    verdict.Explanation,
                    verdict.RemediationAction
                }
                : null,
            Preflight = content.PreflightResults
                .Select(r => new { r.Id, r.Passed, r.Title, r.Detail, r.CanAutoRepair })
                .ToList()
        };

        List<object> timelineEntries = content.Timeline
            .Select(e => BuildTimelineEntry(e, redact, salt))
            .ToList();

        string? directory = Path.GetDirectoryName(Path.GetFullPath(destinationZipPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(destinationZipPath))
        {
            File.Delete(destinationZipPath);
        }

        await using FileStream fileStream = File.Create(destinationZipPath);
        using ZipArchive archive = new(fileStream, ZipArchiveMode.Create);

        await WriteJsonEntryAsync(archive, "summary.json", summary, cancellationToken).ConfigureAwait(false);
        await WriteJsonEntryAsync(archive, "timeline.json", timelineEntries, cancellationToken).ConfigureAwait(false);

        // Free-text log lines can contain Bluetooth addresses or instance IDs that the structured
        // per-property redaction above cannot reach; omit the excerpt entirely for redacted exports
        // rather than risk leaking one through unredacted log text (see RedactionNote above).
        string logExcerpt = redact ? string.Empty : TryReadRecentLogLines();
        if (!string.IsNullOrEmpty(logExcerpt))
        {
            ZipArchiveEntry logEntry = archive.CreateEntry("controlapp-log-excerpt.txt", CompressionLevel.Optimal);
            await using Stream entryStream = logEntry.Open();
            await using StreamWriter writer = new(entryStream, Encoding.UTF8);
            await writer.WriteAsync(logExcerpt).ConfigureAwait(false);
        }
    }

    private static object BuildTimelineEntry(DiagnosticEventRecord record, bool redact, byte[] salt)
    {
        Dictionary<string, object?> properties = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> pair in record.Properties)
        {
            properties[pair.Key] = redact && ShouldRedact(pair.Key) ? Redact(pair.Value, salt) : pair.Value;
        }

        return new
        {
            record.Timestamp,
            record.ProviderName,
            record.ProviderGuid,
            record.EventId,
            record.EventName,
            Properties = properties
        };
    }

    private static bool ShouldRedact(string propertyName)
    {
        return RedactedPropertyNameFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static object? Redact(object? value, byte[] salt)
    {
        if (value is null)
        {
            return null;
        }

        byte[] input = Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty);
        byte[] combined = new byte[salt.Length + input.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(input, 0, combined, salt.Length, input.Length);
        byte[] hash = SHA256.HashData(combined);
        return "REDACTED-" + Convert.ToHexString(hash)[..12];
    }

    private static string TryReadRecentLogLines()
    {
        try
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DsHidMini",
                "Log");
            if (!Directory.Exists(logPath))
            {
                return string.Empty;
            }

            string? latest = Directory.GetFiles(logPath, "ControlAppLog*.txt")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (latest is null)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, ReadLastLines(latest, 500));
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Failed to read recent ControlApp log lines for diagnostic bundle.");
            return string.Empty;
        }
    }

    /// <summary>
    ///     Reads only the last <paramref name="maxLines" /> lines of <paramref name="path" /> using a
    ///     small ring buffer, instead of loading the whole (potentially large, actively-growing) log
    ///     file into memory. Opens with <see cref="FileShare.ReadWrite" /> and
    ///     <see cref="FileShare.Delete" /> so reading the currently-active Serilog file (which the
    ///     logging pipeline keeps open for writing, and which a rolling/retention policy may delete)
    ///     does not throw a sharing violation.
    /// </summary>
    private static Queue<string> ReadLastLines(string path, int maxLines)
    {
        Queue<string> ring = new(maxLines);

        using FileStream stream = new(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (ring.Count == maxLines)
            {
                ring.Dequeue();
            }

            ring.Enqueue(line);
        }

        return ring;
    }

    private static async Task WriteJsonEntryAsync(
        ZipArchive archive,
        string entryName,
        object payload,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using Stream entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(
            entryStream,
            payload,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken).ConfigureAwait(false);
    }
}
