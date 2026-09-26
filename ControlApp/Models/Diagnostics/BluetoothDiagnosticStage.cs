namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Where a <see cref="BluetoothDiagnosticSession" /> run currently is. The UI drives its
///     instructions directly off this value so the wizard never needs a manual "Next" button
///     except where a physical action from the user is required.
/// </summary>
public enum BluetoothDiagnosticStage
{
    Idle,
    RunningPreflight,
    PreflightBlocked,
    Pairing,
    PairingFailed,
    WaitingForUnplug,
    WaitingForWirelessAttempt,
    Classifying,
    Completed,
    Cancelled,
    Faulted
}
