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
        Assert.True(manager.AutoRestartOnHidModeMismatch);
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

public class UserDataLocationMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dshm-loc-" + Guid.NewGuid().ToString("N"));

    public UserDataLocationMigrationTests()
    {
        Directory.CreateDirectory(ProgramData);
        Directory.CreateDirectory(DriverDir);
    }

    private string ProgramData => _root;
    private string DriverDir => Path.Combine(_root, "DsHidMini");
    private string PreferredDir => Path.Combine(DriverDir, "ControlApp");
    private string LegacyDir => Path.Combine(ProgramData, "ControlApp");
    private string PreferredFile => Path.Combine(PreferredDir, "DshmUserData.json");
    private string LegacyFile => Path.Combine(LegacyDir, "DshmUserData.json");

    public void Dispose()
    {
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
    public void MigratesLegacyFile_AndRemovesEmptyLegacyDirectory()
    {
        Directory.CreateDirectory(LegacyDir);
        File.WriteAllText(LegacyFile, """{"SchemaVersion":1}""");

        DshmConfigLocations locations = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);

        Assert.Equal(Path.GetFullPath(PreferredDir), locations.UserDataDirectory);
        Assert.True(File.Exists(PreferredFile));
        Assert.Equal("""{"SchemaVersion":1}""", File.ReadAllText(PreferredFile));
        Assert.False(File.Exists(LegacyFile));
        Assert.False(Directory.Exists(LegacyDir));
    }

    [Fact]
    public void MigratesLegacyArtifacts_AndRemovesEmptyLegacyDirectory()
    {
        Directory.CreateDirectory(LegacyDir);
        File.WriteAllText(LegacyFile, """{"SchemaVersion":1}""");
        string corruptName = "DshmUserData.json.corrupt-20260101120000";
        string tmpName = "DshmUserData.json.tmp";
        File.WriteAllText(Path.Combine(LegacyDir, corruptName), "broken");
        File.WriteAllText(Path.Combine(LegacyDir, tmpName), "tmp");

        DshmConfigLocations locations = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);

        Assert.Equal(Path.GetFullPath(PreferredDir), locations.UserDataDirectory);
        Assert.Equal("""{"SchemaVersion":1}""", File.ReadAllText(PreferredFile));
        Assert.Equal("broken", File.ReadAllText(Path.Combine(PreferredDir, corruptName)));
        Assert.Equal("tmp", File.ReadAllText(Path.Combine(PreferredDir, tmpName)));
        Assert.False(Directory.Exists(LegacyDir));
    }

    [Fact]
    public void NoLegacyData_UsesPreferredDirectory()
    {
        DshmConfigLocations locations = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);

        Assert.Equal(Path.GetFullPath(PreferredDir), locations.UserDataDirectory);
        Assert.False(File.Exists(PreferredFile));
        Assert.False(File.Exists(LegacyFile));
    }

    [Fact]
    public void ExistingDestination_IsAuthoritative_AndIdempotent()
    {
        Directory.CreateDirectory(LegacyDir);
        Directory.CreateDirectory(PreferredDir);
        File.WriteAllText(LegacyFile, """{"SchemaVersion":1,"Source":"legacy"}""");
        File.WriteAllText(PreferredFile, """{"SchemaVersion":1,"Source":"preferred"}""");

        DshmConfigLocations first = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);
        DshmConfigLocations second = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);

        Assert.Equal(Path.GetFullPath(PreferredDir), first.UserDataDirectory);
        Assert.Equal(first.UserDataDirectory, second.UserDataDirectory);
        Assert.Equal("""{"SchemaVersion":1,"Source":"preferred"}""", File.ReadAllText(PreferredFile));
        Assert.True(File.Exists(LegacyFile));
    }

    [Fact]
    public void FailedMove_FallsBackToLegacyDirectory()
    {
        Directory.CreateDirectory(LegacyDir);
        File.WriteAllText(LegacyFile, """{"SchemaVersion":1}""");
        File.WriteAllText(PreferredDir, "blocked");

        DshmConfigLocations locations = DshmConfigLocations.CreateDefault(ProgramData, DriverDir);

        Assert.Equal(Path.GetFullPath(LegacyDir), locations.UserDataDirectory);
        Assert.True(File.Exists(LegacyFile));
        Assert.Equal("""{"SchemaVersion":1}""", File.ReadAllText(LegacyFile));
    }
}
