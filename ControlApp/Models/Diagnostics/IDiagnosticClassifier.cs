namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Turns preflight results plus a captured event timeline into one deterministic
///     <see cref="DiagnosticVerdict" />. Implementations must not depend on any live hardware or
///     ETW session so they can be exercised with fixture timelines in tests.
/// </summary>
public interface IDiagnosticClassifier
{
    /// <param name="preflightResults">Every preflight check that was run.</param>
    /// <param name="timeline">Every structured event captured during this run, in capture order.</param>
    /// <param name="candidateAddress">
    ///     The Bluetooth address of the controller being diagnosed, if known. When provided,
    ///     evidence lookup correlates on this address wherever the underlying driver event carries
    ///     one, so an unrelated device's activity during the same observation window (e.g. a second
    ///     paired controller, or a stale reconnect) is not mistaken for this run's outcome. Events
    ///     whose template carries no address at all (an inherent driver-instrumentation limitation
    ///     for a few event types) are never excluded just because they lack the field.
    /// </param>
    DiagnosticVerdict Classify(
        IReadOnlyList<PreflightCheckResult> preflightResults,
        IReadOnlyList<DiagnosticEventRecord> timeline,
        ulong? candidateAddress = null);
}
