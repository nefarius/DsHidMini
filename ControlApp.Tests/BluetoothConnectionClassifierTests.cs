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

    private static DiagnosticEventRecord DsHid(string eventName, DateTimeOffset? at = null,
        IReadOnlyDictionary<string, object?>? props = null)
    {
        return new DiagnosticEventRecord(
            at ?? DateTimeOffset.UtcNow,
            KnownDiagnosticProviders.DsHidMini,
            "DsHidMini",
            0,
            eventName,
            props ?? new Dictionary<string, object?>());
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
    public void OnlineWithOnlyWirelessDisconnectRequested_DoesNotCountAsLegacyActivity()
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
            DsHid(DsHidMiniEvents.WirelessDisconnectRequested, baseTime.AddMilliseconds(6))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Medium, verdict.Confidence);
    }

    [Fact]
    public void OnlineWithOnlyFailedWithNTStatus_DoesNotCountAsLegacyActivity()
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
            DsHid(DsHidMiniEvents.FailedWithNTStatus, baseTime.AddMilliseconds(6))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Medium, verdict.Confidence);
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
    public void OnlineWithZeroDsHidMiniEventsEver_ProducesLowConfidenceHint_NotHighConfidenceBlame()
    {
        // Nothing from DsHidMini at all, at any point in the run: could be a broken handoff, or
        // could just be an old DsHidMini driver with limited ETW instrumentation. Must not blame
        // DsHidMini with full confidence in that case.
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, at: baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, at: baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, at: baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, at: baseTime.AddMilliseconds(5))
            // no DsHidMini events anywhere in the timeline
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Low, verdict.Confidence);
        Assert.Contains("may not report detailed activity", verdict.Explanation);
    }

    [Fact]
    public void OnlineWithDsHidMiniActivityOnlyBeforeOnline_KeepsMediumConfidenceBlame()
    {
        // DsHidMini clearly emits ETW events in general (proving instrumentation works), just none
        // after BthPS3 reported the device online: this is a stronger signal of a real handoff
        // problem, so confidence should stay Medium (not downgraded to Low).
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            DsHid("EarlierUnrelatedEvent", baseTime.AddSeconds(-10)),
            Bth(BthPS3Events.RemoteConnectReceived, at: baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, at: baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, at: baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, at: baseTime.AddMilliseconds(5))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Medium, verdict.Confidence);
        Assert.DoesNotContain("may not report detailed activity", verdict.Explanation);
    }

    [Fact]
    public void OldDriverWithoutRemoteConnectReceived_ButLegacyEventsPresent_StillProducesSuccess()
    {
        // Simulates a BthPS3 install predating event 27 (RemoteConnectReceived) and the
        // BthPS3PSM PsmPatchActivity event: none of the new structured events are emitted, only
        // the classic 1-26 range. This must classify identically to a fully up-to-date driver
        // instead of falling through to "nothing happened".
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteDeviceName, at: baseTime),
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
        Assert.DoesNotContain(verdict.Evidence, e => e.EventName == BthPS3Events.RemoteConnectReceived);
    }

    [Fact]
    public void OldDriverWithoutAnyEvents_ProducesLowConfidenceNoWirelessAttempt()
    {
        // No BthPS3 or BthPS3PSM events at all (old driver, nothing happened, and the newer
        // BthPS3PSM PsmPatchActivity event does not exist to rule out a silently-disabled filter).
        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, []);

        Assert.Equal(DiagnosticVerdictCode.NoWirelessAttemptObserved, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Low, verdict.Confidence);
        Assert.Contains("does not report Bluetooth filter activity", verdict.Explanation);
    }

    [Fact]
    public void NewDriverWithoutAnyEvents_ButPsmActivitySeen_KeepsMediumConfidence()
    {
        // BthPS3PSM does emit PsmPatchActivity (patched), but nothing else arrived at BthPS3 in the
        // observation window -- the newer filter driver actively rules out "silently disabled",
        // so confidence should stay Medium instead of dropping to Low.
        List<DiagnosticEventRecord> timeline =
        [
            Psm(BthPS3PsmEvents.PsmPatchActivity, new Dictionary<string, object?> { ["Patched"] = true })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.NoWirelessAttemptObserved, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Medium, verdict.Confidence);
        Assert.DoesNotContain("does not report Bluetooth filter activity", verdict.Explanation);
    }

    [Fact]
    public void OldFilterDriverWithLegacyRejection_StillProducesRemoteDeviceRejected()
    {
        // No RemoteConnectReceived (old BthPS3), no PsmPatchActivity (old BthPS3PSM), but the
        // classic rejection path still fires -- must still be diagnosable.
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteDeviceName),
            Bth(BthPS3Events.RemoteDeviceNotIdentified)
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.RemoteDeviceRejected, verdict.Code);
        Assert.Equal(DiagnosticConfidence.High, verdict.Confidence);
    }

    [Fact]
    public void Classify_ThrowsOnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(null!, []));
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify(AllPassed, null!));
    }

    [Fact]
    public void OldDriverNameLookupFailureOnly_ProducesRemoteDeviceUnknown_NotNoAttemptObserved()
    {
        // On an old BthPS3 (no RemoteConnectReceived) whose BthPS3_GetDeviceName call fails, the
        // *only* BthPS3 event in the whole run is this FailedWithNTStatus -- RemoteDeviceName is
        // never emitted on that path. Must still be recognized as "reached BthPS3", not "nothing
        // happened at all".
        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.FailedWithNTStatus,
                new Dictionary<string, object?> { ["FunctionName"] = "BthPS3_GetDeviceName" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.RemoteDeviceUnknown, verdict.Code);
        Assert.Single(verdict.Evidence);
    }

    [Fact]
    public void AddressCorrelation_IgnoresUnrelatedDeviceEvents_ProducesNoWirelessAttemptObserved()
    {
        // A second, unrelated device (a different paired controller) connects successfully during
        // the same observation window. Without address correlation this would be misread as our
        // candidate's own success.
        const ulong candidateAddress = 0xAABBCCDDEEFFUL;
        const ulong otherDeviceAddress = 0x001122334455UL;

        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = otherDeviceAddress }),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, new Dictionary<string, object?> { ["Address"] = otherDeviceAddress }),
            Bth(BthPS3Events.RemoteDeviceOnline, new Dictionary<string, object?> { ["Address"] = otherDeviceAddress })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline, candidateAddress);

        Assert.Equal(DiagnosticVerdictCode.NoWirelessAttemptObserved, verdict.Code);
    }

    [Fact]
    public void AddressCorrelation_MatchingDeviceAmongMultiple_StillProducesSuccess()
    {
        const ulong candidateAddress = 0xAABBCCDDEEFFUL;
        const ulong otherDeviceAddress = 0x001122334455UL;
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        List<DiagnosticEventRecord> timeline =
        [
            // Unrelated device's own (irrelevant) activity interleaved in the same window.
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = otherDeviceAddress }, baseTime),
            Bth(BthPS3Events.RemoteConnectReceived, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(5)),
            Bth(BthPS3Events.RemoteDeviceOnline, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(6)),
            DsHid("SomeDsHidMiniEvent", baseTime.AddMilliseconds(7),
                new Dictionary<string, object?> { ["Address"] = "AABBCCDDEEFF" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline, candidateAddress);

        Assert.Equal(DiagnosticVerdictCode.Success, verdict.Code);
    }

    [Fact]
    public void OnlineWithBluetoothInputStreamStarted_ProducesSuccess()
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
            DsHid(DsHidMiniEvents.BluetoothInputStreamStarted, baseTime.AddMilliseconds(6))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.Success, verdict.Code);
        Assert.Equal(DiagnosticConfidence.High, verdict.Confidence);
        Assert.Contains(verdict.Evidence, e => e.EventName == DsHidMiniEvents.BluetoothInputStreamStarted);
        Assert.Contains("started its Bluetooth input stream", verdict.Explanation);
    }

    [Fact]
    public void BluetoothInputStreamStartedBeforeOnline_DoesNotCountTowardSuccess()
    {
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;
        List<DiagnosticEventRecord> timeline =
        [
            DsHid(DsHidMiniEvents.BluetoothInputStreamStarted, baseTime.AddSeconds(-10)),
            Bth(BthPS3Events.RemoteConnectReceived, at: baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, at: baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, at: baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, at: baseTime.AddMilliseconds(5))
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
        Assert.Equal(DiagnosticConfidence.Medium, verdict.Confidence);
    }

    [Fact]
    public void AddressCorrelation_MatchingBluetoothInputStreamStarted_ProducesSuccess()
    {
        const ulong candidateAddress = 0xAABBCCDDEEFFUL;
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(5)),
            DsHid(DsHidMiniEvents.BluetoothInputStreamStarted, baseTime.AddMilliseconds(6),
                new Dictionary<string, object?> { ["Address"] = "001122334455" }),
            DsHid(DsHidMiniEvents.BluetoothInputStreamStarted, baseTime.AddMilliseconds(7),
                new Dictionary<string, object?> { ["Address"] = "AABBCCDDEEFF" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline, candidateAddress);

        Assert.Equal(DiagnosticVerdictCode.Success, verdict.Code);
        Assert.Contains(verdict.Evidence, e =>
            e.EventName == DsHidMiniEvents.BluetoothInputStreamStarted &&
            e.GetString("Address") == "AABBCCDDEEFF");
    }

    [Fact]
    public void AddressCorrelation_BluetoothInputStreamStartedForUnrelatedDevice_DoesNotCountTowardSuccess()
    {
        const ulong candidateAddress = 0xAABBCCDDEEFFUL;
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(5)),
            DsHid(DsHidMiniEvents.BluetoothInputStreamStarted, baseTime.AddMilliseconds(6),
                new Dictionary<string, object?> { ["Address"] = "001122334455" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline, candidateAddress);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
    }

    [Fact]
    public void AddressCorrelation_DsHidMiniActivityForUnrelatedDevice_DoesNotCountTowardSuccess()
    {
        const ulong candidateAddress = 0xAABBCCDDEEFFUL;
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        List<DiagnosticEventRecord> timeline =
        [
            Bth(BthPS3Events.RemoteConnectReceived, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime),
            Bth(BthPS3Events.RemoteDeviceIdentified, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(1)),
            Bth(BthPS3Events.ChildDeviceCreationSuccessful, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(2)),
            Bth(BthPS3Events.HidControlChannelConnected, at: baseTime.AddMilliseconds(3)),
            Bth(BthPS3Events.HidInterruptChannelConnected, at: baseTime.AddMilliseconds(4)),
            Bth(BthPS3Events.RemoteDeviceOnline, new Dictionary<string, object?> { ["Address"] = candidateAddress }, baseTime.AddMilliseconds(5)),
            // DsHidMini activity after online, but for a different device's hex address string.
            DsHid("SomeDsHidMiniEvent", baseTime.AddMilliseconds(6),
                new Dictionary<string, object?> { ["Address"] = "001122334455" })
        ];

        DiagnosticVerdict verdict = _classifier.Classify(AllPassed, timeline, candidateAddress);

        Assert.Equal(DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing, verdict.Code);
    }
}
