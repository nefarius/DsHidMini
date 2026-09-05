using System.IO;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class ConfigMigrationAndLifecycleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dshm-tests-" + Guid.NewGuid().ToString("N"));

    public ConfigMigrationAndLifecycleTests()
    {
        Directory.CreateDirectory(UserDir);
        Directory.CreateDirectory(DriverDir);
        ProfileData.DefaultProfile.Settings.ResetToDefault();
    }

    private string UserDir => Path.Combine(_root, "ControlApp");
    private string DriverDir => Path.Combine(_root, "DsHidMini");

    public void Dispose()
    {
        ProfileData.DefaultProfile.Settings.ResetToDefault();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void MissingDriverConfig_CreatesFreshUserStore()
    {
        DshmConfigManager manager = CreateManager();
        Assert.False(manager.LastMigrationResult.Attempted);
        Assert.True(manager.LastMigrationResult.Succeeded);
        Assert.True(File.Exists(Path.Combine(UserDir, "DshmUserData.json")));
    }

    [Fact]
    public void NativeSample_IsImported_WithBackup()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DsHidMini.json"),
            Path.Combine(DriverDir, "DsHidMini.json"));

        DshmConfigManager manager = CreateManager();

        Assert.True(manager.LastMigrationResult.Attempted);
        Assert.True(manager.LastMigrationResult.Succeeded);
        Assert.NotNull(manager.LastMigrationResult.BackupPath);
        Assert.True(File.Exists(manager.LastMigrationResult.BackupPath));
        Assert.NotEmpty(manager.LastMigrationResult.Warnings);
        Assert.Equal(SettingsContext.DS4W, manager.GlobalProfile.Settings.HidMode.SettingsContext);
        Assert.NotEmpty(Directory.GetFiles(UserDir, "DshmUserData.json"));
    }

    [Fact]
    public void SparseDeviceOverlay_UsesGlobalMode()
    {
        File.WriteAllText(Path.Combine(DriverDir, "DsHidMini.json"), """
            {
              "Global": { "HidDeviceMode": "XInput", "XInput": { "DeadZoneLeft": { "Apply": true, "PolarValue": 3.0 } } },
              "Devices": {
                "AABBCCDDEEFF": { "DevicePairingMode": "Custom", "CustomPairingAddress": "112233445566" }
              }
            }
            """);

        DshmConfigManager manager = CreateManager();
        DeviceData device = manager.GetDeviceData("AABBCCDDEEFF");
        Assert.Equal(SettingsModes.Global, device.SettingsMode);
        Assert.Equal(BluetoothPairingMode.Custom, device.BluetoothPairingMode);
        Assert.Equal("112233445566", device.PairingAddress);
    }

    [Fact]
    public void MalformedDriverJson_LeavesOriginalUntouched()
    {
        string driverPath = Path.Combine(DriverDir, "DsHidMini.json");
        File.WriteAllText(driverPath, "{ not-json");
        string original = File.ReadAllText(driverPath);

        DshmConfigManager manager = CreateManager();

        Assert.True(manager.LastMigrationResult.Attempted);
        Assert.False(manager.LastMigrationResult.Succeeded);
        Assert.Equal(original, File.ReadAllText(driverPath));
        Assert.False(File.Exists(Path.Combine(UserDir, "DshmUserData.json")));
    }

    [Fact]
    public void RepeatStartup_IsIdempotent()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DsHidMini.json"),
            Path.Combine(DriverDir, "DsHidMini.json"));

        DshmConfigManager first = CreateManager();
        Assert.True(first.LastMigrationResult.Succeeded);

        DshmConfigManager second = CreateManager();
        Assert.False(second.LastMigrationResult.Attempted);
        Assert.True(second.LastMigrationResult.Succeeded);
        Assert.Single(Directory.GetFiles(DriverDir, "DsHidMini.json.pre-controlapp-*"));
    }

    [Fact]
    public void MissingProfileGuid_FallsBackToGlobal()
    {
        DshmConfigManager manager = CreateManager();
        DeviceData device = manager.GetDeviceData("AABBCCDDEEFF");
        device.SettingsMode = SettingsModes.Profile;
        device.GuidOfProfileToUse = Guid.NewGuid();

        DeviceSettings effective = manager.ResolveEffectiveSettings(device);

        Assert.Equal(SettingsModes.Global, device.SettingsMode);
        Assert.Equal(ProfileData.DefaultGuid, device.GuidOfProfileToUse);
        Assert.Same(manager.GlobalProfile.Settings, effective);
        Assert.Equal(SettingsContext.XInput, manager.GetDeviceExpectedHidMode(device));
    }

    [Fact]
    public void ApplySettings_ReportsFailure_WhenDirectoryIsAFile()
    {
        DshmConfigLocations locations = new(UserDir, Path.Combine(_root, "not-a-dir"));
        File.WriteAllText(locations.DriverConfigDirectory, "blocked");
        DshmConfigManagerUserData userData = DshmConfigManagerUserData.Load(locations);
        userData.SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion;
        DshmConfigManager manager = new(userData, locations);

        Assert.False(manager.ApplySettings());
    }

    [Fact]
    public void SaveChangesAndUpdate_WhenDriverWriteFails_RestoresPreviousUserData()
    {
        DshmConfigLocations locations = new(UserDir, Path.Combine(_root, "not-a-dir"));
        File.WriteAllText(locations.DriverConfigDirectory, "blocked");

        DshmConfigManagerUserData userData = DshmConfigManagerUserData.Load(locations);
        userData.SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion;
        userData.AutoRestartOnHidModeMismatch = true;
        userData.Save(locations);
        string original = File.ReadAllText(locations.UserDataFilePath);

        DshmConfigManager manager = new(userData, locations);
        manager.AutoRestartOnHidModeMismatch = false;

        Assert.False(manager.SaveChangesAndUpdateDsHidMiniConfigFile());
        Assert.Equal(original, File.ReadAllText(locations.UserDataFilePath));
    }

    [Fact]
    public void UserData_CorruptFile_IsBackedUp()
    {
        string userFile = Path.Combine(UserDir, "DshmUserData.json");
        Directory.CreateDirectory(UserDir);
        File.WriteAllText(userFile, "{ broken");

        DshmConfigManagerUserData loaded = DshmConfigManagerUserData.Load(new DshmConfigLocations(UserDir, DriverDir));

        Assert.Equal(0, loaded.SchemaVersion);
        Assert.NotEmpty(Directory.GetFiles(UserDir, "DshmUserData.json.corrupt-*"));
    }

    private DshmConfigManager CreateManager() =>
        new(new DshmConfigLocations(UserDir, DriverDir));
}
