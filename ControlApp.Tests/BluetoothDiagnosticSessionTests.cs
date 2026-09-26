using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;
using Nefarius.Utilities.DeviceManagement.PnP;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

/// <summary>
///     Exercises the <see cref="BluetoothDiagnosticSession" /> state machine using hand-written
///     fakes so no ETW session, driver IPC, or real hardware is required. Preflight-blocked paths
///     need no extra seams. Capture and classification are reached via
///     <see cref="BluetoothDiagnosticSession.TryPairOverride" /> (skip live IPC) and a shortened
///     <see cref="BluetoothDiagnosticSession.WirelessAttemptWait" />; an empty
///     <see cref="DshmDevMan.Devices" /> list completes the unplug wait immediately. A wireless
///     reconnect (or a conclusive ETW success, including older BthPS3 event sets) ends the
///     observation window before that timeout.
/// </summary>
public class BluetoothDiagnosticSessionTests
{
    private sealed class FakePreflightProbe : IPreflightProbe
    {
        public IReadOnlyList<PreflightCheckResult> Results { get; set; } = Array.Empty<PreflightCheckResult>();
        public PnPDevice? Candidate { get; set; }
        public bool AutoRepairResult { get; set; }
        public PreflightCheckId? LastAutoRepairedId { get; private set; }
        public string BthPS3VersionDisplay { get; set; } = "Unknown";

        public int RunCount { get; private set; }

        public IReadOnlyList<PreflightCheckResult> Run(PnPDevice? candidateDevice = null)
        {
            RunCount++;
            return Results;
        }

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
        public DiagnosticEventRecord? EventOnStart { get; set; }
        public IReadOnlyList<DiagnosticEventRecord> EventsOnStart { get; set; } = [];
        public Exception? FaultAfterStop { get; set; }

        public event Action<DiagnosticEventRecord>? EventCaptured;
        public event Action<Exception>? CaptureFaulted;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            IsRunning = true;
            if (EventOnStart is { } record)
            {
                EventCaptured?.Invoke(record);
            }

            foreach (DiagnosticEventRecord started in EventsOnStart)
            {
                EventCaptured?.Invoke(started);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            StopCount++;
            IsRunning = false;
            if (FaultAfterStop is { } ex)
            {
                RaiseFault(ex);
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Raise(DiagnosticEventRecord record) => EventCaptured?.Invoke(record);
        public void RaiseFault(Exception ex) => CaptureFaulted?.Invoke(ex);
    }

    private sealed class FakeClassifier : IDiagnosticClassifier
    {
        public DiagnosticVerdict? NextResult { get; set; }
        public int ClassifyCount { get; private set; }
        public IReadOnlyList<PreflightCheckResult>? LastPreflight { get; private set; }
        public IReadOnlyList<DiagnosticEventRecord>? LastTimeline { get; private set; }
        public ulong? LastCandidateAddress { get; private set; }

        public DiagnosticVerdict Classify(
            IReadOnlyList<PreflightCheckResult> preflightResults,
            IReadOnlyList<DiagnosticEventRecord> timeline,
            ulong? candidateAddress = null)
        {
            ClassifyCount++;
            LastPreflight = preflightResults;
            LastTimeline = timeline;
            LastCandidateAddress = candidateAddress;
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

        BluetoothDiagnosticSession session = new(probe, capture, classifier, bundle, devMan)
        {
            // Snapshot preflight unless a test is exercising the USB-arrival wait.
            WaitForUsbWhenMissing = false
        };
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
        Assert.Equal("Turn it on", session.StatusMessage);
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
    public async Task RunAsync_FaultRaisedAfterCaptureStop_StaysFaultedWithoutClassifying()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture,
                FakeClassifier classifier, _, _) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;

        // Reach capture/classify without a live PnPDevice or 25s wireless wait.
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.Zero;

        capture.EventOnStart = new DiagnosticEventRecord(
            DateTimeOffset.UtcNow,
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "BthPS3",
            1,
            "RemoteConnectReceived",
            new Dictionary<string, object?>());
        capture.FaultAfterStop = new InvalidOperationException("pump died after stop");

        await session.RunAsync();

        Assert.Equal(BluetoothDiagnosticStage.Faulted, session.Stage);
        Assert.Null(session.Verdict);
        Assert.Equal(1, capture.StartCount);
        Assert.Equal(1, capture.StopCount);
        Assert.Single(session.Timeline);
        Assert.Equal(0, classifier.ClassifyCount);
        Assert.Null(classifier.LastPreflight);
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

    [Fact]
    public async Task RunAsync_MissingUsbOnly_WaitsUntilCancelled()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture, _, _, _) =
            CreateSession();

        session.WaitForUsbWhenMissing = true;
        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.UsbControllerPresent, false, "Controller is connected with USB",
                "Connect the controller to this PC with a USB cable.")
        ];
        probe.Candidate = null;

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForUsb);

        Assert.Equal("Connect the controller to this PC with a USB cable.", session.StatusMessage);
        Assert.Null(session.Verdict);
        Assert.Equal(0, capture.StartCount);

        session.Cancel();
        await run;

        Assert.Equal(BluetoothDiagnosticStage.Cancelled, session.Stage);
        Assert.Equal(0, capture.StartCount);
    }

    [Fact]
    public async Task RunAsync_SuccessEventsBeforeWirelessWait_DoNotCompleteTheAttempt()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture, _, _, _) =
            CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.FromSeconds(30);

        DateTimeOffset t = DateTimeOffset.UtcNow;
        capture.EventsOnStart =
        [
            Bth(BthPS3Events.RemoteDeviceName, t),
            Bth(BthPS3Events.RemoteDeviceIdentified, t.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, t.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, t.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, t.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, t.AddMilliseconds(5)),
            DsHid("SomeDsHidMiniEvent", t.AddMilliseconds(6))
        ];

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForWirelessAttempt);
        await Task.Delay(50);

        Assert.Equal(BluetoothDiagnosticStage.WaitingForWirelessAttempt, session.Stage);

        session.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BluetoothDiagnosticStage.Cancelled, session.Stage);
        Assert.Null(session.Verdict);
    }

    [Fact]
    public async Task RunAsync_CancelDuringWirelessWait_StaysCancelled()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture, _, _, _) =
            CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.FromSeconds(30);

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForWirelessAttempt);

        session.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BluetoothDiagnosticStage.Cancelled, session.Stage);
        Assert.Null(session.Verdict);
        Assert.Equal(1, capture.StopCount);
    }

    [Fact]
    public async Task RunAsync_WirelessReconnectObserved_CompletesBeforeTimeoutAndTreatsAsSuccess()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, _, FakeClassifier classifier, _,
                DshmDevMan devMan) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.FromSeconds(30);
        session.WirelessReconnectObservedOverride = () => false;
        classifier.NextResult = new DiagnosticVerdict(
            DiagnosticVerdictCode.Inconclusive, DiagnosticConfidence.Low, "n/a", "n/a", "n/a", []);

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForWirelessAttempt);

        session.WirelessReconnectObservedOverride = () => true;
        devMan.RefreshConnectedDevices();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BluetoothDiagnosticStage.Completed, session.Stage);
        Assert.Equal("Done.", session.StatusMessage);
        Assert.NotNull(session.Verdict);
        Assert.Equal(DiagnosticVerdictCode.Success, session.Verdict!.Code);
        Assert.Contains("reappeared over Bluetooth", session.Verdict.Explanation);
    }

    [Fact]
    public async Task RunAsync_WirelessAlreadyPresentAtStart_DoesNotCountAsReconnect()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, _, _, _, _) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.FromSeconds(30);
        session.WirelessReconnectObservedOverride = () => true;

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForWirelessAttempt);
        await Task.Delay(50);

        Assert.Equal(BluetoothDiagnosticStage.WaitingForWirelessAttempt, session.Stage);

        session.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BluetoothDiagnosticStage.Cancelled, session.Stage);
        Assert.Null(session.Verdict);
    }

    [Fact]
    public async Task RunAsync_LegacyBthPs3SuccessEvents_CompletesBeforeTimeout()
    {
        // Older BthPS3 never emits RemoteConnectReceived (event 27). Classic 1-26 events plus
        // DsHidMini activity must still end the wait immediately instead of sitting on the
        // full observation window.
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, FakeTraceCapture capture,
                FakeClassifier classifier, _, _) = CreateSession();

        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "Bluetooth is on", "ok")
        ];
        probe.Candidate = null;
        session.TryPairOverride = _ => Task.FromResult(true);
        session.WirelessAttemptWait = TimeSpan.FromSeconds(30);
        classifier.NextResult = new DiagnosticVerdict(
            DiagnosticVerdictCode.Success, DiagnosticConfidence.High,
            "The controller connected over Bluetooth", "ok", "none", []);

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForWirelessAttempt);

        DateTimeOffset t = DateTimeOffset.UtcNow;
        capture.Raise(Bth(BthPS3Events.RemoteDeviceName, t));
        capture.Raise(Bth(BthPS3Events.RemoteDeviceIdentified, t.AddMilliseconds(1)));
        capture.Raise(Bth(BthPS3Events.ChildDeviceCreationSuccessful, t.AddMilliseconds(2)));
        capture.Raise(Bth(BthPS3Events.HidControlChannelConnected, t.AddMilliseconds(3)));
        capture.Raise(Bth(BthPS3Events.HidInterruptChannelConnected, t.AddMilliseconds(4)));
        capture.Raise(Bth(BthPS3Events.RemoteDeviceOnline, t.AddMilliseconds(5)));
        capture.Raise(DsHid("SomeDsHidMiniEvent", t.AddMilliseconds(6)));

        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BluetoothDiagnosticStage.Completed, session.Stage);
        Assert.Equal("Done.", session.StatusMessage);
        Assert.Equal(DiagnosticVerdictCode.Success, session.Verdict!.Code);
        Assert.DoesNotContain(session.Timeline, e => e.EventName == BthPS3Events.RemoteConnectReceived);
    }

    [Fact]
    public async Task RunAsync_MissingUsbOnly_ReprobesWhenDeviceListUpdates()
    {
        (BluetoothDiagnosticSession session, FakePreflightProbe probe, _, _, _, DshmDevMan devMan) = CreateSession();

        session.WaitForUsbWhenMissing = true;
        probe.Results =
        [
            new PreflightCheckResult(PreflightCheckId.UsbControllerPresent, false, "Controller is connected with USB",
                "Connect the controller to this PC with a USB cable.")
        ];
        probe.Candidate = null;

        Task run = session.RunAsync();
        await WaitUntil(() => session.Stage == BluetoothDiagnosticStage.WaitingForUsb);
        int runsAfterWait = probe.RunCount;

        devMan.RefreshConnectedDevices();
        await WaitUntil(() => probe.RunCount > runsAfterWait);

        session.Cancel();
        await run;

        Assert.True(probe.RunCount > runsAfterWait);
        Assert.Equal(BluetoothDiagnosticStage.Cancelled, session.Stage);
    }

    private static DiagnosticEventRecord Bth(string eventName, DateTimeOffset at) =>
        new(
            at,
            KnownDiagnosticProviders.BthPS3,
            "BthPS3",
            0,
            eventName,
            new Dictionary<string, object?>());

    private static DiagnosticEventRecord DsHid(string eventName, DateTimeOffset at) =>
        new(
            at,
            KnownDiagnosticProviders.DsHidMini,
            "DsHidMini",
            0,
            eventName,
            new Dictionary<string, object?>());

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("Timed out waiting for diagnostic session state.");
    }
}
