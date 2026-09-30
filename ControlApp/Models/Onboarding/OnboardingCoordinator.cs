using Microsoft.Extensions.Hosting;

using Nefarius.DsHidMini.ControlApp.Models;

namespace Nefarius.DsHidMini.ControlApp.Models.Onboarding;

/// <summary>
///     Tracks whether the first-run setup has been completed or deliberately skipped for
///     this installation. Bump <see cref="CurrentOnboardingVersion" /> whenever a future
///     release changes a safety-critical setup step, so returning users are walked through
///     it again.
/// </summary>
public sealed class OnboardingCoordinator
{
    /// <summary>
    ///     Version of the setup flow. Persisted in
    ///     <see cref="ApplicationConfiguration.CompletedOnboardingVersion" /> or
    ///     <see cref="ApplicationConfiguration.SkippedOnboardingVersion" />.
    /// </summary>
    public const int CurrentOnboardingVersion = 1;

    public OnboardingCoordinator(IHostEnvironment? hostEnvironment = null)
    {
        IsDeveloperMode = DetectDeveloperMode(hostEnvironment);
    }

    /// <summary>
    ///     Debug builds, or a host environment named Development, skip the first-run wizard
    ///     so local work is not gated by Bluetooth setup.
    /// </summary>
    public bool IsDeveloperMode { get; }

    /// <summary>
    ///     <see langword="true" /> once this installation has completed setup at
    ///     <see cref="CurrentOnboardingVersion" /> or later. A missing, corrupt, or older stored
    ///     value is always treated as incomplete.
    /// </summary>
    public bool IsCompleted => IsVersionCompleted(ApplicationConfiguration.Instance.CompletedOnboardingVersion);

    /// <summary>
    ///     <see langword="true" /> once this installation skipped setup at
    ///     <see cref="CurrentOnboardingVersion" /> or later, either as a USB-only user or
    ///     after accepting the unsupported-configuration warning.
    /// </summary>
    public bool IsSkipped => IsVersionSkipped(ApplicationConfiguration.Instance.SkippedOnboardingVersion);

    /// <summary>
    ///     <see langword="true" /> when either a successful completion or an accepted skip
    ///     satisfies the current setup version, so startup may continue to the main window.
    /// </summary>
    public bool IsSatisfied => IsOnboardingSatisfied(
        ApplicationConfiguration.Instance.CompletedOnboardingVersion,
        ApplicationConfiguration.Instance.SkippedOnboardingVersion);

    /// <summary>
    ///     Whether startup should show the first-run wizard. Developer mode never gates launch
    ///     and does not persist completion or skip.
    /// </summary>
    public bool ShouldShowFirstRunWizard =>
        IsFirstRunWizardRequired(
            ApplicationConfiguration.Instance.CompletedOnboardingVersion,
            IsDeveloperMode,
            ApplicationConfiguration.Instance.SkippedOnboardingVersion);

    /// <summary>
    ///     Pure comparison extracted for testability: missing, corrupt (negative), or older values
    ///     are always treated as incomplete.
    /// </summary>
    public static bool IsVersionCompleted(int? completedVersion) =>
        IsCurrentVersionSatisfied(completedVersion);

    /// <summary>
    ///     Pure comparison extracted for testability: missing, corrupt (negative), or older values
    ///     are always treated as not skipped for the current setup version.
    /// </summary>
    public static bool IsVersionSkipped(int? skippedVersion) =>
        IsCurrentVersionSatisfied(skippedVersion);

    /// <summary>
    ///     Successful completion or an accepted skip at the current version both satisfy
    ///     first-run gating. Completion wins when both are recorded.
    /// </summary>
    public static bool IsOnboardingSatisfied(int? completedVersion, int? skippedVersion) =>
        IsVersionCompleted(completedVersion) || IsVersionSkipped(skippedVersion);

    /// <summary>
    ///     Debug compilation is always developer mode. Release builds honor a host
    ///     environment named Development only.
    /// </summary>
    public static bool DetectDeveloperMode(IHostEnvironment? hostEnvironment)
    {
#if DEBUG
        return true;
#else
        return hostEnvironment?.IsDevelopment() == true;
#endif
    }

    public static bool IsFirstRunWizardRequired(
        int? completedVersion,
        bool isDeveloperMode,
        int? skippedVersion = null) =>
        !isDeveloperMode && !IsOnboardingSatisfied(completedVersion, skippedVersion);

    /// <summary>
    ///     First-run setup always starts an ETW session, so an unelevated process must
    ///     self-elevate before the wizard is shown.
    /// </summary>
    public static bool RequiresElevationToStart(bool shouldShowWizard, bool isElevated) =>
        shouldShowWizard && !isElevated;

    /// <summary>
    ///     Records a successful setup. Clears any leftover skipped state so a later verified
    ///     run is not still reported as skipped. Only call this after the wireless connection
    ///     was actually verified (or the user explicitly accepted a diagnosed blocker), never
    ///     on a bare window close.
    /// </summary>
    public void MarkCompleted()
    {
        ApplyCompleted(ApplicationConfiguration.Instance);
        ApplicationConfiguration.Instance.Save();
    }

    /// <summary>
    ///     Records that the user skipped pairing for the current setup version, either as a
    ///     USB-only user or after accepting the Bluetooth bypass warning. Does not mark
    ///     setup as completed.
    /// </summary>
    public void MarkSkipped()
    {
        ApplySkipped(ApplicationConfiguration.Instance);
        ApplicationConfiguration.Instance.Save();
    }

    /// <summary>
    ///     Clears completion and skip so setup runs again on next launch. Used by the
    ///     persistent "Run setup again" action; must not touch profiles or any other settings.
    /// </summary>
    public void ResetCompletion()
    {
        ApplyReset(ApplicationConfiguration.Instance);
        ApplicationConfiguration.Instance.Save();
    }

    /// <summary>
    ///     Testable mutation of completion state without touching disk.
    /// </summary>
    public static void ApplyCompleted(ApplicationConfiguration config)
    {
        config.CompletedOnboardingVersion = CurrentOnboardingVersion;
        config.SkippedOnboardingVersion = null;
    }

    /// <summary>
    ///     Testable mutation of skipped state without touching disk.
    /// </summary>
    public static void ApplySkipped(ApplicationConfiguration config)
    {
        config.SkippedOnboardingVersion = CurrentOnboardingVersion;
    }

    /// <summary>
    ///     Testable reset of both completion and skip without touching disk.
    /// </summary>
    public static void ApplyReset(ApplicationConfiguration config)
    {
        config.CompletedOnboardingVersion = null;
        config.SkippedOnboardingVersion = null;
    }

    private static bool IsCurrentVersionSatisfied(int? storedVersion) =>
        storedVersion is { } version && version >= CurrentOnboardingVersion;
}
