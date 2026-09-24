using Nefarius.DsHidMini.ControlApp.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DriverCompatibilityTests
{
    [Theory]
    [InlineData("3.3.1187.0")]
    [InlineData("3.3.1228.0")]
    [InlineData("1.0.0.0")]
    [InlineData("06/21/2006,3.3.1187.0")]
    public void OldDriver_ExplainsVersionMismatch(string raw)
    {
        Version? parsed = DsHidMiniDriverCompatibility.TryParseDriverVersion(raw);
        Assert.NotNull(parsed);
        Assert.True(DsHidMiniDriverCompatibility.IsOlderThanIpcMinimum(parsed));

        string message = DsHidMiniDriverCompatibility.DescribeMissingIpcSlot(parsed);
        Assert.Contains(parsed.ToString(), message);
        Assert.Contains("predates IPC-slot support", message);
        Assert.DoesNotContain("ControlApp", message);
        Assert.Contains(DsHidMiniDriverCompatibility.MinimumIpcDriverVersion.ToString(), message);
        Assert.DoesNotContain("did not report an IPC slot", message);
    }

    [Theory]
    [InlineData("3.4")]
    [InlineData("3.4.0")]
    [InlineData("3.4.0.0")]
    [InlineData("3.14.0.2346")]
    public void CurrentDriver_KeepsSlotMessage(string raw)
    {
        Version? parsed = DsHidMiniDriverCompatibility.TryParseDriverVersion(raw);
        Assert.NotNull(parsed);
        Assert.False(DsHidMiniDriverCompatibility.IsOlderThanIpcMinimum(parsed));
        Assert.Equal(
            DsHidMiniDriverCompatibility.MissingIpcSlotMessage,
            DsHidMiniDriverCompatibility.DescribeMissingIpcSlot(parsed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    public void UnparsableDriverVersion_KeepsSlotMessage(string? raw)
    {
        Assert.Null(DsHidMiniDriverCompatibility.TryParseDriverVersion(raw));
        Assert.False(DsHidMiniDriverCompatibility.IsOlderThanIpcMinimum(null));
        Assert.Equal(
            DsHidMiniDriverCompatibility.MissingIpcSlotMessage,
            DsHidMiniDriverCompatibility.DescribeMissingIpcSlot(
                DsHidMiniDriverCompatibility.TryParseDriverVersion(raw)));
    }
}
