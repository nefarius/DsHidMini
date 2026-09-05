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

    public static DshmConfigLocations Default { get; } = new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            DshmConfigManagerUserData.GlobalUserDataFolderName),
        DshmConfigSerialization.GetDriverConfigDirectory());
}
