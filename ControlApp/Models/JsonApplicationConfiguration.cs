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
        where T : new()
    {
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);
        return LoadFromPath<T>(configPath);
    }

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
        where T : new()
    {
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, directory);
        return LoadFromPath<T>(configPath);
    }

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

    private static T LoadFromPath<T>(string configPath) where T : new()
    {
        if (!File.Exists(configPath))
        {
            return CreateDefaultConfigurationFile<T>(configPath);
        }

        try
        {
            string content = File.ReadAllText(configPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Logger.Error(
                    "Configuration file {ConfigPath} is empty. Backing up and using defaults.",
                    configPath);
                return RecoverWithDefaults<T>(configPath);
            }

            T? loaded = JsonConvert.DeserializeObject<T>(content);
            if (loaded is not null)
            {
                return loaded;
            }

            Log.Logger.Error(
                "Configuration file {ConfigPath} deserialized to null. Backing up and using defaults.",
                configPath);
            return RecoverWithDefaults<T>(configPath);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to load configuration from {ConfigPath}. Backing up corrupt file.",
                configPath);
            return RecoverWithDefaults<T>(configPath);
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

        string tempPath = configPath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(configuration, Formatting.Indented, settings),
                Encoding.UTF8);
            try
            {
                File.Move(tempPath, configPath, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
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
        using FileStream source = File.OpenRead(sourcePath);
        using FileStream destination = new(destinationPath, FileMode.Open, FileAccess.Write, FileShare.Read);
        destination.SetLength(0);
        source.CopyTo(destination);
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
