using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class UsbPowerOffResultTests
{
    private const uint StatusSuccess = 0x00000000;
    private const uint StatusNotSupported = 0xC00000BB;
    private const uint StatusUnsuccessful = 0xC0000001;
    private const uint StatusInformational = 0x40000000;

    [Theory]
    [InlineData(StatusSuccess, true)]
    [InlineData(StatusInformational, true)]
    [InlineData(StatusNotSupported, false)]
    [InlineData(StatusUnsuccessful, false)]
    public void IsNtSuccess_MatchesDriverNtSuccess(uint status, bool expected)
    {
        Assert.Equal(expected, PowerOffUsbResult.IsNtSuccess(status));
    }

    [Fact]
    public void Succeeded_RequiresBothTransfersToSucceed()
    {
        Assert.True(new PowerOffUsbResult
        {
            IndicatorsOffStatus = StatusSuccess,
            ShutdownStatus = StatusSuccess
        }.Succeeded);

        Assert.False(new PowerOffUsbResult
        {
            IndicatorsOffStatus = StatusSuccess,
            ShutdownStatus = StatusNotSupported
        }.Succeeded);

        Assert.False(new PowerOffUsbResult
        {
            IndicatorsOffStatus = StatusUnsuccessful,
            ShutdownStatus = StatusSuccess
        }.Succeeded);
    }

    [Fact]
    public void ToString_IncludesBothStatuses()
    {
        string text = new PowerOffUsbResult
        {
            IndicatorsOffStatus = StatusSuccess,
            ShutdownStatus = StatusNotSupported
        }.ToString();

        Assert.Contains("0x0", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0xC00000BB", text, StringComparison.OrdinalIgnoreCase);
    }
}
