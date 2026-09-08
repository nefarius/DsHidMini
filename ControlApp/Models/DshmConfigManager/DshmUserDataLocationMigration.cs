using System.IO;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     One-time move of ControlApp user data from the legacy ProgramData folder into
///     <c>%ProgramData%\DsHidMini\ControlApp</c>.
/// </summary>
internal static class DshmUserDataLocationMigration
{
    private static readonly string UserDataFileName =
        DshmConfigManagerUserData.GlobalUserDataFileName + ".json";

    public static string ResolveUserDataDirectory(string preferredDirectory, string legacyDirectory)
    {
        string preferredFull = Path.GetFullPath(preferredDirectory);
        string legacyFull = Path.GetFullPath(legacyDirectory);
        if (string.Equals(preferredFull, legacyFull, StringComparison.OrdinalIgnoreCase))
        {
            return preferredFull;
        }

        string preferredFile = UserDataFilePath(preferredFull);
        string legacyFile = UserDataFilePath(legacyFull);

        if (File.Exists(preferredFile))
        {
            TryMigrateSidecarArtifacts(legacyFull, preferredFull);
            TryDeleteEmptyDirectory(legacyFull);
            return preferredFull;
        }

        if (!File.Exists(legacyFile))
        {
            TryMigrateSidecarArtifacts(legacyFull, preferredFull);
            TryDeleteEmptyDirectory(legacyFull);
            return preferredFull;
        }

        try
        {
            Directory.CreateDirectory(preferredFull);
            File.Move(legacyFile, preferredFile);
            Log.Logger.Information(
                "Migrated ControlApp user data from {LegacyPath} to {PreferredPath}.",
                legacyFile, preferredFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Error(ex,
                "Failed to migrate ControlApp user data from {LegacyPath} to {PreferredPath}. Using the legacy location for this run.",
                legacyFile, preferredFile);
            return legacyFull;
        }

        TryMigrateSidecarArtifacts(legacyFull, preferredFull);
        TryDeleteEmptyDirectory(legacyFull);
        return preferredFull;
    }

    private static string UserDataFilePath(string directory) =>
        Path.Combine(directory, UserDataFileName);

    private static void TryMigrateSidecarArtifacts(string legacyDirectory, string preferredDirectory)
    {
        foreach (string sourcePath in EnumerateUserDataArtifacts(legacyDirectory))
        {
            string fileName = Path.GetFileName(sourcePath);
            if (string.Equals(fileName, UserDataFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string destinationPath = Path.Combine(preferredDirectory, fileName);
            if (File.Exists(destinationPath))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(preferredDirectory);
                File.Move(sourcePath, destinationPath);
                Log.Logger.Information(
                    "Migrated ControlApp user-data artifact from {LegacyPath} to {PreferredPath}.",
                    sourcePath, destinationPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Logger.Warning(ex,
                    "Failed to migrate ControlApp user-data artifact from {LegacyPath} to {PreferredPath}.",
                    sourcePath, destinationPath);
            }
        }
    }

    private static IEnumerable<string> EnumerateUserDataArtifacts(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, UserDataFileName + "*");
    }

    private static void TryDeleteEmptyDirectory(string directory)
    {
        try
        {
            if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
            {
                return;
            }

            Directory.Delete(directory);
            Log.Logger.Debug("Removed empty leftover ControlApp directory {Directory}.", directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Warning(ex, "Failed to remove leftover ControlApp directory {Directory}.", directory);
        }
    }
}
