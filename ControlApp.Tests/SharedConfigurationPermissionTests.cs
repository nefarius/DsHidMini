using System.Security.AccessControl;
using System.Security.Principal;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Util;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class SharedConfigurationPermissionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dshm-acl-" + Guid.NewGuid().ToString("N"));

    public SharedConfigurationPermissionTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Explicit ACLs can prevent recursive deletion. The directory name is unique.
        }
    }

    [Fact]
    public void EnsureLayout_CreatesDriverFileAndGrantsNarrowUserAccess()
    {
        SharedConfigurationPermissions.EnsureLayout(_root);

        string driverFile = Path.Combine(_root, "DsHidMini", "DsHidMini.json");
        string userDataDirectory = Path.Combine(_root, "DsHidMini", "ControlApp");
        Assert.True(File.Exists(driverFile));
        Assert.Equal(SharedConfigurationPermissions.MinimalDriverConfigJson, File.ReadAllText(driverFile));
        Assert.True(Directory.Exists(userDataDirectory));

        FileSystemAccessRule driverRule = FindExplicitUsersAllow(new FileInfo(driverFile).GetAccessControl());
        Assert.True(driverRule.FileSystemRights.HasFlag(FileSystemRights.WriteData));
        Assert.False(driverRule.FileSystemRights.HasFlag(FileSystemRights.Delete));

        FileSystemAccessRule directoryRule =
            FindExplicitUsersAllow(new DirectoryInfo(userDataDirectory).GetAccessControl());
        Assert.True(directoryRule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles));
        Assert.True(directoryRule.FileSystemRights.HasFlag(FileSystemRights.DeleteSubdirectoriesAndFiles));
        Assert.False(directoryRule.FileSystemRights.HasFlag(FileSystemRights.Delete));
    }

    [Fact]
    public void EnsureLayout_PreservesExistingDriverConfig_AndIsIdempotent()
    {
        string driverDirectory = Path.Combine(_root, "DsHidMini");
        Directory.CreateDirectory(driverDirectory);
        string driverFile = Path.Combine(driverDirectory, "DsHidMini.json");
        const string existing = "{\r\n  \"IPCEnabled\": false\r\n}\r\n";
        File.WriteAllText(driverFile, existing);

        SharedConfigurationPermissions.EnsureLayout(_root);
        string firstSddl = new FileInfo(driverFile).GetAccessControl()
            .GetSecurityDescriptorSddlForm(AccessControlSections.Access);
        SharedConfigurationPermissions.EnsureLayout(_root);

        Assert.Equal(existing, File.ReadAllText(driverFile));
        Assert.Equal(
            firstSddl,
            new FileInfo(driverFile).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access));
    }

    [Fact]
    public void CanPersist_ReflectsDriverFileWriteAccess()
    {
        SharedConfigurationPermissions.EnsureLayout(_root);
        DshmConfigLocations locations = new(
            Path.Combine(_root, "DsHidMini", "ControlApp"),
            Path.Combine(_root, "DsHidMini"));

        Assert.True(SharedConfigurationAccess.CanPersist(locations));

        FileInfo driverFile = new(locations.DriverConfigFilePath);
        FileSecurity security = driverFile.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(
            WindowsIdentity.GetCurrent().User!,
            FileSystemRights.Write,
            AccessControlType.Deny));
        driverFile.SetAccessControl(security);

        Assert.False(SharedConfigurationAccess.CanPersist(locations));
    }

    [Fact]
    public void CanPersist_IsFalseWhenMissingDriverFileCannotBeCreated()
    {
        string driverDirectory = Path.Combine(_root, "driver");
        string userDataDirectory = Path.Combine(_root, "user");
        Directory.CreateDirectory(driverDirectory);
        Directory.CreateDirectory(userDataDirectory);
        DirectoryInfo directory = new(driverDirectory);
        DirectorySecurity security = directory.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(
            WindowsIdentity.GetCurrent().User!,
            FileSystemRights.CreateFiles,
            AccessControlType.Deny));
        directory.SetAccessControl(security);

        DshmConfigLocations locations = new(userDataDirectory, driverDirectory);

        Assert.False(SharedConfigurationAccess.CanPersist(locations));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void CanRestartDevice_AllowsWirelessWithoutElevation(bool isWireless, bool isElevated, bool expected)
    {
        Assert.Equal(expected, ControlAppActionPolicy.CanRestartDevice(isWireless, isElevated));
    }

    [Theory]
    [InlineData(true, false, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, true, false)]
    public void CanSwitchDefenderBt_RequiresElevationAndAnIdleDevice(
        bool isDetected,
        bool isSwitching,
        bool isElevated,
        bool expected)
    {
        Assert.Equal(
            expected,
            ControlAppActionPolicy.CanSwitchDefenderBt(isDetected, isSwitching, isElevated));
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void CanAutoRepairBthPs3Settings_RequiresElevationAndABrokenInstall(
        bool isElevated,
        bool filterAvailable,
        bool settingsIncorrect,
        bool expected)
    {
        Assert.Equal(
            expected,
            ControlAppActionPolicy.CanAutoRepairBthPs3Settings(
                isElevated,
                filterAvailable,
                settingsIncorrect));
    }

    private static FileSystemAccessRule FindExplicitUsersAllow(FileSystemSecurity security)
    {
        SecurityIdentifier users = new(WellKnownSidType.BuiltinUsersSid, null);
        return security.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
                   .OfType<FileSystemAccessRule>()
                   .Single(rule => rule.IdentityReference.Equals(users) &&
                                   rule.AccessControlType == AccessControlType.Allow);
    }
}
