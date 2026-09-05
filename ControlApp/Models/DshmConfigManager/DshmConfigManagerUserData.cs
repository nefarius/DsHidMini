using System.IO;
using System.Text.Json.Serialization;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Dshm Config Manager user data: profiles, known devices, and schema version.
/// </summary>
internal class DshmConfigManagerUserData
{
    public const int CurrentSchemaVersion = 1;

    [JsonIgnore]
    public static string GlobalUserDataFileName => "DshmUserData";

    public static string GlobalUserDataFolderName => "ControlApp";

    /// <summary>
    ///     Incremented when the ControlApp store format changes. 0 / missing means the store has not been migrated.
    /// </summary>
    public int SchemaVersion { get; set; }

    public Guid GlobalProfileGuid { get; set; } = ProfileData.DefaultGuid;

    public bool AutoRestartOnHidModeMismatch { get; set; } = true;

    public List<ProfileData> Profiles { get; } = new();

    public List<DeviceData> Devices { get; } = new();

    [JsonIgnore]
    public bool FileExistedOnLoad { get; set; }

    [JsonIgnore]
    public string? LoadedFromDirectory { get; set; }

    public bool IsUnmigrated =>
        SchemaVersion < CurrentSchemaVersion && Profiles.Count == 0 && Devices.Count == 0;

    public bool HasUserContent => Profiles.Count > 0 || Devices.Count > 0;

    public static DshmConfigManagerUserData Load(DshmConfigLocations locations)
    {
        DshmConfigManagerUserData data = JsonDshmUserData.Load<DshmConfigManagerUserData>(
            GlobalUserDataFileName,
            locations.UserDataDirectory,
            createIfMissing: false);

        data.LoadedFromDirectory = locations.UserDataDirectory;
        data.FileExistedOnLoad = File.Exists(locations.UserDataFilePath);
        return data;
    }

    public void Save(DshmConfigLocations locations)
    {
        JsonDshmUserData.Save(GlobalUserDataFileName, this, locations.UserDataDirectory);
    }
}
