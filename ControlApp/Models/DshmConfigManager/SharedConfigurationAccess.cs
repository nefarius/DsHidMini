using System.IO;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>
///     Probes whether the current process can complete the shared-configuration save pipeline.
///     Elevation is not used as a proxy: a standard user can succeed once the installer ACLs are present.
/// </summary>
internal static class SharedConfigurationAccess
{
    public static bool CanPersist(DshmConfigLocations locations)
    {
        try
        {
            return CanCreateFiles(locations.UserDataDirectory)
                   && CanUpdateDriverConfig(locations.DriverConfigDirectory, locations.DriverConfigFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool CanCreateFiles(string directory)
    {
        Directory.CreateDirectory(directory);
        string probePath = Path.Combine(directory, ".dshm-access-" + Guid.NewGuid().ToString("N"));
        try
        {
            using FileStream stream = new(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1,
                FileOptions.DeleteOnClose);
            return stream.CanWrite;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            TryDelete(probePath);
        }
    }

    private static bool CanUpdateDriverConfig(string directory, string configPath)
    {
        if (File.Exists(configPath))
        {
            return CanWriteExistingFile(configPath);
        }

        return CanCreateFiles(directory);
    }

    private static bool CanWriteExistingFile(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            return stream.CanWrite;
        }
        catch (IOException ex) when (IsSharingViolation(ex))
        {
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsSharingViolation(IOException ex) =>
        ex.HResult == unchecked((int)0x80070020);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The probe uses DeleteOnClose. A leftover empty file does not change the access decision.
        }
    }
}
