namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Every outcome the Bluetooth diagnostic classifier can produce, ordered roughly by where in
///     the pair/unplug/connect sequence progress stopped. See the Bluetooth Diagnostic Assistant
///     plan for the rationale behind each code.
/// </summary>
public enum DiagnosticVerdictCode
{
    /// <summary>A preflight condition failed; the physical steps were never attempted.</summary>
    PreflightBlocked,

    /// <summary>The driver could not write the host address, or could not read it back.</summary>
    PairingWriteFailed,

    /// <summary>Nothing reached BthPS3 or its filter during the observation window.</summary>
    NoWirelessAttemptObserved,

    /// <summary>Traffic reached the BthPS3PSM filter but PSM patching is disabled.</summary>
    PsmPatchMissing,

    /// <summary>BthPS3 received a connect but could not resolve the remote device's name.</summary>
    RemoteDeviceUnknown,

    /// <summary>BthPS3 resolved a name it does not recognize as a supported controller.</summary>
    RemoteDeviceRejected,

    /// <summary>BthPS3 identified the device but failed to create its bus child device.</summary>
    ChildCreationFailed,

    /// <summary>Only the HID control L2CAP channel connected; the interrupt channel did not.</summary>
    ControlChannelOnly,

    /// <summary>The interrupt channel attempt failed after the control channel connected.</summary>
    InterruptChannelFailed,

    /// <summary>BthPS3 reports the device fully online, but DsHidMini shows no corresponding activity.</summary>
    BthPs3OnlineDsHidMiniMissing,

    /// <summary>BthPS3 is online and DsHidMini activity was observed afterward.</summary>
    Success,

    /// <summary>Evidence does not clearly match any known pattern.</summary>
    Inconclusive
}
