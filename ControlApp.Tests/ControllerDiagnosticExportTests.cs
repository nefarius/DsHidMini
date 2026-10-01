using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;
using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace ControlApp.Tests;

public class ControllerDiagnosticExportTests
{
    [Fact]
    public void IpcLayout_MatchesDriverHeader()
    {
        Assert.Equal(76, Marshal.SizeOf<DSHM_IPC_DIAG_REPORT>());
        Assert.Equal(136, Marshal.SizeOf<DSHM_IPC_DIAG_STRING>());
        Assert.Equal(8, Marshal.SizeOf<DSHM_IPC_DIAG_PIPE>());
        Assert.Equal(28, Marshal.SizeOf<DSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REQUEST>());
        // Header (20) + C struct layout from driver/IPC.h
        Assert.Equal(2388, Marshal.SizeOf<DSHM_IPC_MSG_COLLECT_DIAGNOSTICS_REPLY>());
        Assert.Equal(9u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_COLLECT_DIAGNOSTICS);
    }

    [Fact]
    public void Redaction_MasksAddressBytesOnlyWhenRedacting()
    {
        byte[] f2 = Enumerable.Repeat((byte)0xAA, 16).ToArray();
        byte[] f5 = Enumerable.Repeat((byte)0xBB, 16).ToArray();
        byte[] f7 = Enumerable.Repeat((byte)0xCC, 16).ToArray();

        byte[] m2 = ControllerBundleRedaction.MaskFeature(0xF2, f2, true);
        Assert.Equal(0, m2[7]);
        Assert.Equal(0, m2[9]);
        Assert.Equal(0xAA, m2[6]);
        Assert.Equal(0xAA, m2[10]);

        byte[] m5 = ControllerBundleRedaction.MaskFeature(0xF5, f5, true);
        Assert.All(new[] { 2, 3, 4, 5, 6, 7 }, i => Assert.Equal(0, m5[i]));
        Assert.Equal(0xBB, m5[8]);

        Assert.Equal(f7, ControllerBundleRedaction.MaskFeature(0xF7, f7, true));
        Assert.Equal(f2, ControllerBundleRedaction.MaskFeature(0xF2, f2, false));
        Assert.Equal(0xAA, f2[7]); // input untouched
    }

    private static ControllerDiagnosticsResult SampleSweep(uint restoreStatus = 0)
    {
        DiagnosticsReport Ok(byte id) => new()
        {
            Id = id,
            Data = Enumerable.Repeat((byte)0x11, 64).ToArray()
        };

        return new ControllerDiagnosticsResult
        {
            Version = 1,
            Status = 0,
            DeviceDescriptor = new byte[18],
            ConfigDescriptor = new byte[41],
            Pipes = new[] { new DiagnosticsPipe { EndpointAddress = 0x81, MaximumPacketSize = 64, Interval = 1 } },
            Strings = new[]
            {
                new DiagnosticsString { Index = 1, Text = "Sony" },
                new DiagnosticsString { Index = 2, Text = "PLAYSTATION(R)3 Controller" },
                new DiagnosticsString { Index = 3, Text = "SERIAL-1234" }
            },
            Features = new[] { Ok(0x01), Ok(0xF2), Ok(0xF5), Ok(0xF7), Ok(0xF8) },
            EepromPages = Enumerable.Range(0, 16).Select(i => Ok((byte)(i * 16))).ToArray(),
            RestoreStatus = restoreStatus
        };
    }

    private static ControllerDiagnosticContent Content(ControllerDiagnosticsResult? sweep, ControllerTelemetryCapture? t) =>
        new()
        {
            ControlAppVersion = "1.0",
            DeviceType = "Ds3",
            ConnectionType = "USB",
            DeviceAddress = "AA:BB:CC:DD:EE:FF",
            DriverSweep = sweep,
            Telemetry = t
        };

    private static async Task<(string Summary, string? Sweep, string[] Entries)> WriteAndRead(
        ControllerDiagnosticContent content, bool redact)
    {
        string path = Path.Combine(Path.GetTempPath(), $"diag-{Guid.NewGuid():N}.zip");
        try
        {
            await new ControllerDiagnosticBundleWriter().WriteAsync(content, path, redact);
            using ZipArchive zip = ZipFile.OpenRead(path);
            string Read(string name)
            {
                using StreamReader r = new(zip.GetEntry(name)!.Open(), Encoding.UTF8);
                return r.ReadToEnd();
            }

            return (Read("summary.json"),
                zip.GetEntry("driver-sweep.json") is null ? null : Read("driver-sweep.json"),
                zip.Entries.Select(e => e.FullName).ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Bundle_Redacted_HidesAddressAndSerial()
    {
        var (summary, sweep, entries) = await WriteAndRead(Content(SampleSweep(), null), redact: true);

        Assert.DoesNotContain("AA:BB:CC:DD:EE:FF", summary);
        Assert.Contains("REDACTED-", summary);
        Assert.NotNull(sweep);
        Assert.DoesNotContain("SERIAL-1234", sweep);
        Assert.Contains("PLAYSTATION(R)3 Controller", sweep);
        Assert.Contains("README.txt", entries);

        using JsonDocument doc = JsonDocument.Parse(summary);
        Assert.Equal(1, doc.RootElement.GetProperty("SchemaVersion").GetInt32());
    }

    [Fact]
    public async Task Bundle_Unredacted_KeepsSerialAndAddress()
    {
        var (summary, sweep, _) = await WriteAndRead(Content(SampleSweep(), null), redact: false);

        Assert.Contains("AA:BB:CC:DD:EE:FF", summary);
        Assert.Contains("SERIAL-1234", sweep!);
    }

    [Fact]
    public async Task Bundle_CancelledWrite_PreservesExistingDestination()
    {
        string path = Path.Combine(Path.GetTempPath(), $"diag-{Guid.NewGuid():N}.zip");
        await File.WriteAllTextAsync(path, "keep-me");
        try
        {
            using CancellationTokenSource cts = new();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new ControllerDiagnosticBundleWriter().WriteAsync(Content(SampleSweep(), null), path, true, cts.Token));

            Assert.Equal("keep-me", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Bundle_WithoutSweepOrTelemetry_StillWrites()
    {
        var content = Content(null, null);
        var (summary, sweep, entries) = await WriteAndRead(content, redact: true);

        Assert.Null(sweep);
        Assert.Contains("summary.json", entries);
        Assert.DoesNotContain("driver-sweep.json", entries);
        Assert.Contains("\"Collected\": false", summary);
    }

    [Fact]
    public async Task Bundle_WithTelemetry_AddsCsvAndPhases()
    {
        var telemetry = new ControllerTelemetryCapture
        {
            MotionAvailable = true,
            MotionCsv = Encoding.UTF8.GetBytes("SampleIndex\n1\n"),
            Phases = new[] { new ControllerPhaseResult { Name = "Still", SampleCount = 1 } },
            ReportRates = new[] { new ControllerReportRateSample(1000, 100, 10000) }
        };

        var (_, _, entries) = await WriteAndRead(Content(null, telemetry), redact: true);

        Assert.Contains("telemetry/motion.csv", entries);
        Assert.Contains("telemetry/phases.json", entries);
        Assert.Contains("telemetry/report-rate.csv", entries);
    }

    [Fact]
    public void Interpret_ReportsOldDriverBluetoothAndPartialFailures()
    {
        var old = ControllerDiagnosticCollector.Interpret(
            new ControllerDiagnosticsResult { Status = 0xC0000002 });
        Assert.Null(old.Result);
        Assert.Contains("too old", old.Note);

        var failedRestore = ControllerDiagnosticCollector.Interpret(SampleSweep(restoreStatus: 0xC0000001));
        Assert.NotNull(failedRestore.Result);
        Assert.Contains("0xA0", failedRestore.Note);

        var clean = ControllerDiagnosticCollector.Interpret(SampleSweep());
        Assert.Null(clean.Note);
    }

    private static ControllerExportRequest Request(bool wireless, int? slot) =>
        new("Pad", "Ds3", wireless ? "Bluetooth" : "USB", wireless, slot, "AA", false, "1.0", null, null);

    [Fact]
    public async Task Collect_WirelessAndMissingSlot_SkipSweepWithoutCallingDriver()
    {
        bool called = false;
        ControllerDiagnosticsResult Sweep(int _) { called = true; return SampleSweep(); }

        var bt = await ControllerDiagnosticCollector.CollectSweepAsync(Request(true, 1), true, Sweep);
        var noSlot = await ControllerDiagnosticCollector.CollectSweepAsync(Request(false, null), true, Sweep);
        var noIpc = await ControllerDiagnosticCollector.CollectSweepAsync(Request(false, 1), false, Sweep);

        Assert.False(called);
        Assert.Null(bt.Result);
        Assert.Null(noSlot.Result);
        Assert.Null(noIpc.Result);
    }

    [Fact]
    public async Task Collect_ThrowingDriver_ReturnsNoteInsteadOfThrowing()
    {
        var outcome = await ControllerDiagnosticCollector.CollectSweepAsync(
            Request(false, 1), true, _ => throw new TimeoutException("boom"));

        Assert.Null(outcome.Result);
        Assert.Contains("boom", outcome.Note);
    }

    private sealed class FakeSource : IControllerTelemetrySource
    {
        private uint _index;
        public bool HasMotionTelemetry { get; init; } = true;
        public bool HasInputReportMetrics => true;
        public bool Frozen { get; init; }
        public bool Disconnect { get; init; }

        public bool TryGetMotion(out DsMotionSnapshot snapshot, TimeSpan timeout)
        {
            Thread.Sleep(5);
            snapshot = new DsMotionSnapshot
            {
                SlotIndex = Disconnect ? 0u : 1u,
                Flags = DsMotionSnapshotFlags.Available,
                SampleIndex = ++_index,
                TimestampQpc = _index * 50000UL,
                RawAccelX = Frozen ? (ushort)512 : (ushort)(500 + _index % 7)
            };
            return true;
        }

        public bool TryGetMetrics(out DsInputReportMetrics metrics)
        {
            metrics = new DsInputReportMetrics { SequenceNumber = (int)_index, ReportRateHz = 100, AverageIntervalUs = 10000 };
            return true;
        }

        public void Dispose() { }
    }

    private static readonly ControllerCapturePhase[] ShortPhases =
    {
        new("Still", "hold", TimeSpan.FromMilliseconds(150)),
        new("Shake", "shake", TimeSpan.FromMilliseconds(150))
    };

    [Fact]
    public async Task Runner_RecordsPhasesCsvAndDistinctCounts()
    {
        var result = await GuidedCaptureRunner.RunAsync(new FakeSource(), ShortPhases, null, CancellationToken.None);

        Assert.True(result.MotionAvailable);
        Assert.Equal(2, result.Phases.Count);
        Assert.All(result.Phases, p => Assert.True(p.SampleCount > 0));
        Assert.True(result.Phases[0].DistinctRawAccelX > 1);
        Assert.NotEmpty(result.MotionCsv);
        Assert.StartsWith("SampleIndex,", Encoding.UTF8.GetString(result.MotionCsv));
        Assert.NotEmpty(result.ReportRates);
    }

    [Fact]
    public async Task Runner_FrozenSensor_ShowsOneDistinctValue()
    {
        var result = await GuidedCaptureRunner.RunAsync(
            new FakeSource { Frozen = true }, ShortPhases, null, CancellationToken.None);

        Assert.All(result.Phases, p => Assert.Equal(1, p.DistinctRawAccelX));
    }

    [Fact]
    public async Task Runner_NoTelemetryOrDisconnect_ReturnsPartialWithNote()
    {
        var none = await GuidedCaptureRunner.RunAsync(
            new FakeSource { HasMotionTelemetry = false }, ShortPhases, null, CancellationToken.None);
        Assert.False(none.MotionAvailable);
        Assert.NotNull(none.Note);

        var gone = await GuidedCaptureRunner.RunAsync(
            new FakeSource { Disconnect = true }, ShortPhases, null, CancellationToken.None);
        Assert.False(gone.MotionAvailable);
        Assert.Contains("disconnected", gone.Note);
        Assert.Empty(gone.MotionCsv);
    }

    [Fact]
    public async Task Runner_Cancellation_Throws()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GuidedCaptureRunner.RunAsync(new FakeSource(), ShortPhases, null, cts.Token));
    }
}
