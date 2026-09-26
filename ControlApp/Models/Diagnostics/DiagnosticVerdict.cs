namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     One deterministic classification confidence level. Lower confidence should visibly hedge
///     the user-facing explanation and steer toward exporting a support bundle instead of a firm
///     claim.
/// </summary>
public enum DiagnosticConfidence
{
    Low,
    Medium,
    High
}

/// <summary>
///     Result of classifying one diagnostic run: what happened, where it stopped, why (in plain
///     language), what to do next, and the evidence backing the call.
/// </summary>
/// <param name="Code">Which pattern matched.</param>
/// <param name="Confidence">How strongly the evidence supports <paramref name="Code" />.</param>
/// <param name="LastSuccessfulMilestone">Plain-language description of the last confirmed-good step.</param>
/// <param name="Explanation">One or two sentences explaining the verdict without jargon.</param>
/// <param name="RemediationAction">The single next action to suggest to the user.</param>
/// <param name="Evidence">The events (if any) that drove this verdict, for the technical timeline.</param>
public sealed record DiagnosticVerdict(
    DiagnosticVerdictCode Code,
    DiagnosticConfidence Confidence,
    string LastSuccessfulMilestone,
    string Explanation,
    string RemediationAction,
    IReadOnlyList<DiagnosticEventRecord> Evidence);
