using System.IO;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Class for managing user's dshidmini settings and applying them to the DsHidMini Configuration File
/// </summary>
public class DshmConfigManager
{
    private readonly DshmConfigLocations _locations;
    private readonly DshmConfigManagerUserData _userData;

    public DshmConfigManager() : this(DshmConfigLocations.Default)
    {
    }

    internal DshmConfigManager(DshmConfigLocations locations)
        : this(DshmConfigManagerUserData.Load(locations), locations)
    {
    }

    internal DshmConfigManager(DshmConfigManagerUserData userData, DshmConfigLocations locations)
    {
        _userData = userData;
        _locations = locations;
        LastMigrationResult = DshmDriverConfigMigration.TryImportIfNeeded(_userData, _locations);
        FixDevicesWithBlankProfiles();
    }

    internal DshmDriverConfigMigrationResult LastMigrationResult { get; }

    public ProfileData GlobalProfile
    {
        get
        {
            ProfileData? gp = GetProfile(_userData.GlobalProfileGuid);
            if (gp != null)
            {
                return gp;
            }

            Log.Logger.Debug("Global profile set to non-existing profile");
            Log.Logger.Debug("Reverting Global profile to default profile.");
            _userData.GlobalProfileGuid = ProfileData.DefaultGuid;
            GlobalProfileUpdated?.Invoke(this, EventArgs.Empty);
            return ProfileData.DefaultProfile;
        }
        set
        {
            Log.Logger.Debug("Setting profile {ValueProfileName} as Global Profile", value.ProfileName);
            _userData.GlobalProfileGuid = value.ProfileGuid;
            GlobalProfileUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool AutoRestartOnHidModeMismatch
    {
        get => _userData.AutoRestartOnHidModeMismatch;
        set => _userData.AutoRestartOnHidModeMismatch = value;
    }

    public event EventHandler<DshmUpdatedEventArgs>? DshmConfigurationUpdated;

    public event EventHandler? GlobalProfileUpdated;

    public void SaveChanges()
    {
        Log.Logger.Information("Saving DsHidMini User Data to disk.");
        _userData.Save(_locations);
    }

    private void FixDevicesWithBlankProfiles()
    {
        foreach (DeviceData device in _userData.Devices.Where(device =>
                     GetProfile(device.GuidOfProfileToUse) == null))
        {
            Log.Logger.Information(
                "Device {DeviceDeviceMac} linked to non-existing profile. Reverting link to default profile.", device
                    .DeviceMac);
            device.GuidOfProfileToUse = ProfileData.DefaultGuid;
            if (device.SettingsMode != SettingsModes.Profile)
            {
                continue;
            }

            Log.Logger.Information(
                "Device {DeviceDeviceMac} was in Profile Settings Mode while using a non-existing profile. Setting device back to Global Settings. "
                , device.DeviceMac);
            device.SettingsMode = SettingsModes.Global;
        }
    }

    public ProfileData? GetProfile(Guid profileGuid)
    {
        ProfileData? profile = GetListOfProfilesWithDefault().FirstOrDefault(p => p.ProfileGuid == profileGuid);
        if (profile == null)
        {
            Log.Logger.Debug("No profile with GUID {ProfileGuid} found.", profileGuid);
        }

        return profile;
    }

    public ProfileData ResolveProfileOrDefault(Guid profileGuid) =>
        GetProfile(profileGuid) ?? GlobalProfile;

    public DeviceSettings ResolveEffectiveSettings(DeviceData device)
    {
        switch (device.SettingsMode)
        {
            case SettingsModes.Custom:
                return device.Settings;
            case SettingsModes.Profile:
                ProfileData? profile = GetProfile(device.GuidOfProfileToUse);
                if (profile is not null)
                {
                    return profile.Settings;
                }

                device.GuidOfProfileToUse = ProfileData.DefaultGuid;
                device.SettingsMode = SettingsModes.Global;
                Log.Logger.Warning(
                    "Device {DeviceMac} referenced a missing profile. Falling back to Global settings.",
                    device.DeviceMac);
                return GlobalProfile.Settings;
            case SettingsModes.Global:
            default:
                return GlobalProfile.Settings;
        }
    }

    public bool SaveChangesAndUpdateDsHidMiniConfigFile()
    {
        string userDataPath = _locations.UserDataFilePath;
        string? previousUserJson = File.Exists(userDataPath) ? File.ReadAllText(userDataPath) : null;

        _userData.Save(_locations);
        bool updated = ApplySettings();
        if (!updated)
        {
            RestoreUserDataFile(userDataPath, previousUserJson);
        }

        return updated;
    }

    private static void RestoreUserDataFile(string userDataPath, string? previousUserJson)
    {
        try
        {
            if (previousUserJson is not null)
            {
                File.WriteAllText(userDataPath, previousUserJson);
                return;
            }

            if (File.Exists(userDataPath))
            {
                File.Delete(userDataPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Error(ex,
                "Failed to restore previous ControlApp user data after a driver config write failure.");
        }
    }

    public bool ApplySettings()
    {
        Log.Information("Updating DsHidMini configuration based on DsHidMini User Data");
        Log.Debug("Building DsHidMini configuration object based on DsHidMini User Data");
        DshmConfiguration dshmConfiguration = DshmDriverConfigMigration.BuildDriverConfiguration(_userData);

        Log.Logger.Debug("Configuration object built. Applying configuration.");
        bool updateStatus = dshmConfiguration.ApplyConfiguration(_locations.DriverConfigDirectory);
        DshmConfigurationUpdated?.Invoke(this, new DshmUpdatedEventArgs { UpdatedSuccessfully = updateStatus });
        return updateStatus;
    }

    public List<ProfileData> GetListOfProfilesWithDefault()
    {
        List<ProfileData> userProfilesPlusDefault = new(_userData.Profiles);
        userProfilesPlusDefault.Insert(0, ProfileData.DefaultProfile);
        return userProfilesPlusDefault;
    }

    public ProfileData CreateProfile(string profileName)
    {
        ProfileData newProfile = new() { ProfileName = profileName };
        _userData.Profiles.Add(newProfile);
        Log.Logger.Information("Profile '{ProfileName}' created on DsHidMini User Data.", profileName);
        return newProfile;
    }

    public void DeleteProfile(ProfileData profile)
    {
        Log.Logger.Information("Deleting profile '{ProfileProfileName}'", profile.ProfileName);
        if (profile == ProfileData.DefaultProfile)
        {
            Log.Logger.Information("Default Profile can't be deleted.");
            return;
        }

        _userData.Profiles.Remove(profile);
        FixDevicesWithBlankProfiles();
    }

    public SettingsContext GetDeviceExpectedHidMode(DeviceData dev) =>
        ResolveEffectiveSettings(dev).HidMode.SettingsContext;

    public DeviceData GetDeviceData(string deviceMac)
    {
        Log.Logger.Information("Getting data for device {DeviceMac}.", deviceMac);
        foreach (DeviceData dev in _userData.Devices.Where(dev => dev.DeviceMac == deviceMac))
        {
            return dev;
        }

        Log.Logger.Information("Data for Device {DeviceMac} does not exist. Creating new.", deviceMac);
        DeviceData newDevice = new(deviceMac) { DeviceMac = deviceMac };
        _userData.Devices.Add(newDevice);
        return newDevice;
    }

    public class DshmUpdatedEventArgs : EventArgs
    {
        public bool UpdatedSuccessfully;
    }
}
