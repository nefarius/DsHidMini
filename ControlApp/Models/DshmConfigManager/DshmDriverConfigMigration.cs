using System.IO;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

internal sealed class DshmDriverConfigMigrationResult
{
    public bool Attempted { get; set; }
    public bool Succeeded { get; set; }
    public string? BackupPath { get; set; }
    public List<string> Warnings { get; } = new();
    public string? Error { get; set; }
}

internal static class DshmDriverConfigMigration
{
    public static DshmDriverConfigMigrationResult TryImportIfNeeded(
        DshmConfigManagerUserData userData,
        DshmConfigLocations locations)
    {
        if (!userData.IsUnmigrated)
        {
            if (userData.SchemaVersion < DshmConfigManagerUserData.CurrentSchemaVersion)
            {
                userData.SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion;
                userData.Save(locations);
            }

            return new DshmDriverConfigMigrationResult { Attempted = false, Succeeded = true };
        }

        if (!File.Exists(locations.DriverConfigFilePath))
        {
            userData.SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion;
            userData.Save(locations);
            return new DshmDriverConfigMigrationResult { Attempted = false, Succeeded = true };
        }

        return Import(userData, locations);
    }

    public static DshmDriverConfigMigrationResult Import(
        DshmConfigManagerUserData userData,
        DshmConfigLocations locations)
    {
        DshmDriverConfigMigrationResult result = new() { Attempted = true };
        string sourcePath = locations.DriverConfigFilePath;

        try
        {
            DshmConfiguration driverConfig = DshmConfigSerialization.Deserialize(File.ReadAllText(sourcePath));
            string backupPath = $"{sourcePath}.pre-controlapp-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Copy(sourcePath, backupPath, overwrite: false);
            result.BackupPath = backupPath;

            CollectUnusedModeWarnings(driverConfig.Global, "Global", result.Warnings);
            DeviceSettings importedGlobal = new();
            DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(driverConfig.Global, importedGlobal);
            DeviceSettings.CopySettings(ProfileData.DefaultProfile.Settings, importedGlobal);

            if (driverConfig.Global.AutoRestartOnHidModeMismatch is { } autoRestart)
            {
                userData.AutoRestartOnHidModeMismatch = autoRestart;
            }

            foreach (DshmDeviceData device in driverConfig.Devices)
            {
                CollectUnusedModeWarnings(device.DeviceSettings, device.DeviceAddress, result.Warnings);
                string mac = MacAddressFormatter.Normalize(device.DeviceAddress);
                if (mac.Length != 12)
                {
                    result.Warnings.Add($"Skipped device entry with invalid address '{device.DeviceAddress}'.");
                    continue;
                }

                DeviceData deviceData = new(mac);
                if (device.DeviceSettings.DevicePairingMode is { } pairing &&
                    DshmManagerToDriverConversion.PairingModeDriverToManager.TryGetValue(pairing,
                        out BluetoothPairingMode appPairing))
                {
                    deviceData.BluetoothPairingMode = appPairing;
                }

                deviceData.PairingAddress = MacAddressFormatter.Normalize(device.DeviceSettings.CustomPairingAddress);

                if (DshmManagerToDriverConversion.HasDeviceSpecificSettings(device.DeviceSettings))
                {
                    DshmDeviceSettings merged =
                        DshmManagerToDriverConversion.OverlayDeviceSettings(driverConfig.Global, device.DeviceSettings);
                    DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(merged, deviceData.Settings);
                    deviceData.SettingsMode = SettingsModes.Custom;
                }
                else
                {
                    deviceData.SettingsMode = SettingsModes.Global;
                }

                userData.Devices.Add(deviceData);
            }

            userData.SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion;
            userData.Save(locations);

            DshmConfiguration rewritten = BuildDriverConfiguration(userData);
            if (!DshmConfigSerialization.UpdateDsHidMiniConfigFile(rewritten, locations.DriverConfigDirectory))
            {
                result.Error = "Imported user data was saved, but rewriting the driver configuration failed.";
                Log.Logger.Error(result.Error);
                return result;
            }

            result.Succeeded = true;
            Log.Logger.Information(
                "Imported existing DsHidMini configuration from {SourcePath}. Backup: {BackupPath}",
                sourcePath, backupPath);
            return result;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to import existing DsHidMini configuration from {SourcePath}", sourcePath);
            result.Error = ex.Message;
            return result;
        }
    }

    internal static DshmConfiguration BuildDriverConfiguration(DshmConfigManagerUserData userData)
    {
        ProfileData globalProfile = ResolveGlobalProfile(userData);

        DshmConfiguration configuration = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(globalProfile.Settings,
            configuration.Global);
        configuration.Global.AutoRestartOnHidModeMismatch = userData.AutoRestartOnHidModeMismatch;

        foreach (DeviceData device in userData.Devices)
        {
            configuration.Devices.Add(ToDriverDevice(device, userData, globalProfile));
        }

        return configuration;
    }

    internal static ProfileData ResolveGlobalProfile(DshmConfigManagerUserData userData)
    {
        if (userData.GlobalProfileGuid == ProfileData.DefaultGuid)
        {
            return ProfileData.DefaultProfile;
        }

        return userData.Profiles.FirstOrDefault(p => p.ProfileGuid == userData.GlobalProfileGuid)
               ?? ProfileData.DefaultProfile;
    }

    internal static DshmDeviceData ToDriverDevice(DeviceData device, DshmConfigManagerUserData userData,
        ProfileData globalProfile)
    {
        DshmDeviceData driverDevice = new()
        {
            DeviceAddress = MacAddressFormatter.Normalize(device.DeviceMac),
            DeviceSettings =
            {
                DevicePairingMode =
                    DshmManagerToDriverConversion.PairingModeManagerToDriver[device.BluetoothPairingMode],
                PairOnHotReload = device.PairOnHotReload,
                CustomPairingAddress = device.BluetoothPairingMode == BluetoothPairingMode.Custom
                    ? MacAddressFormatter.Normalize(device.PairingAddress)
                    : null
            }
        };

        DeviceSettings? effective = device.SettingsMode switch
        {
            SettingsModes.Custom => device.Settings,
            SettingsModes.Profile => ResolveProfile(userData, device.GuidOfProfileToUse)?.Settings,
            _ => null
        };

        if (effective is not null)
        {
            DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(effective, driverDevice.DeviceSettings);
        }

        return driverDevice;
    }

    internal static ProfileData? ResolveProfile(DshmConfigManagerUserData userData, Guid profileGuid)
    {
        if (profileGuid == ProfileData.DefaultGuid)
        {
            return ProfileData.DefaultProfile;
        }

        return userData.Profiles.FirstOrDefault(p => p.ProfileGuid == profileGuid);
    }

    private static void CollectUnusedModeWarnings(DshmDeviceSettings settings, string owner,
        List<string> warnings)
    {
        if (settings.UnusedModeBlocks.Count == 0)
        {
            return;
        }

        warnings.Add(
            $"{owner} contains inactive HID mode blocks ({string.Join(", ", settings.UnusedModeBlocks)}) that cannot be mapped losslessly. They remain in the backup.");
    }
}
