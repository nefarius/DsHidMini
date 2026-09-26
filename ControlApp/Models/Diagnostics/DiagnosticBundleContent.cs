namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Everything needed to write one diagnostic support bundle.
/// </summary>
/// <param name="Verdict">The final classification, or <see langword="null" /> if the run never reached one.</param>
/// <param name="PreflightResults">Every preflight check that was run.</param>
/// <param name="Timeline">Every structured event captured during this run, in capture order.</param>
/// <param name="ControlAppVersion">ControlApp's own version string, for support triage.</param>
/// <param name="DsHidMiniDriverVersion">Installed DsHidMini driver version, if known.</param>
/// <param name="BthPS3Version">Installed BthPS3 version, if known.</param>
/// <param name="StartedAt">When this diagnostic run started.</param>
/// <param name="FinishedAt">When this diagnostic run finished (or was cancelled).</param>
public sealed record DiagnosticBundleContent(
    DiagnosticVerdict? Verdict,
    IReadOnlyList<PreflightCheckResult> PreflightResults,
    IReadOnlyList<DiagnosticEventRecord> Timeline,
    string ControlAppVersion,
    string? DsHidMiniDriverVersion,
    string? BthPS3Version,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt);
