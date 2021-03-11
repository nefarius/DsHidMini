using System;

namespace Nefarius.DsHidMini.Util.App
{
    public class ApplicationConfiguration
    {
        private static readonly Lazy<ApplicationConfiguration> AppConfigLazy =
            new Lazy<ApplicationConfiguration>(() => JsonApplicationConfiguration
                .Load<ApplicationConfiguration>(
                    GlobalConfigFileName,
                    true,
                    true));

        /// <summary>
        ///     JSON (and schema) file name holding global configuration values.
        /// </summary>
        public static string GlobalConfigFileName => "DsHidMiniControlGlobalConfig";

        public DateTime LastCheckedForUpdate { get; set; }

        public bool IsUpdateAvailable { get; set; }

        public static ApplicationConfiguration Instance => AppConfigLazy.Value;

        public void Save()
        {
            //
            // Store (modified) configuration to disk
            // 
            JsonApplicationConfiguration.Save(
                GlobalConfigFileName,
                this,
                true);
        }
    }
}