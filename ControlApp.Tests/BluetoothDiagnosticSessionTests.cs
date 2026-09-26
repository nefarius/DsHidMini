using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;
using Nefarius.Utilities.DeviceManagement.PnP;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

/// <summary>
///     Exercises the <see cref="BluetoothDiagnosticSession" /> state machine using hand-written
///     fakes so no ETW session, driver IPC, or real hardware is required. Only the branches that do
///     not need a live <see cref="PnPDevice" /> are covered here (preflight-blocked paths); the
///     pair/unplug/connect branches require real hardware and are covered by the manual hardware
///     matrix described in the Bluetooth Diagnostic Assistant plan.
/// </summary>
public class BluetoothDiagnosticSessionTests
{
    private sealed class FakePreflightProbe : IPreflightProbe
    {
        public IReadOnlyList<PreflightCheckResult> Results { get; set; } = Array.Empty<PreflightCheckResult>();
        public PnPDevice? Candidate { get; set; }
        public bool AutoRepairResult { get; set; }
        public PreflightCheckId? LastAutoRepairedId { get; private set; }

        public IReadOnlyList<PreflightCheckResult> Run(PnPDevice? candidateDevice = null) => Results;

        public PnPDevice? FindEligibleUsbController() => Candidate;

        public bool TryAutoRepair(PreflightCheckId id)
        {
            LastAutoRepairedId = id;
            return AutoRepairResult;
        }
    }

    private sealed class FakeTraceCapture : ITraceCapture
    {
        public bool IsRunning { get; private set; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }

        public event Action<DiagnosticEventRecord>? EventCaptured;
        public event Action<Exception>? CaptureFaulted;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            IsRunning = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCount++;
            IsRunning = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Raise(DiagnosticEventRecord record) => EventCaptured?.Invoke(record);
        public void RaiseFault(Exception ex) => CaptureFaulted?.Invoke(ex);
    }

    private sealed class FakeClassifier : IDiagnosticClassifier
    {
        public DiagnosticVerdict? NextResult { get; set; }
        public IReadOnlyList<PreflightCheckResult>? LastPreflight { get; private set; }
        public IReadOnlyList<DiagnosticEventRecord>? LastTimeline { get; private set; }

        public DiagnosticVerdict Classify(
            IReadOnlyList<PreflightCheckResult> preflightResults,
            IReadOnlyList<DiagnosticEventRecord> timeline)
        {
            LastPreflight = preflightResults;
            LastTimeline = timeline;
            return NextResult ?? new DiagnosticVerdict(
                DiagnosticVerdictCode.Inconclusive,
                DiagnosticConfidence.Low,
                "n/a",
                "n/a",
                "n/a",
                Array.Empty<DiagnosticEventRecord>());
        }
    }

    private sealed class FakeBundleWriter : IDiagnosticBundleWriter
    {
        public DiagnosticBundleContent? LastContent { get; private set; }

        public Task WriteAsync(
            DiagnosticBundleContent content,
            string destinationZipPath,
            bool redact = true,
            CancellationToken cancellationToken = default)
        {
            LastContent = content;
            return Task.CompletedTask;
        }
    }

    private static (BluetoothDiagnosticSession Session, FakePreflightProbe Probe, FakeTraceCapture Capture,
        FakeClassifier Classifier, FakeBundleWriter Bundle, DshmDevMan DevMan) CreateSession()
    {
        FakePreflightProbe probe = new();
        FakeTraceCapture capture = new();
        FakeClassifier classifier = new();
        FakeBundleWriter bundle = new();
        DshmDevMan devMan = new();

        BluetoothDiagnosticSession session = new(probe, capture, classifier, bundle, devMan);
        return (session, probe, capture, classifier, bundle, devMan);
    }

    [Fact]
    public async Task RunAsync_FailedPreflightCheck_StopsAtPreflightBlockedWithoutStartingCapture()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture,
                FakeClassifier classifier, _, _) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, false, "Bluetooth is on", "Turn it on")
        ];
        probe.Candidate = null;

        classifier.NextResult = new DiagnosticVerdict(
            DiagnosticVerdictCode.PreflightBlocked,
            DiagnosticConfidence.High,
            "Nothing checked yet",
            "Turn it on",
            "Turn it on",
            Array.Empty<DiagnosticEventRecord>());

        await session.RunAsync();

        Assert.Equal(BluetoothDiagnosticStage.PreflightBlocked, session.Stage);
        Assert.NotNull(session.Verdict);
        Assert.Equal(DiagnosticVerdictCode.PreflightBlocked, session.Verdict!.Code);
        Assert.Equal(0, capture.StartCount);
        Assert.Empty(session.Timeline);
    }

    [Fact]
    public async Task RunAsync_PreflightPassesButNoEligibleController_StopsAtPreflightBlocked()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture, _, _, _) =
            CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null; // no USB controller connected

        await session.RunAsync();

        Assert.Equal(BluetoothDiagnosticStage.PreflightBlocked, session.Stage);
        Assert.Equal(0, capture.StartCount);
    }

    [Fact]
    public void Cancel_BeforeAnyRun_DoesNotThrow()
    {
        (BluetoothDiagnosticSession session, _, _, _, _, _) = CreateSession();

        session.Cancel();
    }

    [Fact]
    public async Task TryAutoRepairBlockedCheck_WithAutoRepairableFailure_DelegatesToProbe()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, _, _, _, _) = CreateSession();

        // Simulate a prior run having populated PreflightResults via reflection-free path: run once
        // with a repairable failing check.
        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BthPS3SettingsCorrect, false, "t", "d", CanAutoRepair: true)
        ];
        probe.AutoRepairResult = true;

        await session.RunAsync();

        bool repaired = session.TryAutoRepairBlockedCheck();

        Assert.True(repaired);
        Assert.Equal(PreflightCheckId.BthPS3SettingsCorrect, probe.LastAutoRepairedId);
    }

    [Fact]
    public async Task ExportBundleAsync_PassesCurrentVerdictAndPreflightToWriter()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, _, FakeClassifier classifier,
                FakeBundleWriter bundle, _) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, false, "t", "d")
        ];
        classifier.NextResult = new DiagnosticVerdict(
            DiagnosticVerdictCode.PreflightBlocked, DiagnosticConfidence.High, "t", "e", "r", []);

        await session.RunAsync();
        await session.ExportBundleAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip"));

        Assert.NotNull(bundle.LastContent);
        Assert.Equal(DiagnosticVerdictCode.PreflightBlocked, bundle.LastContent!.Verdict!.Code);
        Assert.Same(probe.Results, bundle.LastContent.PreflightResults);
    }
}
