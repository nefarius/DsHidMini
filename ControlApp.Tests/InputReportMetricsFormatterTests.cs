using Nefarius.DsHidMini.ControlApp.Models.Input;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class InputReportMetricsFormatterTests
{
    [Fact]
    public void FormatRateHz_UsesUnknownAndDashAndInvariantSuffix()
    {
        Assert.Equal("Unknown", InputReportMetricsFormatter.FormatRateHz(null));
        Assert.Equal("\u2014", InputReportMetricsFormatter.FormatRateHz(0));
        Assert.Equal("101 Hz", InputReportMetricsFormatter.FormatRateHz(101));
    }

    [Fact]
    public void FormatIntervalUs_UsesUnknownAndDashAndGroupedMicroseconds()
    {
        Assert.Equal("Unknown", InputReportMetricsFormatter.FormatIntervalUs(null));
        Assert.Equal("\u2014", InputReportMetricsFormatter.FormatIntervalUs(0));
        Assert.Equal("9,901 \u00B5s", InputReportMetricsFormatter.FormatIntervalUs(9901));
    }
}
