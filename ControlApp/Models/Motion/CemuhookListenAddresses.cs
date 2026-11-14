using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

public sealed record CemuhookListenAddress(string Address, string DisplayName, bool IsAvailable = true);

internal static class CemuhookListenAddresses
{
    public static IReadOnlyList<CemuhookListenAddress> Discover()
    {
        var addresses = new List<(string Name, IPAddress Address)>();
        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (UnicastIPAddressInformation unicast in adapter.GetIPProperties().UnicastAddresses)
                addresses.Add((adapter.Name, unicast.Address));
        }

        return CreateOptions(addresses);
    }

    internal static IReadOnlyList<CemuhookListenAddress> CreateOptions(IEnumerable<(string Name, IPAddress Address)> addresses)
    {
        var options = new List<CemuhookListenAddress>
        {
            new("127.0.0.1", "This PC only — 127.0.0.1"),
            new("0.0.0.0", "All IPv4 interfaces — 0.0.0.0")
        };
        var seen = new HashSet<string>(options.Select(option => option.Address), StringComparer.Ordinal);
        foreach (var entry in addresses.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(entry => entry.Address.ToString(), StringComparer.Ordinal))
        {
            string address = entry.Address.ToString();
            if (entry.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(entry.Address) ||
                !seen.Add(address))
                continue;

            options.Add(new(address, $"{entry.Name} — {address}"));
        }

        return options;
    }

    public static string? ValidateEndpoint(string? address, int port)
    {
        if (!IPAddress.TryParse(address, out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return "Choose an available IPv4 listen address.";

        if (port is < 1 or > 65535)
            return "UDP port must be between 1 and 65535.";

        return null;
    }

    public static string AddressKey(string? address) => IPAddress.TryParse(address, out IPAddress? ip)
        ? ip.ToString()
        : address ?? string.Empty;
}
