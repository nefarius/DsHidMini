namespace Nefarius.DsHidMini.ControlApp.Models.Onboarding;

/// <summary>
///     Tracks whether the mandatory first-run setup has been completed for this installation.
///     Bump <see cref="CurrentOnboardingVersion" /> whenever a future release changes a
///     safety-critical setup step, so returning users are walked through it again.
/// </summary>
public sealed class OnboardingCoordinator
{
    /// <summary>
    ///     Version of the setup flow. Persisted in <see cref="ApplicationConfiguration.CompletedOnboardingVersion" />.
    /// </summary>
    public const int CurrentOnboardingVersion = 1;

    /// <summary>
    ///     <see langword="true" /> once this installation has completed setup at
    ///     <see cref="CurrentOnboardingVersion" /> or later. A missing, corrupt, or older stored
    ///     value is always treated as incomplete.
    /// </summary>
    public bool IsCompleted => IsVersionCompleted(ApplicationConfiguration.Instance.CompletedOnboardingVersion);

    /// <summary>
    ///     Pure comparison extracted for testability: missing, corrupt (negative), or older values
    ///     are always treated as incomplete.
    /// </summary>
    public static bool IsVersionCompleted(int? completedVersion) =>
        completedVersion is { } completed && completed >= CurrentOnboardingVersion;

    /// <summary>
    ///     Records that setup finished successfully. Only call this after the wireless connection
    ///     was actually verified (or the user explicitly accepted a diagnosed blocker), never on a
    ///     bare window close.
    /// </summary>
    public void MarkCompleted()
    {
        ApplicationConfiguration.Instance.CompletedOnboardingVersion = CurrentOnboardingVersion;
        ApplicationConfiguration.Instance.Save();
    }

    /// <summary>
    ///     Clears completion so setup runs again on next launch. Used by the persistent
    ///     "Run setup again" action; must not touch profiles or any other settings.
    /// </summary>
    public void ResetCompletion()
    {
        ApplicationConfiguration.Instance.CompletedOnboardingVersion = null;
        ApplicationConfiguration.Instance.Save();
    }
}
