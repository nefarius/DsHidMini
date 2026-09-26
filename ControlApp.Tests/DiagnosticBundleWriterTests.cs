using System.IO.Compression;
using System.Text.Json;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DiagnosticBundleWriterTests
{
    private static DiagnosticEventRecord MakeEvent(ulong address, string instanceId)
    {
        return new DiagnosticEventRecord(
            DateTimeOffset.UtcNow,
            KnownDiagnosticProviders.BthPS3,
            "BthPS3",
            21,
            BthPS3Events.RemoteDeviceOnline,
            new Dictionary<string, object?> { ["Address"] = address, ["InstanceId"] = instanceId });
    }

    [Fact]
    public async Task WriteAsync_WithRedaction_HidesAddressButKeepsCorrelation()
    {
        DiagnosticBundleWriter writer = new();
        DiagnosticEventRecord sameDeviceFirst = MakeEvent(0xAABBCCDDEEFF, "USB\\VID_1234");
        DiagnosticEventRecord sameDeviceSecond = MakeEvent(0xAABBCCDDEEFF, "USB\\VID_1234");
        DiagnosticEventRecord differentDevice = MakeEvent(0x001122334455, "USB\\VID_5678");

        DiagnosticBundleContent content = new(
            null,
            [],
            [sameDeviceFirst, sameDeviceSecond, differentDevice],
            "1.0.0",
            "3.5.0",
            "2.0.144",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        try
        {
            await writer.WriteAsync(content, path);

            using ZipArchive archive = ZipFile.OpenRead(path);
            ZipArchiveEntry? timelineEntry = archive.GetEntry("timeline.json");
            Assert.NotNull(timelineEntry);

            using Stream entryStream = timelineEntry!.Open();
            using JsonDocument doc = await JsonDocument.ParseAsync(entryStream);
            JsonElement[] events = doc.RootElement.EnumerateArray().ToArray();

            Assert.Equal(3, events.Length);

            string firstAddress = events[0].GetProperty("Properties").GetProperty("Address").GetString()!;
            string secondAddress = events[1].GetProperty("Properties").GetProperty("Address").GetString()!;
            string thirdAddress = events[2].GetProperty("Properties").GetProperty("Address").GetString()!;

            Assert.StartsWith("REDACTED-", firstAddress);
            Assert.Equal(firstAddress, secondAddress); // same device -> same token
            Assert.NotEqual(firstAddress, thirdAddress); // different device -> different token
            Assert.DoesNotContain("AABBCCDDEEFF", firstAddress);

            ZipArchiveEntry? summaryEntry = archive.GetEntry("summary.json");
            Assert.NotNull(summaryEntry);
            using Stream summaryStream = summaryEntry!.Open();
            using JsonDocument summaryDoc = await JsonDocument.ParseAsync(summaryStream);
            Assert.True(summaryDoc.RootElement.GetProperty("Redacted").GetBoolean());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_WithoutRedaction_KeepsOriginalValues()
    {
        DiagnosticBundleWriter writer = new();
        DiagnosticEventRecord evt = MakeEvent(0xAABBCCDDEEFF, "USB\\VID_1234");

        DiagnosticBundleContent content = new(
            null,
            [],
            [evt],
            "1.0.0",
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        try
        {
            await writer.WriteAsync(content, path, redact: false);

            using ZipArchive archive = ZipFile.OpenRead(path);
            using Stream entryStream = archive.GetEntry("timeline.json")!.Open();
            using JsonDocument doc = await JsonDocument.ParseAsync(entryStream);

            JsonElement address = doc.RootElement[0].GetProperty("Properties").GetProperty("Address");
            Assert.Equal(JsonValueKind.Number, address.ValueKind);
            Assert.Equal(0xAABBCCDDEEFFUL, address.GetUInt64());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
