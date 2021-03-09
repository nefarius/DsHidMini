using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace Nefarius.DsHidMini.Util.Web
{
    public class Updater
    {
        public static Version AssemblyVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static Uri ReleasesUri => new Uri("https://api.github.com/repos/ViGEm/DsHidMini/releases");

        public static bool IsUpdateAvailable
        {
            get
            {
                using (var client = new WebClient())
                {
                    client.Headers["User-Agent"] =
                        "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                        "(compatible; MSIE 6.0; Windows NT 5.1; " +
                        ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                    var response = client.DownloadString(ReleasesUri);

                    var result = JsonConvert.DeserializeObject<IList<Root>>(response);

                    var latest = result.First();

                    var version = Version.Parse(new string(latest.TagName.Skip(1).ToArray()));

                    return version > AssemblyVersion;
                }
            }
        }
    }
}