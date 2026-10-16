#nullable enable

using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Grants standard users modify access to the shared DsHidMini configuration without opening the
///     rest of ProgramData. The driver directory stays protected. Users can update the existing driver
///     file in place, and can create the temporary files required under the ControlApp data directory.
/// </summary>
public static class SharedConfigurationPermissions
{
    public const string DriverDirectoryName = "DsHidMini";
    public const string DriverFileName = "DsHidMini.json";
    public const string UserDataDirectoryName = "ControlApp";
    public const string UserDataFileName = "DshmUserData.json";

    public const string MinimalDriverConfigJson = "{\r\n  \"IPCEnabled\": true\r\n}\r\n";

    /// <summary>
    ///     Write and create rights for the user-data directory. Delete-child is included so a save can
    ///     replace files inside the directory. Delete of the directory itself is not granted.
    /// </summary>
    public const FileSystemRights UserDataDirectoryRights =
        FileSystemRights.ReadAndExecute |
        FileSystemRights.Write |
        FileSystemRights.DeleteSubdirectoriesAndFiles |
        FileSystemRights.Synchronize;

    /// <summary>
    ///     In-place write access for <c>DsHidMini.json</c>. Delete is omitted so a standard user cannot
    ///     remove the file from the protected parent directory and then be unable to recreate it.
    /// </summary>
    public static readonly FileSystemRights DriverConfigFileRights =
        FileSystemRights.Modify & ~FileSystemRights.Delete;

    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static void EnsureLayout(string programDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(programDataDirectory))
        {
            throw new ArgumentException("ProgramData directory is required.", nameof(programDataDirectory));
        }

        string driverDirectory = Path.Combine(programDataDirectory, DriverDirectoryName);
        string userDataDirectory = Path.Combine(driverDirectory, UserDataDirectoryName);
        string driverConfigPath = Path.Combine(driverDirectory, DriverFileName);
        string legacyUserDataDirectory = Path.Combine(programDataDirectory, UserDataDirectoryName);

        Directory.CreateDirectory(driverDirectory);
        Directory.CreateDirectory(userDataDirectory);
        if (!File.Exists(driverConfigPath))
        {
            File.WriteAllText(driverConfigPath, MinimalDriverConfigJson, Utf8NoBom);
        }

        GrantUserDataDirectoryAccess(userDataDirectory);
        GrantDriverConfigFileAccess(driverConfigPath);

        if (Directory.Exists(legacyUserDataDirectory) &&
            File.Exists(Path.Combine(legacyUserDataDirectory, UserDataFileName)))
        {
            GrantUserDataDirectoryAccess(legacyUserDataDirectory);
        }
    }

    public static bool TryEnsureLayout(string programDataDirectory, bool isElevated)
    {
        if (!isElevated)
        {
            return false;
        }

        try
        {
            EnsureLayout(programDataDirectory);
            return true;
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or System.Security.SecurityException
                                       or PrivilegeNotHeldException
                                       or ArgumentException)
        {
            return false;
        }
    }

    public static void GrantDriverConfigFileAccess(string driverConfigFilePath)
    {
        FileInfo file = new FileInfo(driverConfigFilePath);
        FileSecurity security = file.GetAccessControl();
        GrantFullControl(security, WellKnownSidType.LocalSystemSid);
        GrantFullControl(security, WellKnownSidType.BuiltinAdministratorsSid);
        security.SetAccessRule(new FileSystemAccessRule(
            UsersSid(),
            DriverConfigFileRights,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Allow));
        file.SetAccessControl(security);
    }

    public static bool TryGrantDriverConfigFileAccess(string driverConfigFilePath)
    {
        try
        {
            GrantDriverConfigFileAccess(driverConfigFilePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException
                                       or UnauthorizedAccessException
                                       or System.Security.SecurityException
                                       or PrivilegeNotHeldException)
        {
            return false;
        }
    }

    public static void GrantUserDataDirectoryAccess(string userDataDirectory)
    {
        DirectoryInfo directory = new DirectoryInfo(userDataDirectory);
        DirectorySecurity security = directory.GetAccessControl();
        GrantFullControl(
            security,
            WellKnownSidType.LocalSystemSid,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        GrantFullControl(
            security,
            WellKnownSidType.BuiltinAdministratorsSid,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        security.SetAccessRule(new FileSystemAccessRule(
            UsersSid(),
            UserDataDirectoryRights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        directory.SetAccessControl(security);
    }

    private static void GrantFullControl(FileSystemSecurity security, WellKnownSidType sid)
    {
        GrantFullControl(security, sid, InheritanceFlags.None);
    }

    private static void GrantFullControl(
        FileSystemSecurity security,
        WellKnownSidType sid,
        InheritanceFlags inheritance)
    {
        security.SetAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(sid, null),
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static SecurityIdentifier UsersSid() =>
        new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
}
