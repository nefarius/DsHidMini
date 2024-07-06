using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;

using Newtonsoft.Json;

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

public class Validator
{
    public static Uri ApiUrl => new("https://docs.nefarius.at/projects/DsHidMini/genuine_oui_db.json");

    [Obsolete("Redesign to use modern HttpClient instead.")]
    public static bool IsGenuineAddress(PhysicalAddress address)
    {
        using (WebClient client = new WebClient())
        {
            IEnumerable<OUIEntry> ouiList = JsonConvert.DeserializeObject<IList<string>>(client.DownloadString(ApiUrl))
                .Select(e => new OUIEntry(e));

            OUIEntry device = new OUIEntry(address);

            return ouiList.Contains(device);
        }
    }
}