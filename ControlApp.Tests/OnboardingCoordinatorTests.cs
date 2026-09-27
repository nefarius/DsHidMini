using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Onboarding;

using Newtonsoft.Json;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class OnboardingCoordinatorTests
{
    [Fact]
    public void IsVersionCompleted_NullValue_IsIncomplete()
    {
        Assert.False(OnboardingCoordinator.IsVersionCompleted(null));
    }

    [Fact]
    public void IsVersionCompleted_OlderVersion_IsIncomplete()
    {
        Assert.False(OnboardingCoordinator.IsVersionCompleted(OnboardingCoordinator.CurrentOnboardingVersion - 1));
    }

    [Fact]
    public void IsVersionCompleted_CurrentVersion_IsComplete()
    {
        Assert.True(OnboardingCoordinator.IsVersionCompleted(OnboardingCoordinator.CurrentOnboardingVersion));
    }

    [Fact]
    public void IsVersionCompleted_NewerVersion_IsComplete()
    {
        Assert.True(OnboardingCoordinator.IsVersionCompleted(OnboardingCoordinator.CurrentOnboardingVersion + 1));
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesCompletedOnboardingVersion()
    {
        ApplicationConfiguration original = new() { CompletedOnboardingVersion = 3 };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.CompletedOnboardingVersion);
    }

    [Fact]
    public void ApplicationConfiguration_Default_HasNoCompletedOnboardingVersion()
    {
        ApplicationConfiguration config = new();

        Assert.Null(config.CompletedOnboardingVersion);
        Assert.Null(config.SkippedOnboardingVersion);
        Assert.False(OnboardingCoordinator.IsVersionCompleted(config.CompletedOnboardingVersion));
        Assert.False(OnboardingCoordinator.IsVersionSkipped(config.SkippedOnboardingVersion));
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesSkippedOnboardingVersion()
    {
        ApplicationConfiguration original = new() { SkippedOnboardingVersion = 2 };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.SkippedOnboardingVersion);
        Assert.Null(loaded.CompletedOnboardingVersion);
    }

    [Fact]
    public void IsVersionSkipped_NullOrOlder_IsNotSkipped()
    {
        Assert.False(OnboardingCoordinator.IsVersionSkipped(null));
        Assert.False(OnboardingCoordinator.IsVersionSkipped(OnboardingCoordinator.CurrentOnboardingVersion - 1));
    }

    [Fact]
    public void IsVersionSkipped_CurrentOrNewer_IsSkipped()
    {
        Assert.True(OnboardingCoordinator.IsVersionSkipped(OnboardingCoordinator.CurrentOnboardingVersion));
        Assert.True(OnboardingCoordinator.IsVersionSkipped(OnboardingCoordinator.CurrentOnboardingVersion + 1));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(0, null, false)]
    [InlineData(null, 0, false)]
    [InlineData(OnboardingCoordinator.CurrentOnboardingVersion, null, true)]
    [InlineData(null, OnboardingCoordinator.CurrentOnboardingVersion, true)]
    [InlineData(OnboardingCoordinator.CurrentOnboardingVersion, OnboardingCoordinator.CurrentOnboardingVersion, true)]
    [InlineData(OnboardingCoordinator.CurrentOnboardingVersion + 1, 0, true)]
    [InlineData(0, OnboardingCoordinator.CurrentOnboardingVersion + 1, true)]
    public void IsOnboardingSatisfied_CompletionOrCurrentSkip(int? completed, int? skipped, bool expected)
    {
        Assert.Equal(expected, OnboardingCoordinator.IsOnboardingSatisfied(completed, skipped));
    }

    [Fact]
    public void ApplySkipped_DoesNotMarkCompleted()
    {
        ApplicationConfiguration config = new();

        OnboardingCoordinator.ApplySkipped(config);

        Assert.Null(config.CompletedOnboardingVersion);
        Assert.Equal(OnboardingCoordinator.CurrentOnboardingVersion, config.SkippedOnboardingVersion);
        Assert.False(OnboardingCoordinator.IsVersionCompleted(config.CompletedOnboardingVersion));
        Assert.True(OnboardingCoordinator.IsOnboardingSatisfied(
            config.CompletedOnboardingVersion,
            config.SkippedOnboardingVersion));
    }

    [Fact]
    public void ApplyCompleted_ClearsStaleSkippedState()
    {
        ApplicationConfiguration config = new()
        {
            SkippedOnboardingVersion = OnboardingCoordinator.CurrentOnboardingVersion
        };

        OnboardingCoordinator.ApplyCompleted(config);

        Assert.Equal(OnboardingCoordinator.CurrentOnboardingVersion, config.CompletedOnboardingVersion);
        Assert.Null(config.SkippedOnboardingVersion);
        Assert.True(OnboardingCoordinator.IsVersionCompleted(config.CompletedOnboardingVersion));
        Assert.False(OnboardingCoordinator.IsVersionSkipped(config.SkippedOnboardingVersion));
        Assert.True(OnboardingCoordinator.IsOnboardingSatisfied(
            config.CompletedOnboardingVersion,
            config.SkippedOnboardingVersion));
    }

    [Fact]
    public void ApplyReset_ClearsCompletedAndSkipped()
    {
        ApplicationConfiguration config = new()
        {
            CompletedOnboardingVersion = OnboardingCoordinator.CurrentOnboardingVersion,
            SkippedOnboardingVersion = OnboardingCoordinator.CurrentOnboardingVersion
        };

        OnboardingCoordinator.ApplyReset(config);

        Assert.Null(config.CompletedOnboardingVersion);
        Assert.Null(config.SkippedOnboardingVersion);
        Assert.False(OnboardingCoordinator.IsOnboardingSatisfied(
            config.CompletedOnboardingVersion,
            config.SkippedOnboardingVersion));
    }

    [Fact]
    public void IsFirstRunWizardRequired_DeveloperMode_SkipsEvenWhenIncomplete()
    {
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: true));
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(0, isDeveloperMode: true));
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: true, skippedVersion: 0));
    }

    [Fact]
    public void IsFirstRunWizardRequired_ReleaseIncomplete_ShowsWizard()
    {
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: false));
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(0, isDeveloperMode: false));
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: false, skippedVersion: 0));
    }

    [Fact]
    public void IsFirstRunWizardRequired_ReleaseCompleted_DoesNotShowWizard()
    {
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(
            OnboardingCoordinator.CurrentOnboardingVersion,
            isDeveloperMode: false));
    }

    [Fact]
    public void IsFirstRunWizardRequired_ReleaseSkipped_DoesNotShowWizard()
    {
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(
            completedVersion: null,
            isDeveloperMode: false,
            skippedVersion: OnboardingCoordinator.CurrentOnboardingVersion));
    }

    [Fact]
    public void IsFirstRunWizardRequired_OlderSkip_ShowsWizardAgain()
    {
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(
            completedVersion: null,
            isDeveloperMode: false,
            skippedVersion: OnboardingCoordinator.CurrentOnboardingVersion - 1));
    }

    [Fact]
    public void RequiresElevationToStart_OnlyWhenWizardWillShowAndProcessIsUnelevated()
    {
        Assert.True(OnboardingCoordinator.RequiresElevationToStart(shouldShowWizard: true, isElevated: false));
        Assert.False(OnboardingCoordinator.RequiresElevationToStart(shouldShowWizard: true, isElevated: true));
        Assert.False(OnboardingCoordinator.RequiresElevationToStart(shouldShowWizard: false, isElevated: false));
    }
}
