using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

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
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public void Save_WhenDeleteIsDenied_OverwritesExistingFile()
    {
        ApplicationConfiguration original = new()
        {
            IsLoggingEnabled = false,
            MinimizeToTray = false
        };
        JsonApplicationConfiguration.Save(FileName, original, _root);

        ApplicationConfiguration changed = new()
        {
            IsLoggingEnabled = true,
            MinimizeToTray = true
        };

        using (DenyFileReplacement(ConfigPath))
        {
            string accessSddl = GetAccessSddl(ConfigPath);
            Assert.True(HasExplicitDeleteDenial(ConfigPath));
            JsonApplicationConfiguration.Save(FileName, changed, _root);
            Assert.Equal(accessSddl, GetAccessSddl(ConfigPath));
            Assert.True(HasExplicitDeleteDenial(ConfigPath));
        }

        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);
        Assert.True(loaded.IsLoggingEnabled);
        Assert.True(loaded.MinimizeToTray);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public void Save_WhenDestinationHasExplicitAcl_PreservesAcl()
    {
        ApplicationConfiguration original = new() { IsLoggingEnabled = false };
        JsonApplicationConfiguration.Save(FileName, original, _root);

        FileInfo file = new(ConfigPath);
        FileSecurity security = file.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
            FileSystemRights.Read,
            AccessControlType.Allow));
        file.SetAccessControl(security);
        string accessSddl = GetAccessSddl(ConfigPath);

        JsonApplicationConfiguration.Save(FileName, new ApplicationConfiguration { IsLoggingEnabled = true }, _root);

        Assert.Equal(accessSddl, GetAccessSddl(ConfigPath));
        ApplicationConfiguration loaded = JsonApplicationConfiguration.Load<ApplicationConfiguration>(FileName, _root);
        Assert.True(loaded.IsLoggingEnabled);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
        Assert.Empty(Directory.GetFiles(_root, "*.bak"));
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
            JsonApplicationConfiguration.ConfigurationLoadResult<ApplicationConfiguration> loaded =
                JsonApplicationConfiguration.LoadResult<ApplicationConfiguration>(FileName, _root);

            Assert.False(loaded.Configuration.IsLoggingEnabled);
            Assert.True(loaded.Configuration.IsUpdateCheckEnabled);
            Assert.False(loaded.PersistenceEnabled);
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

    private static string GetAccessSddl(string path) =>
        new FileInfo(path).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access);

    private static bool HasExplicitDeleteDenial(string path)
    {
        SecurityIdentifier user = CurrentUserSid();
        AuthorizationRuleCollection rules = new FileInfo(path)
            .GetAccessControl()
            .GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));

        return rules
            .OfType<FileSystemAccessRule>()
            .Any(rule =>
                rule.IdentityReference.Equals(user)
                && rule.AccessControlType == AccessControlType.Deny
                && rule.FileSystemRights.HasFlag(FileSystemRights.Delete));
    }

    private static RestoreReplacementDenial DenyFileReplacement(string path)
    {
        FileInfo file = new(path);
        DirectoryInfo directory = file.Directory
                                  ?? throw new InvalidOperationException("Configuration file has no parent directory.");
        FileSecurity originalFile = file.GetAccessControl();
        DirectorySecurity originalDirectory = directory.GetAccessControl();
        SecurityIdentifier user = CurrentUserSid();

        FileSecurity fileSecurity = file.GetAccessControl();
        fileSecurity.AddAccessRule(new FileSystemAccessRule(
            user,
            FileSystemRights.Delete,
            AccessControlType.Deny));
        file.SetAccessControl(fileSecurity);

        DirectorySecurity directorySecurity = directory.GetAccessControl();
        directorySecurity.AddAccessRule(new FileSystemAccessRule(
            user,
            FileSystemRights.DeleteSubdirectoriesAndFiles,
            AccessControlType.Deny));
        directory.SetAccessControl(directorySecurity);

        return new RestoreReplacementDenial(file, originalFile, directory, originalDirectory);
    }

    private static SecurityIdentifier CurrentUserSid() =>
        WindowsIdentity.GetCurrent().User
        ?? throw new InvalidOperationException("Current Windows user SID is unavailable.");

    private sealed class RestoreReplacementDenial : IDisposable
    {
        private readonly FileInfo _file;
        private readonly FileSecurity _originalFile;
        private readonly DirectoryInfo _directory;
        private readonly DirectorySecurity _originalDirectory;

        public RestoreReplacementDenial(
            FileInfo file,
            FileSecurity originalFile,
            DirectoryInfo directory,
            DirectorySecurity originalDirectory)
        {
            _file = file;
            _originalFile = originalFile;
            _directory = directory;
            _originalDirectory = originalDirectory;
        }

        public void Dispose()
        {
            _file.SetAccessControl(_originalFile);
            _directory.SetAccessControl(_originalDirectory);
        }
    }
}
