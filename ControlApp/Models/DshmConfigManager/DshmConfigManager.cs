using System.IO;
using System.Threading;

using Newtonsoft.Json;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Class for managing user's dshidmini settings and applying them to the DsHidMini Configuration File
/// </summary>
public class DshmConfigManager : IDisposable
{
    private static readonly TimeSpan DriverConfigRefreshDebounce = TimeSpan.FromMilliseconds(250);

    private readonly DshmConfigLocations _locations;
    private readonly DshmConfigManagerUserData _userData;
    private readonly object _driverConfigWatchLock = new();
    private string? _lastGeneratedDriverJson;
    private FileSystemWatcher? _driverConfigWatcher;
    private CancellationTokenSource? _driverConfigRefreshCts;
    private EventHandler? _effectiveDriverConfigurationChanged;
    private int _disposed;

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
        _lastGeneratedDriverJson = DshmConfigSerialization.Serialize(
            DshmDriverConfigMigration.BuildDriverConfiguration(_userData));
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

    public bool IPCEnabled
    {
        get => _userData.IPCEnabled;
        set => _userData.IPCEnabled = value;
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

    /// <summary>
    ///     Bluetooth HID channel the driver is using for this device: on-disk
    ///     <c>DsHidMini.json</c> (device overlay, then Global), or ControlApp-managed
    ///     settings when that file cannot be read.
    /// </summary>
    public BluetoothOutputReportTransport ResolveEffectiveBluetoothOutputReportTransport(DeviceData device)
    {
        if (DshmConfigSerialization.TryReadDriverConfigFile(
                out DshmConfiguration? configuration,
                _locations.DriverConfigDirectory)
            && configuration is not null)
        {
            string mac = MacAddressFormatter.Normalize(device.DeviceMac);
            DshmDeviceData? overlay = configuration.Devices.FirstOrDefault(candidate =>
                string.Equals(
                    MacAddressFormatter.Normalize(candidate.DeviceAddress),
                    mac,
                    StringComparison.OrdinalIgnoreCase));

            return overlay?.DeviceSettings.BluetoothOutputReportTransport
                   ?? configuration.Global.BluetoothOutputReportTransport
                   ?? BluetoothOutputReportTransport.Control;
        }

        return ResolveEffectiveSettings(device).OutputReport.BluetoothOutputReportTransport;
    }

    /// <summary>
    ///     HID mode the driver will apply for this device: on-disk <c>DsHidMini.json</c>
    ///     (device overlay, then Global), or ControlApp-managed settings when that
    ///     file cannot be read. Does not import the file into profiles or user data.
    /// </summary>
    public SettingsContext ResolveEffectiveHidMode(DeviceData device)
    {
        if (DshmConfigSerialization.TryReadDriverConfigFile(
                out DshmConfiguration? configuration,
                _locations.DriverConfigDirectory)
            && configuration is not null)
        {
            string mac = MacAddressFormatter.Normalize(device.DeviceMac);
            DshmDeviceData? overlay = configuration.Devices.FirstOrDefault(candidate =>
                string.Equals(
                    MacAddressFormatter.Normalize(candidate.DeviceAddress),
                    mac,
                    StringComparison.OrdinalIgnoreCase));

            HidDeviceMode? mode = overlay?.DeviceSettings.HidDeviceMode
                                  ?? configuration.Global.HidDeviceMode;
            if (mode is { } resolved &&
                DshmManagerToDriverConversion.HidDeviceModeDriverToManager.TryGetValue(
                    resolved,
                    out SettingsContext context))
            {
                return context;
            }
        }

        return ResolveEffectiveSettings(device).HidMode.SettingsContext;
    }

    /// <summary>
    ///     Raised after <c>DsHidMini.json</c> changes on disk, including an external edit
    ///     or an atomic replace. Subscribing starts a debounced watcher. The handler must
    ///     not treat this as a request to overwrite ControlApp user data.
    /// </summary>
    public event EventHandler? EffectiveDriverConfigurationChanged
    {
        add
        {
            EnsureDriverConfigWatcher();
            _effectiveDriverConfigurationChanged += value;
        }
        remove => _effectiveDriverConfigurationChanged -= value;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_driverConfigWatchLock)
        {
            _driverConfigRefreshCts?.Cancel();
            _driverConfigRefreshCts?.Dispose();
            _driverConfigRefreshCts = null;
            DisposeDriverConfigWatcher();
        }
    }

    public bool SaveChangesAndUpdateDsHidMiniConfigFile()
    {
        string userDataPath = _locations.UserDataFilePath;
        string? previousUserJson = null;
        try
        {
            previousUserJson = File.Exists(userDataPath) ? File.ReadAllText(userDataPath) : null;
            _userData.Save(_locations);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Error(ex,
                "Failed to persist ControlApp user data to {UserDataPath}.",
                userDataPath);
            RestoreUserDataMemory(previousUserJson);
            return false;
        }

        bool updated = ApplySettings();
        if (!updated)
        {
            RestoreUserDataFile(userDataPath, previousUserJson);
            RestoreUserDataMemory(previousUserJson);
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

    private void RestoreUserDataMemory(string? previousUserJson)
    {
        try
        {
            _userData.RestoreFromSnapshot(previousUserJson);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            Log.Logger.Error(ex,
                "Failed to restore in-memory ControlApp user data after a driver config write failure.");
        }
    }

    public bool ApplySettings()
    {
        Log.Information("Updating DsHidMini configuration based on DsHidMini User Data");
        Log.Debug("Building DsHidMini configuration object based on DsHidMini User Data");
        DshmConfiguration dshmConfiguration = DshmDriverConfigMigration.BuildDriverConfiguration(_userData);
        string desiredJson = DshmConfigSerialization.Serialize(dshmConfiguration);

        Log.Logger.Debug("Configuration object built. Applying configuration.");
        bool updateStatus = DshmConfigSerialization.UpdateDsHidMiniConfigFilePreservingUnrelated(
            desiredJson,
            _lastGeneratedDriverJson,
            _locations.DriverConfigDirectory);
        if (updateStatus)
        {
            _lastGeneratedDriverJson = desiredJson;
        }

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

    /// <summary>
    ///     Moves a user profile to <paramref name="targetIndex" /> in the user-profile list (Default excluded).
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if the list changed; otherwise <see langword="false" />.
    /// </returns>
    public bool MoveUserProfile(ProfileData profile, int targetIndex)
    {
        if (profile == ProfileData.DefaultProfile || profile.ProfileGuid == ProfileData.DefaultGuid)
        {
            Log.Logger.Debug("Default profile cannot be reordered.");
            return false;
        }

        int currentIndex = _userData.Profiles.FindIndex(existing => existing.ProfileGuid == profile.ProfileGuid);
        if (currentIndex < 0)
        {
            Log.Logger.Debug("Profile '{ProfileGuid}' is not in the user profile list.", profile.ProfileGuid);
            return false;
        }

        if (targetIndex < 0 || targetIndex >= _userData.Profiles.Count || targetIndex == currentIndex)
        {
            return false;
        }

        ProfileData item = _userData.Profiles[currentIndex];
        _userData.Profiles.RemoveAt(currentIndex);
        _userData.Profiles.Insert(targetIndex, item);
        Log.Logger.Information(
            "Moved profile '{ProfileName}' ({ProfileGuid}) from {FromIndex} to {ToIndex}.",
            item.ProfileName, item.ProfileGuid, currentIndex, targetIndex);
        return true;
    }

    public SettingsContext GetDeviceExpectedHidMode(DeviceData dev) =>
        ResolveEffectiveHidMode(dev);

    private void EnsureDriverConfigWatcher()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        lock (_driverConfigWatchLock)
        {
            if (_driverConfigWatcher is not null || Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(_locations.DriverConfigDirectory);
                FileSystemWatcher watcher = new(_locations.DriverConfigDirectory)
                {
                    Filter = DshmConfigSerialization.DriverFileName,
                    NotifyFilter = NotifyFilters.FileName
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Size
                                   | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false
                };
                watcher.Changed += OnDriverConfigFileChanged;
                watcher.Created += OnDriverConfigFileChanged;
                watcher.Deleted += OnDriverConfigFileChanged;
                watcher.Renamed += OnDriverConfigFileRenamed;
                watcher.Error += OnDriverConfigWatcherError;
                watcher.EnableRaisingEvents = true;
                _driverConfigWatcher = watcher;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log.Logger.Warning(ex,
                    "Failed to watch DsHidMini configuration in {Directory}.",
                    _locations.DriverConfigDirectory);
            }
        }
    }

    private void DisposeDriverConfigWatcher()
    {
        if (_driverConfigWatcher is null)
        {
            return;
        }

        _driverConfigWatcher.EnableRaisingEvents = false;
        _driverConfigWatcher.Changed -= OnDriverConfigFileChanged;
        _driverConfigWatcher.Created -= OnDriverConfigFileChanged;
        _driverConfigWatcher.Deleted -= OnDriverConfigFileChanged;
        _driverConfigWatcher.Renamed -= OnDriverConfigFileRenamed;
        _driverConfigWatcher.Error -= OnDriverConfigWatcherError;
        _driverConfigWatcher.Dispose();
        _driverConfigWatcher = null;
    }

    private void OnDriverConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        if (IsDriverConfigFile(e.FullPath))
        {
            QueueEffectiveDriverConfigurationRefresh();
        }
    }

    private void OnDriverConfigFileRenamed(object sender, RenamedEventArgs e)
    {
        if (IsDriverConfigFile(e.FullPath) || IsDriverConfigFile(e.OldFullPath))
        {
            QueueEffectiveDriverConfigurationRefresh();
        }
    }

    private void OnDriverConfigWatcherError(object sender, ErrorEventArgs e)
    {
        Log.Logger.Warning(e.GetException(), "DsHidMini configuration watcher failed.");
    }

    private static bool IsDriverConfigFile(string? path) =>
        path is not null &&
        string.Equals(
            Path.GetFileName(path),
            DshmConfigSerialization.DriverFileName,
            StringComparison.OrdinalIgnoreCase);

    private void QueueEffectiveDriverConfigurationRefresh()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        CancellationToken token;
        lock (_driverConfigWatchLock)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            CancellationTokenSource cts = new();
            CancellationTokenSource? previous = Interlocked.Exchange(ref _driverConfigRefreshCts, cts);
            previous?.Cancel();
            previous?.Dispose();
            token = cts.Token;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DriverConfigRefreshDebounce, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested || Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            Log.Logger.Debug("DsHidMini.json changed. Refreshing the effective driver configuration.");
            _effectiveDriverConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }, token);
    }

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
