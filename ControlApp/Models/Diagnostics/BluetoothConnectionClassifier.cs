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
        IReadOnlyList<DiagnosticEventRecord> timeline)
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

        // 'RemoteConnectReceived' is a newer, explicit "we got something" signal that may not exist
        // on an older BthPS3 install (older drivers never emit event 27 at all). Never require it:
        // fall back to whichever per-connection event from the classic 1-26 range shows up first,
        // so an older, fully-working driver is never misclassified as "nothing happened".
        DiagnosticEventRecord? remoteConnectReceived = FindLast(bthPs3, BthPS3Events.RemoteConnectReceived);
        DiagnosticEventRecord? reachedBthPs3 = remoteConnectReceived
                                                ?? FindLast(bthPs3, BthPS3Events.RemoteDeviceName)
                                                ?? FindLast(bthPs3, BthPS3Events.RemoteDeviceIdentified)
                                                ?? FindLast(bthPs3, BthPS3Events.RemoteDeviceNotIdentified)
                                                ?? FindLast(bthPs3, BthPS3Events.ChildDeviceCreationSuccessful)
                                                ?? FindLast(bthPs3, BthPS3Events.ChildDeviceCreationFailed)
                                                ?? FindLast(bthPs3, BthPS3Events.L2CAPRemoteConnectFailed)
                                                ?? FindLast(bthPs3, BthPS3Events.HidControlChannelConnected)
                                                ?? FindLast(bthPs3, BthPS3Events.HidInterruptChannelConnected)
                                                ?? FindLast(bthPs3, BthPS3Events.RemoteDeviceOnline);

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

        DiagnosticEventRecord? notIdentified = FindLast(bthPs3, BthPS3Events.RemoteDeviceNotIdentified);
        DiagnosticEventRecord? nameLookupFailed = FindLast(bthPs3,
            e => e.EventName == BthPS3Events.FailedWithNTStatus &&
                 (e.GetString("FunctionName")?.Contains("GetDeviceName", StringComparison.OrdinalIgnoreCase) ?? false));

        if (nameLookupFailed is not null && FindLast(bthPs3, BthPS3Events.RemoteDeviceIdentified) is null)
        {
            evidence.Add(nameLookupFailed);
            return new DiagnosticVerdict(
                DiagnosticVerdictCode.RemoteDeviceUnknown,
                DiagnosticConfidence.High,
                "The controller's connection attempt reached BthPS3",
                "BthPS3 received the connection but could not read the controller's name from the radio, " +
                "so it could not identify it.",
                "Re-pair the controller with USB connected, then try again.",
                evidence);
        }

        if (notIdentified is not null && FindLast(bthPs3, BthPS3Events.RemoteDeviceIdentified) is null)
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

        DiagnosticEventRecord? identified = FindLast(bthPs3, BthPS3Events.RemoteDeviceIdentified);
        DiagnosticEventRecord? childCreated = FindLast(bthPs3, BthPS3Events.ChildDeviceCreationSuccessful);
        DiagnosticEventRecord? childFailed = FindLast(bthPs3, BthPS3Events.ChildDeviceCreationFailed);

        if (identified is not null)
        {
            evidence.Add(identified);
        }

        if (childCreated is null)
        {
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

        evidence.Add(childCreated);

        DiagnosticEventRecord? controlConnected = FindLast(bthPs3, BthPS3Events.HidControlChannelConnected);
        DiagnosticEventRecord? interruptConnected = FindLast(bthPs3, BthPS3Events.HidInterruptChannelConnected);
        DiagnosticEventRecord? online = FindLast(bthPs3, BthPS3Events.RemoteDeviceOnline);

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

        bool hasDsHidMiniActivity = dsHidMini.Any(e => e.Timestamp >= online.Timestamp);
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

        return new DiagnosticVerdict(
            DiagnosticVerdictCode.Success,
            DiagnosticConfidence.High,
            "The controller connected over Bluetooth",
            "BthPS3 reports the controller fully online, and DsHidMini driver activity followed.",
            "No action needed. The controller should now behave normally over Bluetooth.",
            evidence);
    }

    private static List<DiagnosticEventRecord> Filter(IReadOnlyList<DiagnosticEventRecord> timeline, Guid providerGuid)
    {
        return timeline.Where(e => e.ProviderGuid == providerGuid).ToList();
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
