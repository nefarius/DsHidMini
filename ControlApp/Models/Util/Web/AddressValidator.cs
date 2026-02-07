using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Nefarius.DsHidMini.ControlApp.Models.Util.Web;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class OUIEntry : IEquatable<OUIEntry>
{
    public OUIEntry(string manufacturer)
    {
        if (manufacturer is null)
        {
            throw new ArgumentNullException(nameof(manufacturer));
        }

        // Strip common separators and all whitespace (e.g., "AA:BB:CC", "AA-BB-CC", "AABBCC", " AA BB CC ").
        string hex = new string(manufacturer
            .Where(c => !char.IsWhiteSpace(c) && c is not ':' and not '-' and not '.')
            .ToArray());

        // Fail fast on malformed lengths (prevents truncation of a trailing nibble).
        if ((hex.Length % 2) != 0)
        {
            throw new ArgumentException(
                $@"OUI hex string must contain an even number of hex digits after removing separators/whitespace, but was {hex.Length}.",
                nameof(manufacturer));
        }

        // OUIs are exactly 3 bytes = 6 hex digits.
        if (hex.Length != 6)
        {
            throw new ArgumentException(
                $@"OUI hex string must contain exactly 6 hex digits (3 bytes) after removing separators/whitespace, but was {hex.Length}.",
                nameof(manufacturer));
        }

        // Optional: validate characters before parsing to give a clearer error than Convert.ToByte might.
        if (hex.Any(c => !Uri.IsHexDigit(c)))
        {
            throw new ArgumentException(
                @"OUI hex string contains non-hex characters after removing separators/whitespace.",
                nameof(manufacturer));
        }

        Bytes = Enumerable.Range(0, hex.Length / 2)
            .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
            .ToArray();
    }

    public OUIEntry(PhysicalAddress address)
    {
        Bytes = address.GetAddressBytes().Take(3).ToArray();
    }

    public byte[] Bytes { get; }

    public bool Equals(OUIEntry? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Bytes.SequenceEqual(other.Bytes);
    }

    public override bool Equals(object? obj)
    {
        if (obj is OUIEntry entry)
        {
            return Bytes.SequenceEqual(entry.Bytes);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(Bytes);
        return hash.ToHashCode();
    }
}

/// <summary>
///     Genuine controller MAC address validator.
/// </summary>
/// <remarks>https://github.com/nefarius/DsHidMini/discussions/166</remarks>
public sealed class AddressValidator(IHttpClientFactory clientFactory, ILogger<AddressValidator>? logger = null)
{
    public async Task<bool> IsGenuineAddress(PhysicalAddress address)
    {
        using HttpClient client = clientFactory.CreateClient("Docs");

        try
        {
            IList<string>? rawOuiList =
                await client.GetFromJsonAsync<IList<string>>("/projects/DsHidMini/genuine_oui_db.json");

            if (rawOuiList is null)
            {
                return false;
            }

            // Protect Select + OUIEntry construction too (constructor can throw on malformed strings)
            IEnumerable<OUIEntry> ouiList = rawOuiList.Select(e => new OUIEntry(e));

            OUIEntry device = new(address);

            return ouiList.Contains(device);
        }
        catch (HttpRequestException ex)
        {
            logger?.LogWarning(ex, "Failed to download genuine OUI database; treating address as not genuine.");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger?.LogWarning(ex, "Downloading genuine OUI database timed out/canceled; treating address as not genuine.");
            return false;
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Failed to deserialize genuine OUI database JSON; treating address as not genuine.");
            return false;
        }
        catch (Exception ex)
        {
            // Covers LINQ Select enumeration issues + OUIEntry ctor failures + any unexpected runtime errors.
            logger?.LogWarning(ex, "Error while evaluating genuine OUI database; treating address as not genuine.");
            return false;
        }
    }
}