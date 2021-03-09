using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Nefarius.DsHidMini.Util.Web
{
    public class OUIEntry : IEquatable<OUIEntry>
    {
        public OUIEntry(string manufacturer)
        {
            var hex = manufacturer.Replace(":", string.Empty);

            Bytes = Enumerable.Range(0, hex.Length / 2).Select(x => Convert.ToByte(hex.Substring(x * 2, 2), 16))
                .ToArray();
        }

        public OUIEntry(PhysicalAddress address)
        {
            Bytes = address.GetAddressBytes().Take(3).ToArray();
        }

        public byte[] Bytes { get; }

        public override bool Equals(object obj)
        {
            if (obj is OUIEntry entry)
                return Bytes.SequenceEqual(entry.Bytes);
            return false;
        }

        public bool Equals(OUIEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Bytes.SequenceEqual(other.Bytes);
        }

        public override int GetHashCode()
        {
            return Bytes.GetHashCode();
        }
    }

    public class Validator
    {
        public static Uri ApiUrl => new Uri("https://vigem.org/projects/DsHidMini/genuine_oui_db.json");

        public static bool IsGenuineAddress(PhysicalAddress address)
        {
            using (var client = new WebClient())
            {
                var ouiList = JsonConvert.DeserializeObject<IList<string>>(client.DownloadString(ApiUrl)).Select(e => new OUIEntry(e));

                var device = new OUIEntry(address);

                return ouiList.Contains(device);
            }
        }
    }
}