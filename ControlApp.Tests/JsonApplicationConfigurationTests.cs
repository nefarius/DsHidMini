using System.IO;

using Nefarius.DsHidMini.ControlApp.Models;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class JsonApplicationConfigurationTests : IDisposable
{
    private const string FileName = "ControlApp";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dshm-appconfig-" + Guid.NewGuid().ToString("N"));

    public JsonApplicationConfigurationTests()
    {
        Directory.CreateDirectory(_root);
    }

    private string ConfigPath => Path.Combine(_root, FileName + ".json");

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
    public void Load_MissingFile_CreatesDefaultConfiguration()
    {
        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.NotNull(loaded);
        Assert.True(File.Exists(ConfigPath));
        Assert.True(loaded.IsUpdateCheckEnabled);
        Assert.Null(loaded.CompletedOnboardingVersion);
        Assert.Null(loaded.SkippedOnboardingVersion);
    }

    [Fact]
    public void SaveAndLoad_PreservesApplicationSettings()
    {
        ApplicationConfiguration original = new()
        {
            IsLoggingEnabled = true,
            IsUpdateCheckEnabled = false,
            MinimizeToTray = true,
            CompletedOnboardingVersion = 1,
            SkippedOnboardingVersion = null
        };

        JsonApplicationConfiguration.Save(FileName, original, _root);
        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.True(loaded.IsLoggingEnabled);
        Assert.False(loaded.IsUpdateCheckEnabled);
        Assert.True(loaded.MinimizeToTray);
        Assert.Equal(1, loaded.CompletedOnboardingVersion);
        Assert.Null(loaded.SkippedOnboardingVersion);
        Assert.False(File.Exists(ConfigPath + ".tmp"));
    }

    [Fact]
    public void Load_EmptyFile_ReturnsDefaultsAndPreservesCorruptBackup()
    {
        File.WriteAllText(ConfigPath, "   ");

        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsUpdateCheckEnabled);
        Assert.Null(loaded.CompletedOnboardingVersion);
        Assert.NotEmpty(Directory.GetFiles(_root, FileName + ".json.corrupt-*"));
        Assert.True(File.Exists(ConfigPath));
    }

    [Fact]
    public void Load_JsonNull_ReturnsDefaultsAndPreservesCorruptBackup()
    {
        File.WriteAllText(ConfigPath, "null");

        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsUpdateCheckEnabled);
        Assert.Null(loaded.CompletedOnboardingVersion);
        Assert.NotEmpty(Directory.GetFiles(_root, FileName + ".json.corrupt-*"));
    }

    [Fact]
    public void Load_MalformedJson_ReturnsDefaultsAndPreservesCorruptBackup()
    {
        File.WriteAllText(ConfigPath, "{ broken");

        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsUpdateCheckEnabled);
        Assert.Null(loaded.CompletedOnboardingVersion);

        string[] backups = Directory.GetFiles(_root, FileName + ".json.corrupt-*");
        Assert.Single(backups);
        Assert.Equal("{ broken", File.ReadAllText(backups[0]));
    }

    [Fact]
    public void Load_UnreadableFile_ReturnsInMemoryDefaultsWithoutReplacingFile()
    {
        const string original = """{"IsLoggingEnabled":true,"IsUpdateCheckEnabled":false}""";
        File.WriteAllText(ConfigPath, original);

        using (new FileStream(ConfigPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            ApplicationConfiguration loaded =
                JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

            Assert.False(loaded.IsLoggingEnabled);
            Assert.True(loaded.IsUpdateCheckEnabled);
            Assert.Empty(Directory.GetFiles(_root, FileName + ".json.corrupt-*"));
        }

        Assert.Equal(original, File.ReadAllText(ConfigPath));
    }

    [Fact]
    public void Load_RecoveredDefaults_CanBeSavedAndReloaded()
    {
        File.WriteAllText(ConfigPath, "null");

        ApplicationConfiguration recovered = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);
        recovered.MinimizeToTray = true;
        JsonApplicationConfiguration.Save(FileName, recovered, _root);

        ApplicationConfiguration reloaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);

        Assert.True(reloaded.MinimizeToTray);
        Assert.True(reloaded.IsUpdateCheckEnabled);
    }
}
