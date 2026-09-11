using Nefarius.DsHidMini.ControlApp.Models.Util;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DefenderBtModeSwitcherTests
{
    [Theory]
    [InlineData((ushort)0x054C, (ushort)0x05C4, (ushort)0x0221, true)]
    [InlineData((ushort)0x054C, (ushort)0x05C4, (ushort)0x0100, false)]
    [InlineData((ushort)0x054C, (ushort)0x0268, (ushort)0x0221, false)]
    [InlineData((ushort)0x054C, (ushort)0x09CC, (ushort)0x0221, false)]
    [InlineData((ushort)0x045E, (ushort)0x05C4, (ushort)0x0221, false)]
    public void MatchesDs4Identity_RequiresDefenderBtVidPidAndBcdDevice(
        ushort vendorId, ushort productId, ushort versionNumber, bool expected)
    {
        Assert.Equal(expected, DefenderBtModeSwitcher.MatchesDs4Identity(vendorId, productId, versionNumber));
    }

    [Fact]
    public void DualShock3ProductId_MatchesSonyDs3()
    {
        Assert.Equal(0x054C, DefenderBtModeSwitcher.SonyVendorId);
        Assert.Equal(0x0268, DefenderBtModeSwitcher.DualShock3ProductId);
        Assert.Equal(0x05C4, DefenderBtModeSwitcher.DualShock4ProductId);
        Assert.Equal(0x0221, DefenderBtModeSwitcher.DefenderBtVersionNumber);
    }

    [Theory]
    [InlineData(@"USB\VID_054C&PID_0268\6&4B29C3C&0&2", true)]
    [InlineData(@"USB\VID_054C&PID_05C4\6&4B29C3C&0&2", false)]
    [InlineData(@"USB\VID_054C&PID_042F\6&4B29C3C&0&2", false)]
    [InlineData(null, false)]
    public void IsDualShock3UsbInstanceId_RequiresSonyDs3VidPid(string? instanceId, bool expected)
    {
        Assert.Equal(expected, DefenderBtModeSwitcher.IsDualShock3UsbInstanceId(instanceId));
    }

    [Fact]
    public void HasNewlyAppearedDualShock3Usb_IgnoresPreExistingIdentities()
    {
        string existing = @"USB\VID_054C&PID_0268\EXISTING";
        string[] before = [existing];

        Assert.False(DefenderBtModeSwitcher.HasNewlyAppearedDualShock3Usb([existing], before));
        Assert.True(DefenderBtModeSwitcher.HasNewlyAppearedDualShock3Usb(
            [existing, @"USB\VID_054C&PID_0268\NEW"], before));
        Assert.False(DefenderBtModeSwitcher.HasNewlyAppearedDualShock3Usb(
            [@"USB\VID_054C&PID_05C4\NEW"], before));
    }
}
