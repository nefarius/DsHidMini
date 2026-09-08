using System.IO;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     One-time move of ControlApp user data from the legacy ProgramData folder into
///     <c>%ProgramData%\DsHidMini\ControlApp</c>.
/// </summary>
internal static class DshmUserDataLocationMigration
{
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
            TryDeleteEmptyDirectory(legacyFull);
            return preferredFull;
        }

        if (!File.Exists(legacyFile))
        {
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

        TryDeleteEmptyDirectory(legacyFull);
        return preferredFull;
    }

    private static string UserDataFilePath(string directory) =>
        Path.Combine(directory, DshmConfigManagerUserData.GlobalUserDataFileName + ".json");

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
