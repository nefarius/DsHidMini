namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Outcome of one <see cref="PreflightCheckId" />, in plain language so it can be shown directly
///     in onboarding and the diagnostic wizard without any further translation.
/// </summary>
/// <param name="Id">Which condition this describes.</param>
/// <param name="Passed"><see langword="true" /> if the condition is satisfied.</param>
/// <param name="Title">Short, literal description of the condition (e.g. "Bluetooth is on").</param>
/// <param name="Detail">One sentence of context or, when <paramref name="Passed" /> is false, what to do.</param>
/// <param name="CanAutoRepair">
///     <see langword="true" /> if <see cref="IPreflightProbe.TryAutoRepair" /> can fix this specific check.
/// </param>
public sealed record PreflightCheckResult(
    PreflightCheckId Id,
    bool Passed,
    string Title,
    string Detail,
    bool CanAutoRepair = false);
