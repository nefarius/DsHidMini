using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Serilog;

namespace Nefarius.DsHidMini.ControlApp.Models.Util.Web
{
    /// <summary>
    ///     Checks for updates via GitHub API.
    /// </summary>
    public static class Updater
    {
        /// <summary>
        ///     Gets the control application version.
        /// </summary>
        public static Version AssemblyVersion => Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        ///     Gets the releases API URI.
        /// </summary>
        public static Uri ReleasesUri => new Uri("https://api.github.com/repos/ViGEm/DsHidMini/releases");

        /// <summary>
        ///     True if tag on latest GitHub release is newer than own assembly version, false otherwise.
        /// </summary>
        public static bool IsUpdateAvailable
        {
            get
            {
                Log.Information("Checking for new version, current version is {Version}",
                    AssemblyVersion);

                if (!ApplicationConfiguration.Instance.IsUpdateCheckEnabled)
                {
                    Log.Information("Update check is disabled in configuration");
                    return false;
                }

                try
                {
                    // Query for releases/tags and store information
                    using (var client = new WebClient())
                    {
                        Log.Information("Checking for updates, preparing web request");

                        // Required or result is HTTP-403
                        client.Headers["User-Agent"] =
                            "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                            "(compatible; MSIE 6.0; Windows NT 5.1; " +
                            ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                        // Get body
                        var response = client.DownloadString(ReleasesUri);

                        // Get JSON objects
                        var result = JsonConvert.DeserializeObject<IList<Root>>(response);
                        Log.Debug("Found {ReleaseCount} release(s)", result.Count);

                        // Top release is latest of interest
                        var latest = result.FirstOrDefault();

                        // No release found to compare to, bail out
                        if (latest == null)
                            return false;

                        Log.Debug("Latest tag name: {Tag}", latest.TagName);

                        var tag = new string(latest.TagName.Skip(1).ToArray());
                        Log.Debug("Stripped tag name: {Tag}", tag);

                        // Expected format e.g. "v1.2.3" so strip first character
                        var version = Version.Parse(tag);
                        Log.Debug("Tag to version conversion: {Version}", version);


                        var isOutdated = version.CompareTo(AssemblyVersion) > 0;
                        if (isOutdated)
                            Log.Information("Update available");

                        return isOutdated;
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("Updated check failed: {Exception}", ex);

                    // May happen on network issues, ignore
                    return false;
                }
            }
        }
    }
}