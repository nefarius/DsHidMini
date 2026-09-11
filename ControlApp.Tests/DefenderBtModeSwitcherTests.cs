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
}
