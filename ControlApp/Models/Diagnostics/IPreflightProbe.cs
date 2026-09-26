using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Runs the deterministic, hardware-independent checks that must pass before either the
///     mandatory first-run setup or the Bluetooth diagnostic wizard asks the user to touch the
///     controller.
/// </summary>
public interface IPreflightProbe
{
    /// <summary>
    ///     Runs every check. When <paramref name="candidateDevice" /> is <see langword="null" />, the
    ///     probe tries to auto-select the first wired, pairing-eligible DsHidMini device itself via
    ///     <see cref="FindEligibleUsbController" />.
    /// </summary>
    IReadOnlyList<PreflightCheckResult> Run(PnPDevice? candidateDevice = null);

    /// <summary>
    ///     Finds the first currently connected USB (wired) DsHidMini device that is eligible for
    ///     Bluetooth pairing (reports its own address, is not disabled for pairing).
    /// </summary>
    PnPDevice? FindEligibleUsbController();

    /// <summary>
    ///     Attempts to automatically fix the given check (currently only BthPS3 RawPDO/PSM settings).
    ///     Requires administrator privileges. Returns <see langword="false" /> if the check has no
    ///     automatic repair or the repair failed.
    /// </summary>
    bool TryAutoRepair(PreflightCheckId id);

    /// <summary>
    ///     Human-readable installed BthPS3 version (or "Not installed"/"Unknown"), as of the most
    ///     recent <see cref="Run" /> call. Used to label support bundles so an old-driver install can
    ///     be told apart from a genuinely missing signal.
    /// </summary>
    string BthPS3VersionDisplay { get; }
}
