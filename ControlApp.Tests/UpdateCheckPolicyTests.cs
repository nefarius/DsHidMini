using System.Text.Json;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Util.Web;
using Nefarius.DsHidMini.ControlApp.Services;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class UpdateCheckPolicyTests
{
    [Theory]
    [InlineData(false, "2026-09-08", null, false)]
    [InlineData(true, "2026-09-08", null, true)]
    [InlineData(true, "2026-09-08", "2026-09-08", false)]
    [InlineData(true, "2026-09-09", "2026-09-08", true)]
    public void ShouldPerformNetworkCheck_RespectsEnablementAndLocalCalendarDay(
        bool isEnabled,
        string todayText,
        string? lastCheckText,
        bool expected)
    {
        DateOnly today = DateOnly.Parse(todayText);
        DateOnly? lastCheck = lastCheckText is null ? null : DateOnly.Parse(lastCheckText);

        Assert.Equal(expected, UpdateCheckPolicy.ShouldPerformNetworkCheck(isEnabled, today, lastCheck));
    }

    [Theory]
    [InlineData(false, "2026-09-08", "2026-09-08")]
    [InlineData(true, "2026-09-08", "2026-09-08")]
    public void ShouldPerformNetworkCheck_ManualIgnoresEnablementAndLastCheckDate(
        bool isEnabled,
        string todayText,
        string lastCheckText)
    {
        DateOnly today = DateOnly.Parse(todayText);
        DateOnly lastCheck = DateOnly.Parse(lastCheckText);

        Assert.True(UpdateCheckPolicy.ShouldPerformNetworkCheck(
            isEnabled,
            today,
            lastCheck,
            ignoreLastCheckDate: true));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void ShouldReuseInFlightCheck_ManualDoesNotReuseGatedCheck(
        bool requestIgnoresLastCheckDate,
        bool inFlightIgnoresLastCheckDate,
        bool expected)
    {
        Assert.Equal(
            expected,
            UpdateCheckPolicy.ShouldReuseInFlightCheck(
                requestIgnoresLastCheckDate,
                inFlightIgnoresLastCheckDate));
    }

    [Theory]
    [InlineData("3.4.2131.0", "3.0.0.0", true)]
    [InlineData("3.4.2131.0", "3.4.2131.0", false)]
    [InlineData("3.0.0.0", "3.4.2131.0", false)]
    public void IsRemoteNewer_ComparesParsedFileVersions(string remoteText, string localText, bool expected)
    {
        Assert.True(UpdateCheckPolicy.TryParseFileVersion(remoteText, out Version? remote));
        Assert.True(UpdateCheckPolicy.TryParseFileVersion(localText, out Version? local));
        Assert.NotNull(remote);
        Assert.NotNull(local);

        Assert.Equal(expected, UpdateCheckPolicy.IsRemoteNewer(remote, local));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("v3.4.2131.0")]
    [InlineData("3.4.2131.0+f4a6a3ca02d16f45d861ffa808ae23919f14592b")]
    public void TryParseFileVersion_RejectsMissingAndMalformedValues(string? value)
    {
        Assert.False(UpdateCheckPolicy.TryParseFileVersion(value, out Version? version));
        Assert.Null(version);
    }

    [Fact]
    public void ArtifactMetaData_DeserializesBuildbotFileVersionPayload()
    {
        const string json =
            """{"FileVersion":"3.4.2131.0","ProductVersion":"3.4.2131.0\u002Bf4a6a3ca02d16f45d861ffa808ae23919f14592b"}""";

        ArtifactMetaData? metadata = JsonSerializer.Deserialize<ArtifactMetaData>(json);

        Assert.NotNull(metadata);
        Assert.Equal("3.4.2131.0", metadata.FileVersion);
        Assert.Equal("3.4.2131.0+f4a6a3ca02d16f45d861ffa808ae23919f14592b", metadata.ProductVersion);
        Assert.True(UpdateCheckPolicy.TryParseFileVersion(metadata.FileVersion, out Version? version));
        Assert.Equal(new Version(3, 4, 2131, 0), version);
    }

    [Fact]
    public void TryGetLocalFileVersion_RejectsMissingProcessPath()
    {
        Assert.False(ControlAppUpdateService.TryGetLocalFileVersion(null, out Version? version));
        Assert.Null(version);
    }
}
