using System.Globalization;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <inheritdoc cref="IDiagnosticClassifier" />
/// <remarks>
///     Rules are evaluated in the same order the pair/unplug/connect sequence actually progresses
///     (preflight, then BthPS3PSM patching, then BthPS3 identification, then bus/PDO creation, then
///     the two HID L2CAP channels, then the BthPS3-to-DsHidMini handoff) so the first rule that
///     matches always corresponds to the earliest point where things actually stopped.
/// </remarks>
public sealed class BluetoothConnectionClassifier : IDiagnosticClassifier
{
    public DiagnosticVerdict Classify(
        IReadOnlyList<PreflightCheckResult> preflightResults,
        IReadOnlyList<DiagnosticEventRecord> timeline,
        ulong? candidateAddress = null)
    {
        ArgumentNullException.ThrowIfNull(preflightResults);
        ArgumentNullException.ThrowIfNull(timeline);

        PreflightCheckResult? firstFailedCheck = preflightResults.FirstOrDefault(r => !r.Passed);
        if (firstFailedCheck is not null)
        {
            return new DiagnosticVerdict(
                DiagnosticVerdictCode.PreflightBlocked,
                DiagnosticConfidence.High,
                "Nothing checked yet",
                $"{firstFailedCheck.Title}: {firstFailedCheck.Detail}",
                firstFailedCheck.CanAutoRepair
                    ? "Apply the automatic fix, then run this check again."
                    : firstFailedCheck.Detail,
                Array.Empty<DiagnosticEventRecord>());
        }

        List<DiagnosticEventRecord> bthPs3Psm = Filter(timeline, KnownDiagnosticProviders.BthPS3Psm);
        List<DiagnosticEventRecord> bthPs3 = Filter(timeline, KnownDiagnosticProviders.BthPS3);
        List<DiagnosticEventRecord> dsHidMini = Filter(timeline, KnownDiagnosticProviders.DsHidMini);

        // Correlate on the specific controller's address wherever the event template carries one,
        // so an unrelated device's activity during the same observation window (a second paired
        // controller, or a stale reconnect) is never mistaken for this run's outcome. Events with
        // no address field at all (HID channel connects carry no payload; BthPS3PSM never learns
        // the remote address before identification) are never excluded just for lacking it -- that
        // is an inherent driver-instrumentation gap, not a reason to discard the evidence.
        DiagnosticEventRecord? FindLastForCandidate(string eventName) =>
            FindLast(bthPs3, e => e.EventName == eventName && MatchesCandidateAddress(e, candidateAddress));

        DiagnosticEventRecord? nameLookupFailed = FindLast(bthPs3,
            e => e.EventName == BthPS3Events.FailedWithNTStatus &&
                 (e.GetString("FunctionName")?.Contains("GetDeviceName", StringComparison.OrdinalIgnoreCase) ?? false));

        // 'RemoteConnectReceived' is a newer, explicit "we got something" signal that may not exist
        // on an older BthPS3 install (older drivers never emit event 27 at all). Never require it:
        // fall back to whichever per-connection event from the classic 1-26 range shows up first,
        // so an older, fully-working driver is never misclassified as "nothing happened". The
        // name-lookup failure is included here too: on an old driver it is otherwise the *only*
        // event a failed lookup ever produces (BthPS3_GetDeviceName failing skips RemoteDeviceName
        // entirely), so omitting it would misclassify that failure as "no attempt observed".
        DiagnosticEventRecord? remoteConnectReceived = FindLastForCandidate(BthPS3Events.RemoteConnectReceived);
        DiagnosticEventRecord? reachedBthPs3 = remoteConnectReceived
                                                ?? nameLookupFailed
                                                ?? FindLastForCandidate(BthPS3Events.RemoteDeviceName)
                                                ?? FindLastForCandidate(BthPS3Events.RemoteDeviceIdentified)
                                                ?? FindLastForCandidate(BthPS3Events.RemoteDeviceNotIdentified)
                                                ?? FindLastForCandidate(BthPS3Events.ChildDeviceCreationSuccessful)
                                                ?? FindLastForCandidate(BthPS3Events.ChildDeviceCreationFailed)
                                                ?? FindLastForCandidate(BthPS3Events.L2CAPRemoteConnectFailed)
                                                ?? FindLastForCandidate(BthPS3Events.HidControlChannelConnected)
                                                ?? FindLastForCandidate(BthPS3Events.HidInterruptChannelConnected)
                                                ?? FindLastForCandidate(BthPS3Events.RemoteDeviceOnline);

        bool sawNewConnectSignal = remoteConnectReceived is not null;
        bool sawAnyPsmPatchActivity = bthPs3Psm.Any(e => e.EventName == BthPS3PsmEvents.PsmPatchActivity);

        if (reachedBthPs3 is null)
        {
            List<DiagnosticEventRecord> unpatchedAttempts = bthPs3Psm
                .Where(e => e.EventName == BthPS3PsmEvents.PsmPatchActivity && e.GetBool("Patched") == false)
                .ToList();

            if (unpatchedAttempts.Count > 0)
            {
                return new DiagnosticVerdict(
                    DiagnosticVerdictCode.PsmPatchMissing,
                    DiagnosticConfidence.High,
                    "Controller traffic reached the Bluetooth filter",
                    "The controller's connection request reached the Bluetooth filter driver, but PSM " +
                    "patching is turned off, so BthPS3 never saw the connection.",
                    "Turn PSM patching on (Settings > BthPS3 > Fix settings), then try again.",
                    unpatchedAttempts);
            }

            // Older BthPS3PSM builds never emit PsmPatchActivity, so a missing-patch condition
            // there is silent instead of diagnosable. Say so plainly rather than implying the
            // filter driver was ruled out.
            string filterVisibilityNote = sawAnyPsmPatchActivity
                ? string.Empty
                : " (This BthPS3 version does not report Bluetooth filter activity in detail, so a " +
                  "silently-disabled filter cannot be ruled out here.)";

            return new DiagnosticVerdict(
                DiagnosticVerdictCode.NoWirelessAttemptObserved,
                sawAnyPsmPatchActivity ? DiagnosticConfidence.Medium : DiagnosticConfidence.Low,
                "Pairing information was written to the controller",
                "No connection attempt from the controller was observed at all during this run." +
                filterVisibilityNote,
                "Make sure the controller is charged, then press the PS button once while it is unplugged.",
                Array.Empty<DiagnosticEventRecord>());
        }

        List<DiagnosticEventRecord> evidence = new() { reachedBthPs3 };
        if (sawNewConnectSignal && !ReferenceEquals(reachedBthPs3, remoteConnectReceived))
        {
            evidence.Add(remoteConnectReceived!);
        }

        DiagnosticEventRecord? notIdentified = FindLastForCandidate(BthPS3Events.RemoteDeviceNotIdentified);

        if (nameLookupFailed is not null && FindLastForCandidate(BthPS3Events.RemoteDeviceIdentified) is null)
        {
            // 'nameLookupFailed' may already be 'reachedBthPs3' itself (added to 'evidence' above)
            // when this is the only BthPS3 event in the whole run; avoid listing it twice.
            if (!ReferenceEquals(reachedBthPs3, nameLookupFailed))
            {
                evidence.Add(nameLookupFailed);
            }

            return new DiagnosticVerdict(
                DiagnosticVerdictCode.RemoteDeviceUnknown,
                DiagnosticConfidence.High,
                "The controller's connection attempt reached BthPS3",
                "BthPS3 received the connection but could not read the controller's name from the radio, " +
                "so it could not identify it.",
                "Re-pair the controller with USB connected, then try again.",
                evidence);
        }

        if (notIdentified is not null && FindLastForCandidate(BthPS3Events.RemoteDeviceIdentified) is null)
        {
            evidence.Add(notIdentified);
            return new DiagnosticVerdict(
                DiagnosticVerdictCode.RemoteDeviceRejected,
                DiagnosticConfidence.High,
                "The controller's connection attempt reached BthPS3",
                "BthPS3 read the controller's name but does not recognize it as a supported controller.",
                "Confirm this is a genuine or supported controller model, then consult the online documentation.",
                evidence);
        }

        DiagnosticEventRecord? identified = FindLastForCandidate(BthPS3Events.RemoteDeviceIdentified);
        DiagnosticEventRecord? childCreated = FindLastForCandidate(BthPS3Events.ChildDeviceCreationSuccessful);
        DiagnosticEventRecord? childFailed = FindLastForCandidate(BthPS3Events.ChildDeviceCreationFailed);

        if (childCreated is null)
        {
            // BthPS3 skips identification and PDO creation entirely when it finds an existing PDO
            // for this address (e.g. a connection reused from an earlier, not-yet-torn-down
            // session) -- that path is not a failure. Only call it ChildCreationFailed when BthPS3
            // *did* identify the controller in this run but then failed to create its device.
            if (identified is null)
            {
                return new DiagnosticVerdict(
                    DiagnosticVerdictCode.Inconclusive,
                    DiagnosticConfidence.Low,
                    "The controller's connection attempt reached BthPS3",
                    "BthPS3 did not report identifying the controller or creating its internal device, but " +
                    "also did not report a name-lookup or rejection failure. This can happen when a " +
                    "connection reuses an existing driver-side session instead of starting a new one.",
                    "Try again; if it keeps failing, export a diagnostic package.",
                    evidence);
            }

            evidence.Add(identified);

            if (childFailed is not null)
            {
                evidence.Add(childFailed);
            }

            return new DiagnosticVerdict(
                DiagnosticVerdictCode.ChildCreationFailed,
                DiagnosticConfidence.High,
                "BthPS3 identified the controller",
                "BthPS3 recognized the controller but failed to create its internal device for it.",
                "Restart Bluetooth (turn it off and on) and try again; if it keeps failing, export a diagnostic package.",
                evidence);
        }

        if (identified is not null)
        {
            evidence.Add(identified);
        }

        evidence.Add(childCreated);

        DiagnosticEventRecord? controlConnected = FindLastForCandidate(BthPS3Events.HidControlChannelConnected);
        DiagnosticEventRecord? interruptConnected = FindLastForCandidate(BthPS3Events.HidInterruptChannelConnected);
        DiagnosticEventRecord? online = FindLastForCandidate(BthPS3Events.RemoteDeviceOnline);

        if (online is null)
        {
            if (controlConnected is not null && interruptConnected is null)
            {
                evidence.Add(controlConnected);
                return new DiagnosticVerdict(
                    DiagnosticVerdictCode.ControlChannelOnly,
                    DiagnosticConfidence.High,
                    "The control channel connected",
                    "The first of the two required Bluetooth channels connected, but the second " +
                    "(interrupt) channel did not.",
                    "Move the controller closer to the PC and try again; if it keeps failing, export a diagnostic package.",
                    evidence);
            }

            if (controlConnected is null)
            {
                return new DiagnosticVerdict(
                    DiagnosticVerdictCode.InterruptChannelFailed,
                    DiagnosticConfidence.Medium,
                    "BthPS3 created its internal device for the controller",
                    "Neither Bluetooth channel confirmed as connected before the observation window ended.",
                    "Try again; if it keeps failing, export a diagnostic package.",
                    evidence);
            }

            evidence.Add(interruptConnected!);
            return new DiagnosticVerdict(
                DiagnosticVerdictCode.InterruptChannelFailed,
                DiagnosticConfidence.Medium,
                "The control channel connected",
                "Both channels reported connecting, but BthPS3 never marked the device fully online.",
                "Try again; if it keeps failing, export a diagnostic package.",
                evidence);
        }

        evidence.Add(online);

        // Correlate the handoff too: DsHidMini's own 'Address' property is the same 12-hex-digit
        // string (see driver/Ds3.c, driver/Device.c: "%02X%02X%02X%02X%02X%02X") as BthPS3's
        // numeric one, just formatted differently -- MatchesCandidateAddress() normalizes both.
        // Prefer the explicit wireless-ready milestone when the installed driver emits it;
        // older builds only produce generic DsHidMini activity after BthPS3 goes online.
        DiagnosticEventRecord? inputStreamStarted = dsHidMini.LastOrDefault(e =>
            e.EventName == DsHidMiniEvents.BluetoothInputStreamStarted &&
            e.Timestamp >= online.Timestamp &&
            MatchesCandidateAddress(e, candidateAddress));
        bool hasDsHidMiniActivity = inputStreamStarted is not null || dsHidMini.Any(e =>
            e.Timestamp >= online.Timestamp && MatchesCandidateAddress(e, candidateAddress));
        if (!hasDsHidMiniActivity)
        {
            // Zero DsHidMini events in the *entire* session (not just after BthPS3 went online) means
            // this could be a very old driver build whose ETW instrumentation itself is limited,
            // rather than proof that the handoff actually failed. Hedge accordingly instead of
            // pointing a finger with full confidence.
            bool everSawDsHidMiniActivity = dsHidMini.Count > 0;
            return new DiagnosticVerdict(
                DiagnosticVerdictCode.BthPs3OnlineDsHidMiniMissing,
                everSawDsHidMiniActivity ? DiagnosticConfidence.Medium : DiagnosticConfidence.Low,
                "BthPS3 reports the controller fully online",
                everSawDsHidMiniActivity
                    ? "BthPS3 finished its part of the connection successfully, but no DsHidMini driver " +
                      "activity was observed afterward. This points at the DsHidMini driver or HID stack, not BthPS3."
                    : "BthPS3 finished its part of the connection successfully, but no DsHidMini driver " +
                      "activity was observed at any point during this run. This may point at the DsHidMini " +
                      "driver or HID stack, or this DsHidMini version may not report detailed activity.",
                "Update or reinstall the DsHidMini driver, then try again.",
                evidence);
        }

        if (inputStreamStarted is not null)
        {
            evidence.Add(inputStreamStarted);
        }

        return new DiagnosticVerdict(
            DiagnosticVerdictCode.Success,
            DiagnosticConfidence.High,
            "The controller connected over Bluetooth",
            inputStreamStarted is not null
                ? "BthPS3 reports the controller fully online, and DsHidMini started its Bluetooth input stream."
                : "BthPS3 reports the controller fully online, and DsHidMini driver activity followed.",
            "No action needed. The controller should now behave normally over Bluetooth.",
            evidence);
    }

    private static List<DiagnosticEventRecord> Filter(IReadOnlyList<DiagnosticEventRecord> timeline, Guid providerGuid)
    {
        return timeline.Where(e => e.ProviderGuid == providerGuid).ToList();
    }

    /// <summary>
    ///     True when <paramref name="candidateAddress" /> is <see langword="null" /> (no address to
    ///     correlate against), when <paramref name="record" />'s template has no "Address" property
    ///     at all (cannot correlate), or when the two addresses match. BthPS3 reports "Address" as a
    ///     raw <c>UInt64</c>; DsHidMini reports it as a 12-hex-digit <c>AnsiString</c> in the same
    ///     byte order (see driver/Ds3.c, driver/Device.c) -- both are normalized to <c>ulong</c> here.
    /// </summary>
    private static bool MatchesCandidateAddress(DiagnosticEventRecord record, ulong? candidateAddress)
    {
        if (candidateAddress is null)
        {
            return true;
        }

        if (!record.Properties.TryGetValue("Address", out object? raw) || raw is null)
        {
            return true;
        }

        if (raw is string hex)
        {
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong parsed))
            {
                return true; // Unparsable string: cannot correlate, don't exclude on that basis.
            }

            return parsed == candidateAddress.Value;
        }

        ulong? numeric = record.GetUInt64("Address");
        return numeric is null || numeric.Value == candidateAddress.Value;
    }

    private static DiagnosticEventRecord? FindLast(IReadOnlyList<DiagnosticEventRecord> events, string eventName)
    {
        return FindLast(events, e => e.EventName == eventName);
    }

    private static DiagnosticEventRecord? FindLast(
        IReadOnlyList<DiagnosticEventRecord> events,
        Func<DiagnosticEventRecord, bool> predicate)
    {
        for (int i = events.Count - 1; i >= 0; i--)
        {
            if (predicate(events[i]))
            {
                return events[i];
            }
        }

        return null;
    }
}
