namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Global settings of the UI tool (stored in %AppData%).
/// </summary>
public class ApplicationConfiguration
{
    /// <summary>
    ///     Implicitly loads configuration from file.
    /// </summary>
    private static readonly Lazy<ApplicationConfiguration> AppConfigLazy =
        new(() => JsonApplicationConfiguration
            .Load<ApplicationConfiguration>(
                GlobalConfigFileName,
                true,
                false));

    /// <summary>
    ///     JSON (and schema) file name holding global configuration values.
    /// </summary>
    public static string GlobalConfigFileName => "ControlApp";

    /// <summary>
    ///     True if a log file should be generated, false otherwise.
    /// </summary>
    public bool IsLoggingEnabled { get; set; } = false;

    /// <summary>
    ///     True if check for new version happens on startup, false otherwise.
    /// </summary>
    public bool IsUpdateCheckEnabled { get; set; } = true;

    /// <summary>
    ///     If true, downloads genuine OUI list and compares controller MAC against.
    /// </summary>
    public bool IsGenuineCheckEnabled { get; set; } = true;

    /// <summary>
    ///     Whether user has acknowledged the donation dialog.
    /// </summary>
    public bool HasAcknowledgedDonationDialog { get; set; } = false;

    /// <summary>
    ///     Singleton instance of app configuration.
    /// </summary>
    public static ApplicationConfiguration Instance => AppConfigLazy.Value;

    /// <summary>
    ///     Write changes to file.
    /// </summary>
    public void Save()
    {
        //
        // Store (modified) configuration to disk
        // 
        JsonApplicationConfiguration.Save(
            GlobalConfigFileName,
            this,
            false);
    }
}