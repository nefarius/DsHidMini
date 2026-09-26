namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Turns preflight results plus a captured event timeline into one deterministic
///     <see cref="DiagnosticVerdict" />. Implementations must not depend on any live hardware or
///     ETW session so they can be exercised with fixture timelines in tests.
/// </summary>
public interface IDiagnosticClassifier
{
    DiagnosticVerdict Classify(
        IReadOnlyList<PreflightCheckResult> preflightResults,
        IReadOnlyList<DiagnosticEventRecord> timeline);
}
