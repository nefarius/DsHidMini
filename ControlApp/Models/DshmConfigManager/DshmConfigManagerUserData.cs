using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Dshm Config Manager user data: profiles, known devices, and schema version.
/// </summary>
internal class DshmConfigManagerUserData
{
    public const int CurrentSchemaVersion = 1;

    [System.Text.Json.Serialization.JsonIgnore]
    public static string GlobalUserDataFileName => "DshmUserData";

    public static string GlobalUserDataFolderName => "ControlApp";

    /// <summary>
    ///     Incremented when the ControlApp store format changes. 0 / missing means the store has not been migrated.
    /// </summary>
    public int SchemaVersion { get; set; }

    public Guid GlobalProfileGuid { get; set; } = ProfileData.DefaultGuid;

    public bool AutoRestartOnHidModeMismatch { get; set; } = true;

    public bool IPCEnabled { get; set; } = true;

    public List<ProfileData> Profiles { get; } = new();

    public List<DeviceData> Devices { get; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public bool FileExistedOnLoad { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
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

    /// <summary>
    ///     Replays a previous on-disk snapshot onto this instance so view-model references stay valid.
    /// </summary>
    internal void RestoreFromSnapshot(string? previousUserJson)
    {
        DshmConfigManagerUserData source = DeserializeSnapshot(previousUserJson);
        SchemaVersion = source.SchemaVersion;
        GlobalProfileGuid = source.GlobalProfileGuid;
        AutoRestartOnHidModeMismatch = source.AutoRestartOnHidModeMismatch;
        IPCEnabled = source.IPCEnabled;
        ReplaceProfiles(source.Profiles);
        ReplaceDevices(source.Devices);
    }

    private static DshmConfigManagerUserData DeserializeSnapshot(string? previousUserJson)
    {
        if (string.IsNullOrWhiteSpace(previousUserJson))
        {
            return new DshmConfigManagerUserData();
        }

        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());
        return JsonConvert.DeserializeObject<DshmConfigManagerUserData>(previousUserJson, settings)
               ?? new DshmConfigManagerUserData();
    }

    private void ReplaceProfiles(List<ProfileData> sourceProfiles)
    {
        Dictionary<Guid, ProfileData> incoming = sourceProfiles.ToDictionary(profile => profile.ProfileGuid);
        for (int i = Profiles.Count - 1; i >= 0; i--)
        {
            if (!incoming.Remove(Profiles[i].ProfileGuid, out ProfileData? restored))
            {
                Profiles.RemoveAt(i);
                continue;
            }

            Profiles[i].ProfileName = restored.ProfileName;
            DeviceSettings.CopySettings(Profiles[i].Settings, restored.Settings);
        }

        foreach (ProfileData added in incoming.Values)
        {
            Profiles.Add(added);
        }
    }

    private void ReplaceDevices(List<DeviceData> sourceDevices)
    {
        Dictionary<string, DeviceData> incoming = new(StringComparer.OrdinalIgnoreCase);
        foreach (DeviceData device in sourceDevices)
        {
            incoming[device.DeviceMac] = device;
        }

        for (int i = Devices.Count - 1; i >= 0; i--)
        {
            if (!incoming.Remove(Devices[i].DeviceMac, out DeviceData? restored))
            {
                Devices.RemoveAt(i);
                continue;
            }

            Devices[i].CustomName = restored.CustomName;
            Devices[i].GuidOfProfileToUse = restored.GuidOfProfileToUse;
            Devices[i].BluetoothPairingMode = restored.BluetoothPairingMode;
            Devices[i].PairingAddress = restored.PairingAddress;
            Devices[i].SettingsMode = restored.SettingsMode;
            DeviceSettings.CopySettings(Devices[i].Settings, restored.Settings);
        }

        foreach (DeviceData added in incoming.Values)
        {
            Devices.Add(added);
        }
    }
}
