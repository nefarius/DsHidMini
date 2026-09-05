using System.Text.RegularExpressions;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

public static class MacAddressFormatter
{
    private static readonly Regex NonHex = new(@"[^a-fA-F0-9]", RegexOptions.Compiled);

    public static string Normalize(string? mac)
    {
        if (string.IsNullOrEmpty(mac))
        {
            return string.Empty;
        }

        string hex = NonHex.Replace(mac, string.Empty).ToUpperInvariant();
        return hex.Length > 12 ? hex[..12] : hex;
    }

    public static string ToFriendly(string? mac)
    {
        string normalized = Normalize(mac);
        if (normalized.Length != 12)
        {
            return normalized;
        }

        return string.Join(":",
            normalized[..2],
            normalized.Substring(2, 2),
            normalized.Substring(4, 2),
            normalized.Substring(6, 2),
            normalized.Substring(8, 2),
            normalized.Substring(10, 2));
    }
}
