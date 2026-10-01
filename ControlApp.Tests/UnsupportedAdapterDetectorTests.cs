using Nefarius.DsHidMini.ControlApp.Models.Util;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class UnsupportedAdapterDetectorTests
{
    [Fact]
    public void TwinUsbIds_MatchGreenAsiaPantherLordAdapter()
    {
        Assert.Equal(0x0810, UnsupportedAdapterDetector.TwinUsbVendorId);
        Assert.Equal(0x0001, UnsupportedAdapterDetector.TwinUsbProductId);
        Assert.True(UnsupportedAdapterDetector.IsKnownUnsupported(0x0810, 0x0001));
        Assert.False(UnsupportedAdapterDetector.IsKnownUnsupported(0x0810, 0x0003));
        Assert.False(UnsupportedAdapterDetector.IsKnownUnsupported(0x2563, 0x0575));
        Assert.False(UnsupportedAdapterDetector.IsKnownUnsupported(0x054C, 0x0268));
    }

    [Theory]
    [InlineData(@"USB\VID_0810&PID_0001\6&4B29C3C&0&1", true)]
    [InlineData(@"USB\VID_0810&PID_0001&REV_0106", true)]
    [InlineData(@"USB\VID_0810&PID_0003\6&4B29C3C&0&1", false)]
    [InlineData(@"USB\VID_2563&PID_0575\6&4B29C3C&0&2", false)]
    [InlineData(@"USB\VID_054C&PID_0268\6&4B29C3C&0&2", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsKnownUnsupportedInstanceId_RequiresTwinUsbVidPid(string? instanceId, bool expected)
    {
        Assert.Equal(expected, UnsupportedAdapterDetector.IsKnownUnsupportedInstanceId(instanceId));
    }

    [Fact]
    public void TryMatchInstanceId_ReturnsTwinUsbAdapterCopy()
    {
        Assert.True(UnsupportedAdapterDetector.TryMatchInstanceId(
            @"USB\VID_0810&PID_0001\6&4B29C3C&0&1", out UnsupportedAdapter adapter));
        Assert.Equal(UnsupportedAdapterDetector.TwinUsbVendorId, adapter.VendorId);
        Assert.Equal(UnsupportedAdapterDetector.TwinUsbProductId, adapter.ProductId);
        Assert.Equal("Twin USB Joystick", adapter.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(adapter.StatusTitle));
        Assert.False(string.IsNullOrWhiteSpace(adapter.StatusMessage));
    }
}
