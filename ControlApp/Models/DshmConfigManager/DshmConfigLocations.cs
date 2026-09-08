using System.IO;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

internal sealed class DshmConfigLocations
{
    public DshmConfigLocations(string userDataDirectory, string driverConfigDirectory)
    {
        UserDataDirectory = userDataDirectory;
        DriverConfigDirectory = driverConfigDirectory;
    }

    public string UserDataDirectory { get; }

    public string DriverConfigDirectory { get; }

    public string UserDataFilePath =>
        Path.Combine(UserDataDirectory, DshmConfigManagerUserData.GlobalUserDataFileName + ".json");

    public string DriverConfigFilePath =>
        DshmConfigSerialization.GetDriverConfigFilePath(DriverConfigDirectory);

    private static readonly Lazy<DshmConfigLocations> DefaultLazy = new(() => CreateDefault());

    public static DshmConfigLocations Default => DefaultLazy.Value;

    internal static DshmConfigLocations CreateDefault(
        string? programDataDirectory = null,
        string? driverConfigDirectory = null)
    {
        string programData = programDataDirectory
                             ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string driverDirectory = driverConfigDirectory
                                 ?? DshmConfigSerialization.GetDriverConfigDirectory();
        string preferredUserDataDirectory = Path.Combine(
            driverDirectory,
            DshmConfigManagerUserData.GlobalUserDataFolderName);
        string legacyUserDataDirectory = Path.Combine(
            programData,
            DshmConfigManagerUserData.GlobalUserDataFolderName);

        return new DshmConfigLocations(
            DshmUserDataLocationMigration.ResolveUserDataDirectory(
                preferredUserDataDirectory,
                legacyUserDataDirectory),
            driverDirectory);
    }
}
