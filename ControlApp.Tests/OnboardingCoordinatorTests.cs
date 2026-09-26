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
        Assert.False(OnboardingCoordinator.IsVersionCompleted(config.CompletedOnboardingVersion));
    }

    [Fact]
    public void IsFirstRunWizardRequired_DeveloperMode_SkipsEvenWhenIncomplete()
    {
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: true));
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(0, isDeveloperMode: true));
    }

    [Fact]
    public void IsFirstRunWizardRequired_ReleaseIncomplete_ShowsWizard()
    {
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(null, isDeveloperMode: false));
        Assert.True(OnboardingCoordinator.IsFirstRunWizardRequired(0, isDeveloperMode: false));
    }

    [Fact]
    public void IsFirstRunWizardRequired_ReleaseCompleted_DoesNotShowWizard()
    {
        Assert.False(OnboardingCoordinator.IsFirstRunWizardRequired(
            OnboardingCoordinator.CurrentOnboardingVersion,
            isDeveloperMode: false));
    }
}
