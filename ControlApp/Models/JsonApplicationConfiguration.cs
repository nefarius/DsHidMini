//-----------------------------------------------------------------------
// <copyright file="JsonDshmUserData.cs" company="Visual JSON Editor">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://visualjsoneditor.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.IO;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>Provides methods to load and save the application configuration. </summary>
public static class JsonApplicationConfiguration
{
    private const string ConfigExtension = ".json";

    /// <summary>Loads the application configuration. </summary>
    /// <typeparam name="T">The type of the application configuration. </typeparam>
    /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
    /// <param name="storeInAppData">Defines if the configuration file should be loaded from the user's AppData directory. </param>
    /// <returns>The configuration object. </returns>
    /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
    public static T Load<T>(string fileNameWithoutExtension, bool storeInAppData)
        where T : new() =>
        LoadResult<T>(fileNameWithoutExtension, storeInAppData).Configuration;

    /// <summary>Saves the configuration. </summary>
    /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
    /// <param name="configuration">The configuration object to store. </param>
    /// <param name="storeInAppData">Defines if the configuration file should be stored in the user's AppData directory. </param>
    /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
    public static void Save<T>(string fileNameWithoutExtension, T configuration, bool storeInAppData) where T : new()
    {
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);
        SaveToPath(configPath, configuration);
    }

    /// <summary>
    ///     Test seam: load from an explicit directory instead of AppData.
    /// </summary>
    internal static T Load<T>(string fileNameWithoutExtension, string directory)
        where T : new() =>
        LoadResult<T>(fileNameWithoutExtension, directory).Configuration;

    internal static ConfigurationLoadResult<T> LoadResult<T>(string fileNameWithoutExtension, bool storeInAppData)
        where T : new()
    {
        return LoadResult<T>(() =>
            CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData));
    }

    internal static ConfigurationLoadResult<T> LoadResult<T>(string fileNameWithoutExtension, string directory)
        where T : new()
    {
        return LoadResult<T>(() =>
            CreateFilePath(fileNameWithoutExtension, ConfigExtension, directory));
    }

    private static ConfigurationLoadResult<T> LoadResult<T>(Func<string> resolvePath)
        where T : new()
    {
        try
        {
            return LoadFromPath<T>(resolvePath());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Error(ex,
                "Failed to resolve or persist configuration. Using in-memory defaults without writing.");
            return new ConfigurationLoadResult<T>(new T(), false);
        }
    }

    internal readonly record struct ConfigurationLoadResult<T>(T Configuration, bool PersistenceEnabled)
        where T : new();

    /// <summary>
    ///     Test seam: save to an explicit directory instead of AppData.
    /// </summary>
    internal static void Save<T>(string fileNameWithoutExtension, T configuration, string directory) where T : new()
    {
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, directory);
        SaveToPath(configPath, configuration);
    }

    internal static void BackupCorruptFile(string configPath)
    {
        try
        {
            string backupPath = $"{configPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Copy(configPath, backupPath, overwrite: true);
            Log.Logger.Warning("Backed up unreadable configuration to {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to back up corrupt configuration file {ConfigPath}", configPath);
        }
    }

    private static ConfigurationLoadResult<T> LoadFromPath<T>(string configPath) where T : new()
    {
        string content;
        try
        {
            content = File.ReadAllText(configPath, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            try
            {
                return new ConfigurationLoadResult<T>(CreateDefaultConfigurationFile<T>(configPath), true);
            }
            catch (Exception createEx) when (createEx is IOException or UnauthorizedAccessException)
            {
                Log.Logger.Error(createEx,
                    "Failed to create default configuration at {ConfigPath}. Using in-memory defaults.",
                    configPath);
                return new ConfigurationLoadResult<T>(new T(), false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Error(ex,
                "Failed to read configuration from {ConfigPath}. Using in-memory defaults without replacing the file.",
                configPath);
            return new ConfigurationLoadResult<T>(new T(), false);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Logger.Error(
                    "Configuration file {ConfigPath} is empty. Backing up and using defaults.",
                    configPath);
                return new ConfigurationLoadResult<T>(RecoverWithDefaults<T>(configPath), true);
            }

            T? loaded = JsonConvert.DeserializeObject<T>(content);
            if (loaded is not null)
            {
                return new ConfigurationLoadResult<T>(loaded, true);
            }

            Log.Logger.Error(
                "Configuration file {ConfigPath} deserialized to null. Backing up and using defaults.",
                configPath);
            return new ConfigurationLoadResult<T>(RecoverWithDefaults<T>(configPath), true);
        }
        catch (JsonException ex)
        {
            Log.Logger.Error(ex, "Failed to load configuration from {ConfigPath}. Backing up corrupt file.",
                configPath);
            return new ConfigurationLoadResult<T>(RecoverWithDefaults<T>(configPath), true);
        }
    }

    private static void SaveToPath<T>(string configPath, T configuration) where T : new()
    {
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());

        string? directoryPath = Path.GetDirectoryName(configPath);
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string tempPath = $"{configPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(configuration, Formatting.Indented, settings),
                Encoding.UTF8);
            try
            {
                if (File.Exists(configPath))
                {
                    File.Replace(tempPath, configPath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempPath, configPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (!File.Exists(configPath))
                {
                    throw;
                }

                Log.Logger.Warning(ex,
                    "Atomic replace of configuration file {ConfigPath} failed. Falling back to in-place overwrite.",
                    configPath);
                OverwriteExistingFile(tempPath, configPath);
            }
        }
        finally
        {
            TryDeleteTemporaryFile(tempPath);
        }
    }

    private static T RecoverWithDefaults<T>(string configPath) where T : new()
    {
        BackupCorruptFile(configPath);
        return CreateDefaultConfigurationFile<T>(configPath);
    }

    private static string CreateFilePath(string fileNameWithoutExtension, string extension, bool storeInAppData)
    {
        if (storeInAppData)
        {
            return CreateFilePath(
                fileNameWithoutExtension,
                extension,
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        return fileNameWithoutExtension + extension;
    }

    private static string CreateFilePath(string fileNameWithoutExtension, string extension, string directory)
    {
        string filePath = Path.Combine(directory, fileNameWithoutExtension) + extension;
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return filePath;
    }

    private static T CreateDefaultConfigurationFile<T>(string configPath)
        where T : new()
    {
        T config = new();
        SaveToPath(configPath, config);
        return config;
    }

    private static void OverwriteExistingFile(string sourcePath, string destinationPath)
    {
        byte[] content = File.ReadAllBytes(sourcePath);
        string backupPath = $"{destinationPath}.{Guid.NewGuid():N}.bak";
        File.Copy(destinationPath, backupPath, overwrite: true);
        try
        {
            using FileStream destination = new(destinationPath, FileMode.Open, FileAccess.Write, FileShare.Read);
            destination.Write(content, 0, content.Length);
            destination.SetLength(content.Length);
            destination.Flush(flushToDisk: true);
        }
        catch
        {
            try
            {
                File.Copy(backupPath, destinationPath, overwrite: true);
            }
            catch (Exception restoreEx) when (restoreEx is IOException or UnauthorizedAccessException)
            {
                Log.Logger.Error(restoreEx,
                    "Failed to restore configuration file {ConfigPath} from backup {BackupPath}.",
                    destinationPath, backupPath);
            }

            throw;
        }
        finally
        {
            TryDeleteTemporaryFile(backupPath);
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception cleanupEx) when (cleanupEx is IOException or UnauthorizedAccessException)
        {
            Log.Logger.Debug(cleanupEx, "Failed to delete temporary configuration file {TempPath}.", path);
        }
    }
}
