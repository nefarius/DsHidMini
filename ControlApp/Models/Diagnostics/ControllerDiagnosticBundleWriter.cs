using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Writes a controller diagnostic export (versioned, privacy-aware ZIP).
/// </summary>
public interface IControllerDiagnosticBundleWriter
{
    Task WriteAsync(
        ControllerDiagnosticContent content,
        string destinationZipPath,
        bool redact = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Redaction rules shared by the writer and its tests. They intentionally mirror the standalone probe
///     (research/ds3-motion/probe) so both paths produce comparable data.
/// </summary>
internal static class ControllerBundleRedaction
{
    /// <summary>
    ///     Byte offsets of a feature report that contain device or host Bluetooth address bytes.
    /// </summary>
    public static IReadOnlyList<int> AddressOffsets(byte reportId)
    {
        return reportId switch
        {
            0xF2 => new[] { 7, 8, 9 },
            0xF5 => new[] { 2, 3, 4, 5, 6, 7 },
            _ => Array.Empty<int>()
        };
    }

    /// <summary>
    ///     Returns a copy of <paramref name="data" /> with address bytes zeroed when <paramref name="redact" /> is set.
    /// </summary>
    public static byte[] MaskFeature(byte reportId, byte[] data, bool redact)
    {
        byte[] copy = (byte[])data.Clone();
        if (!redact)
        {
            return copy;
        }

        foreach (int offset in AddressOffsets(reportId))
        {
            if (offset < copy.Length)
            {
                copy[offset] = 0;
            }
        }

        return copy;
    }

    /// <summary>
    ///     Salted one-way token, stable within one export.
    /// </summary>
    public static string HashToken(string value, byte[] salt)
    {
        byte[] input = Encoding.UTF8.GetBytes(value);
        byte[] combined = new byte[salt.Length + input.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(input, 0, combined, salt.Length, input.Length);
        return "REDACTED-" + Convert.ToHexString(SHA256.HashData(combined))[..12];
    }
}

/// <inheritdoc cref="IControllerDiagnosticBundleWriter" />
public sealed class ControllerDiagnosticBundleWriter : IControllerDiagnosticBundleWriter
{
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task WriteAsync(
        ControllerDiagnosticContent content,
        string destinationZipPath,
        bool redact = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(destinationZipPath);

        byte[] salt = RandomNumberGenerator.GetBytes(16);

        string destination = Path.GetFullPath(destinationZipPath);
        string? directory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using FileStream fileStream = File.Create(tempPath);
            using ZipArchive archive = new(fileStream, ZipArchiveMode.Create);

            await WriteJsonEntryAsync(archive, "summary.json", BuildSummary(content, redact, salt), cancellationToken)
                .ConfigureAwait(false);

            if (content.IdentificationBlob is { Length: > 0 } blob)
            {
                await WriteJsonEntryAsync(archive, "identification.json", new
                {
                    Feature = "0x01",
                    Length = blob.Length,
                    Hex = Convert.ToHexString(blob)
                }, cancellationToken).ConfigureAwait(false);
            }

            if (content.DriverSweep is { } sweep)
            {
                await WriteJsonEntryAsync(archive, "driver-sweep.json", BuildSweep(sweep, redact, salt), cancellationToken)
                    .ConfigureAwait(false);
            }

            if (content.Telemetry is { } telemetry)
            {
                await WriteJsonEntryAsync(archive, "telemetry/phases.json", new
                {
                    telemetry.MotionAvailable,
                    telemetry.Note,
                    telemetry.Phases
                }, cancellationToken).ConfigureAwait(false);

                if (telemetry.MotionCsv.Length > 0)
                {
                    ZipArchiveEntry csv = archive.CreateEntry("telemetry/motion.csv", CompressionLevel.Optimal);
                    await using Stream stream = csv.Open();
                    await stream.WriteAsync(telemetry.MotionCsv, cancellationToken).ConfigureAwait(false);
                }

                if (telemetry.ReportRates.Count > 0)
                {
                    ZipArchiveEntry rates = archive.CreateEntry("telemetry/report-rate.csv", CompressionLevel.Optimal);
                    await using Stream stream = rates.Open();
                    await using StreamWriter writer = new(stream, new UTF8Encoding(false));
                    await writer.WriteLineAsync("OffsetMs,ReportRateHz,AverageIntervalUs").ConfigureAwait(false);
                    foreach (ControllerReportRateSample sample in telemetry.ReportRates)
                    {
                        await writer.WriteLineAsync(string.Create(
                            CultureInfo.InvariantCulture,
                            $"{sample.OffsetMs:0.0},{sample.ReportRateHz},{sample.AverageIntervalUs}")).ConfigureAwait(false);
                    }
                }
            }

            ZipArchiveEntry readme = archive.CreateEntry("README.txt", CompressionLevel.Optimal);
            await using (Stream stream = readme.Open())
            await using (StreamWriter writer = new(stream, new UTF8Encoding(false)))
            {
                await writer.WriteAsync(BuildReadme(redact)).ConfigureAwait(false);
            }
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        try
        {
            if (File.Exists(destination))
            {
                File.Replace(tempPath, destination, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, destination);
            }
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Failed to delete temporary diagnostic export '{Path}'.", path);
        }
    }

    private static object BuildSummary(ControllerDiagnosticContent content, bool redact, byte[] salt)
    {
        string? address = content.DeviceAddress;
        if (redact && !string.IsNullOrEmpty(address))
        {
            address = ControllerBundleRedaction.HashToken(address, salt);
        }

        return new
        {
            SchemaVersion,
            content.ControlAppVersion,
            content.DsHidMiniDriverVersion,
            content.CreatedAt,
            Redacted = redact,
            RedactionNote = redact
                ? "Bluetooth/host address bytes in Features 0xF2/0xF5, the USB serial number string and the " +
                  "device address are masked or hashed. Everything else is exported verbatim."
                : null,
            content.DeviceType,
            content.ConnectionType,
            DeviceAddress = address,
            content.IsDeviceAddressSynthesized,
            content.SlotIndex,
            content.AuthenticityLabel,
            AuthenticityNote = "The label is a heuristic over the Feature 0x01 identification blob, not a verdict.",
            DriverSweep = new
            {
                Collected = content.DriverSweep is not null,
                Note = content.DriverSweepNote,
                Status = content.DriverSweep is { } s ? Hex(s.Status) : null,
                Version = content.DriverSweep?.Version
            },
            Telemetry = new
            {
                Collected = content.Telemetry is not null,
                MotionAvailable = content.Telemetry?.MotionAvailable,
                Note = content.Telemetry?.Note
            }
        };
    }

    private static object BuildSweep(ControllerDiagnosticsResult sweep, bool redact, byte[] salt)
    {
        return new
        {
            SchemaVersion,
            sweep.Version,
            Status = Hex(sweep.Status),
            DeviceDescriptor = Convert.ToHexString(sweep.DeviceDescriptor),
            ConfigDescriptor = new
            {
                Status = Hex(sweep.ConfigDescriptorStatus),
                Hex = Convert.ToHexString(sweep.ConfigDescriptor)
            },
            Pipes = sweep.Pipes.Select(p => new
            {
                p.PipeType,
                EndpointAddress = $"0x{p.EndpointAddress:X2}",
                p.MaximumPacketSize,
                p.Interval
            }),
            Strings = sweep.Strings.Select((s, i) => new
            {
                Kind = i switch { 0 => "Manufacturer", 1 => "Product", _ => "SerialNumber" },
                s.Index,
                Status = Hex(s.Status),
                Text = redact && i == 2 && !string.IsNullOrEmpty(s.Text)
                    ? ControllerBundleRedaction.HashToken(s.Text, salt)
                    : s.Text
            }),
            Features = sweep.Features.Select(f => ReportEntry(f, redact, maskAddresses: true)),
            EepromPages = sweep.EepromPages.Select(p => ReportEntry(p, redact, maskAddresses: false)),
            RestoreStatus = Hex(sweep.RestoreStatus)
        };
    }

    private static object ReportEntry(DiagnosticsReport report, bool redact, bool maskAddresses)
    {
        byte[] data = maskAddresses
            ? ControllerBundleRedaction.MaskFeature(report.Id, report.Data, redact)
            : report.Data;

        return new
        {
            Id = $"0x{report.Id:X2}",
            SetStatus = Hex(report.SetStatus),
            GetStatus = Hex(report.GetStatus),
            Length = data.Length,
            Hex = Convert.ToHexString(data)
        };
    }

    private static string Hex(uint status)
    {
        return $"0x{status:X8}";
    }

    private static string BuildReadme(bool redact)
    {
        return "DsHidMini controller diagnostic export\r\n" +
               "======================================\r\n\r\n" +
               "summary.json          overview, versions, heuristic label\r\n" +
               "identification.json   cached Feature 0x01 blob\r\n" +
               "driver-sweep.json     descriptors, endpoints, strings, Features 0xF7/0xF8, EEPROM pages (USB only)\r\n" +
               "telemetry/            guided capture: motion.csv, report-rate.csv, phases.json\r\n\r\n" +
               (redact
                   ? "Address bytes and the serial number are masked/hashed. Please attach this file to your report.\r\n"
                   : "This export is NOT redacted and contains device addresses. Share only with people you trust.\r\n");
    }

    private static async Task WriteJsonEntryAsync(
        ZipArchive archive,
        string entryName,
        object payload,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using Stream entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(entryStream, payload, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
