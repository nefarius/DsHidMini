using System.Globalization;

namespace Nefarius.DsHidMini.ControlApp.Models.Input;

internal static class InputReportMetricsFormatter
{
    public static string FormatRateHz(uint? hz)
    {
        if (hz is null)
        {
            return "Unknown";
        }

        if (hz == 0)
        {
            return "\u2014";
        }

        return string.Format(CultureInfo.InvariantCulture, "{0} Hz", hz.Value);
    }

    public static string FormatIntervalUs(uint? microseconds)
    {
        if (microseconds is null)
        {
            return "Unknown";
        }

        if (microseconds == 0)
        {
            return "\u2014";
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:N0} \u00B5s", microseconds.Value);
    }
}
