using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;

namespace Nefarius.DsHidMini.ControlApp.Models.Util.Web;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class OUIEntry : IEquatable<OUIEntry>
{
    public OUIEntry(string manufacturer)
    {
        string hex = manufacturer.Replace(":", string.Empty);

        Bytes = Enumerable.Range(0, hex.Length / 2).Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
            .ToArray();
    }

    public OUIEntry(PhysicalAddress address)
    {
        Bytes = address.GetAddressBytes().Take(3).ToArray();
    }

    public byte[] Bytes { get; }

    public bool Equals(OUIEntry other)
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

    public override bool Equals(object obj)
    {
        if (obj is OUIEntry entry)
        {
            return Bytes.SequenceEqual(entry.Bytes);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Bytes.GetHashCode();
    }
}

/// <summary>
///     Genuine controller MAC address validator.
/// </summary>
/// <remarks>https://github.com/nefarius/DsHidMini/discussions/166</remarks>
internal sealed class AddressValidator(IHttpClientFactory clientFactory)
{
    [Obsolete("Redesign to use modern HttpClient instead.")]
    public async Task<bool> IsGenuineAddress(PhysicalAddress address)
    {
        using HttpClient client = clientFactory.CreateClient("Docs");

        IEnumerable<OUIEntry>? ouiList =
            (await client.GetFromJsonAsync<IList<string>>("/projects/DsHidMini/genuine_oui_db.json"))
            ?.Select(e => new OUIEntry(e));

        if (ouiList is null)
        {
            return false;
        }

        OUIEntry device = new(address);

        return ouiList.Contains(device);
    }
}