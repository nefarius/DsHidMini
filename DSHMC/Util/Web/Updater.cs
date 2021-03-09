using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Nefarius.DsHidMini.Properties;
using Newtonsoft.Json;

namespace Nefarius.DsHidMini.Util.Web
{
    public class Updater
    {
        public static Version AssemblyVersion => Assembly.GetEntryAssembly().GetName().Version;

        public static Uri ReleasesUri => new Uri("https://api.github.com/repos/ViGEm/DsHidMini/releases");

        /// <summary>
        ///     True if tag on latest GitHub release is newer than own assembly version, false otherwise.
        /// </summary>
        public static bool IsUpdateAvailable
        {
            get
            {
                // Get cached value
                var lastChecked = (DateTime) Settings.Default["LastCheckedForUpdate"];

                // If we already checked today, return cached value to reduce HTTP calls
                if (lastChecked.AddDays(1) >= DateTime.UtcNow)
                    return (bool) Settings.Default["IsUpdateAvailable"];

                try
                {
                    // Query for releases/tags and store information
                    using (var client = new WebClient())
                    {
                        // Required or result is HTTP-403
                        client.Headers["User-Agent"] =
                            "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                            "(compatible; MSIE 6.0; Windows NT 5.1; " +
                            ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                        // Get body
                        var response = client.DownloadString(ReleasesUri);

                        // Get JSON objects
                        var result = JsonConvert.DeserializeObject<IList<Root>>(response);

                        // Top release is latest of interest
                        var latest = result.FirstOrDefault();

                        // No release found to compare to, bail out
                        if (latest == null)
                            return false;

                        // Expected format e.g. "v1.2.3" so strip first character
                        var version = Version.Parse(new string(latest.TagName.Skip(1).ToArray()));

                        // Store values in user settings
                        Settings.Default["LastCheckedForUpdate"] = DateTime.UtcNow;
                        Settings.Default["IsUpdateAvailable"] = version > AssemblyVersion;
                        Settings.Default.Save();

                        return (bool) Settings.Default["IsUpdateAvailable"];
                    }
                }
                catch
                {
                    // May happen on network issues, ignore
                    return false;
                }
            }
        }
    }
}