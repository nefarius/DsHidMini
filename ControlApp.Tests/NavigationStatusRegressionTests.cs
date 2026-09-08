using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.IPC.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class NavigationStatusRegressionTests
{
    [Theory]
    [InlineData(@"USB\VID_054C&PID_042F\6&4B29C3C&0&2")]
    [InlineData(@"BTHPS3BUS\{206F84FC-1615-4D9F-954D-21F5A5D388C5}&Dev&VID_054C&PID_042F")]
    public void FromInstanceId_NavigationIds_AreRecognized(string instanceId)
    {
        Assert.Equal(DsDeviceType.Navigation, DsDeviceCapabilities.FromInstanceId(instanceId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"USB\VID_054C&PID_ZZZZ")]
    [InlineData(@"USB\VID_1234&PID_042F")]
    public void FromInstanceId_InvalidOrUnsupportedIds_AreUnknown(string? instanceId)
    {
        Assert.Equal(DsDeviceType.Unknown, DsDeviceCapabilities.FromInstanceId(instanceId));
    }

    [Theory]
    [InlineData(0x00, DsBatteryStatus.Unknown)]
    [InlineData(0x05, DsBatteryStatus.Full)]
    [InlineData(0x06, DsBatteryStatus.Unknown)]
    [InlineData(0xEE, DsBatteryStatus.Charging)]
    [InlineData(0xEF, DsBatteryStatus.Charged)]
    [InlineData(0xF0, DsBatteryStatus.Charging)]
    [InlineData(0xF1, DsBatteryStatus.Charged)]
    public void NormalizeBatteryStatus_UsesSonyChargingBit(byte rawStatus, DsBatteryStatus expected)
    {
        Assert.Equal(expected, DsDeviceCapabilities.NormalizeBatteryStatus(rawStatus));
    }
}
