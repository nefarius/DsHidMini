using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class BluetoothConnectionClassifierTests
{
    private static readonly IReadOnlyList<PreflightCheckResult> AllPassed =
    [
        new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, true, "t", "d")
    ];

    private readonly BluetoothConnectionClassifier _classifier = new();

    private static DiagnosticEventRecord Bth(string eventName, IReadOnlyDictionary<string, object?>? props = null,
        DateTimeOffset? at = null)
    {
        return new DiagnosticEventRecord(
            at ?? DateTimeOffset.UtcNow,
            KnownDiagnosticProviders.BthPS3,
            "BthPS3",
            0,
            eventName,
            props ?? new Dictionary<string, object?>());
    }

    private static DiagnosticEventRecord Psm(string eventName, IReadOnlyDictionary<string, object?>? props = null,
        DateTimeOffset? at = null)
    {
        return new DiagnosticEventRecord(
            at ?? DateTimeOffset.UtcNow,
            KnownDiagnosticProviders.BthPS3Psm,
            "BthPS3PSM",
            0,
            eventName,
            props ?? new Dictionary<string, object?>());
    }

    private static DiagnosticEventRecord DsHid(string eventName, DateTimeOffset? at = null)
    {
        return new DiagnosticEventRecord(
            at ?? DateTimeOffset.UtcNow,
            KnownDiagnosticProviders.DsHidMini,
            "DsHidMini",
            0,
            eventName,
            new Dictionary<string, object?>());
    }

    [Fact]
    public void FailedPreflightCheck_ProducesPreflightBlocked_RegardlessOfTimeline()
    {
        List<PreflightCheckResult> preflight =
        [
            new PreflightCheckResult(PreflightCheckId.BluetoothRadioOperable, false, "Bluetooth is on", "Turn it on")
        ];

        DiagnosticVerdict verdict = _classifier.Classify(preflight, [Bth(BthPS3Events.RemoteDeviceOnline)]);

        Assert.Equal(DiagnosticVerdictCode.PreflightBlocked, verdict.Code);
        Assert.Contains("Turn it on", verdict.Explanation);
    }

    [Fact]
    public void NoEvents_ProducesNoWirelessAttemptObserved()
    {
        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, []);

        Assert.Equal(DiagnosticVerdictCode.NoWirelessAttemptObserved, verdict.Code);
    }

    [Fact]
    public void PatchDisabled_WithNoRemoteConnectReceived_ProducesPsmPatchMissing()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Psm(BthPS3PsmEvents.PsmPatchActivity, new Dictionary<string, object?> { ["Patched"] = false })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.PsmPatchMissing, verdict.Code);
    }

    [Fact]
    public void NameLookupFailure_ProducesRemoteDeviceUnknown()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived),
            Bth(BthPS3Events.FailedWithNTStatus,
                new Dictionary<string, object?> { ["FunctionName"] = "BthPS3_GetDeviceName" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.RemoteDeviceUnknown, verdict.Code);
    }

    [Fact]
    public void UnidentifiedDevice_ProducesRemoteDeviceRejected()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived),
            Bth(BthPS3Events.RemoteDeviceName),
            Bth(BthPS3Events.RemoteDeviceNotIdentified)
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.RemoteDeviceRejected, verdict.Code);
    }

    [Fact]
    public void IdentifiedButChildCreationFailed_ProducesChildCreationFailed()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived),
            Bth(BthPS3Events.RemoteDeviceIdentified),
            Bth(BthPS3Events.ChildDeviceCreationFailed)
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.ChildCreationFailed, verdict.Code);
    }

    [Fact]
    public void ControlChannelOnly_NoInterruptChannel_ProducesControlChannelOnly()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived),
            Bth(BthPS3Events.RemoteDeviceIdentified),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful),
            Bth(BthPS3Events.HidControlChannelConnected)
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.ControlChannelOnly, verdict.Code);
    }

    [Fact]
    public void BothChannelsButNoOnlineEvent_ProducesInterruptChannelFailed()
    {
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived),
            Bth(BthPS3Events.RemoteDeviceIdentified),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful),
            Bth(BthPS3Events.HidControlChannelConnected),
            Bth(BthPS3Events.HidInterruptChannelConnected)
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.InterruptChannelFailed, verdict.Code);
    }

    [Fact]
    public void OnlineWithNoDsHidMiniActivity_ProducesBthPs3OnlineDsHidMiniMissing()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, at: baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, at: baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, at: baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, at: baseTime.AddMilliseconds(5))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
    }

    [Fact]
    public void OnlineWithDsHidMiniActivityAfterward_ProducesSuccess()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, at: baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, at: baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, at: baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, at: baseTime.AddMilliseconds(5)),
            DsHid("SomeDsHidMiniEvent", baseTime.AddMilliseconds(6))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.Success, verdict.Code);
        Assert.Equal(DiagnosticConfidence.High, verdict.Confidence);
    }

    [Fact]
    public void Classify_ThrowsOnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(null!, []));
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(AllPassed, null!));
    }
}
