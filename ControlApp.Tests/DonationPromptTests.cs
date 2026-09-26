using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Services;

using Newtonsoft.Json;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DonationPromptTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ShouldShow_OnlyWhenNotYetAcknowledged(bool hasAcknowledged, bool expected)
    {
        Assert.Equal(expected, DonationPromptPolicy.ShouldShow(hasAcknowledged));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ShouldAcknowledge_OnlyWhenUserTickedAlreadyDonated(bool ticked, bool expected)
    {
        Assert.Equal(expected, DonationPromptPolicy.ShouldAcknowledge(ticked));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ShouldOpenDonations_OnlyWhenUserAskedHow(bool showHow, bool expected)
    {
        Assert.Equal(expected, DonationPromptPolicy.ShouldOpenDonations(showHow));
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesHasAcknowledgedDonationDialog()
    {
        ApplicationConfiguration original = new() { HasAcknowledgedDonationDialog = true };

        string json = JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded = JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.True(loaded.HasAcknowledgedDonationDialog);
    }

    [Fact]
    public async Task ShowIfNeededAsync_WhenAlreadyAcknowledged_DoesNotPrompt()
    {
        ApplicationConfiguration config = new() { HasAcknowledgedDonationDialog = true };
        int promptCount = 0;
        DonationPromptService service = new(config: config, persist: () => { });
        service.ShowPromptOverride = () =>
        {
            promptCount++;
            return Task.FromResult(new DonationPromptResult(false, false));
        };

        await service.ShowIfNeededAsync();

        Assert.Equal(0, promptCount);
    }

    [Fact]
    public async Task ShowIfNeededAsync_AcknowledgedWithoutCheckbox_DoesNotPersist()
    {
        ApplicationConfiguration config = new();
        int persistCount = 0;
        string? openedUrl = null;
        DonationPromptService service = new(config: config, persist: () => persistCount++);
        service.ShowPromptOverride = () => Task.FromResult(new DonationPromptResult(false, false));
        service.OpenUrlOverride = url => openedUrl = url;

        await service.ShowIfNeededAsync();

        Assert.False(config.HasAcknowledgedDonationDialog);
        Assert.Equal(0, persistCount);
        Assert.Null(openedUrl);
    }

    [Fact]
    public async Task ShowIfNeededAsync_ShowHowAndCheckbox_OpensUrlAndPersists()
    {
        ApplicationConfiguration config = new();
        int persistCount = 0;
        string? openedUrl = null;
        DonationPromptService service = new(config: config, persist: () => persistCount++);
        service.ShowPromptOverride = () => Task.FromResult(new DonationPromptResult(true, true));
        service.OpenUrlOverride = url => openedUrl = url;

        await service.ShowIfNeededAsync();

        Assert.True(config.HasAcknowledgedDonationDialog);
        Assert.Equal(1, persistCount);
        Assert.Equal(DonationPromptPolicy.DonationsUrl, openedUrl);
    }
}
