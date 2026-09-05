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

using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

/// <summary>Provides methods to load and save the ControlApp user-data store.</summary>
public static class JsonDshmUserData
{
    private const string ConfigExtension = ".json";

    public static T Load<T>(string fileNameWithoutExtension, string userDataDir, bool createIfMissing = true)
        where T : new()
    {
        Log.Logger.Debug(
            "Loading DsHidMini User Data from {FileNameWithoutExtension} file in {UserDataDir}.",
            fileNameWithoutExtension, userDataDir);
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, userDataDir);

        if (!File.Exists(configPath))
        {
            Log.Logger.Debug("User Data file does not exist in the specified directory.");
            return createIfMissing
                ? CreateDefaultConfigurationFile<T>(fileNameWithoutExtension, userDataDir)
                : new T();
        }

        try
        {
            JsonSerializerSettings settings = new() { Error = HandleDeserializationError };
            T? loaded = JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath, Encoding.UTF8), settings);
            if (loaded is not null)
            {
                return loaded;
            }

            Log.Logger.Error("User Data file {ConfigPath} deserialized to null. Backing up and using defaults.",
                configPath);
            BackupCorruptFile(configPath);
            return new T();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to load User Data from {ConfigPath}. Backing up corrupt file.", configPath);
            BackupCorruptFile(configPath);
            return new T();
        }
    }

    public static void Save<T>(string fileNameWithoutExtension, T configuration, string userDataDir) where T : new()
    {
        Log.Logger.Debug(
            "Saving DsHidMini User Data to {FileNameWithoutExtension} in dir {UserDataDir}", fileNameWithoutExtension,
            userDataDir);
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());

        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, userDataDir);
        string tempPath = configPath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(configuration, Formatting.Indented, settings),
                Encoding.UTF8);
            File.Move(tempPath, configPath, overwrite: true);
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception cleanupEx) when (cleanupEx is IOException or UnauthorizedAccessException)
            {
                Log.Logger.Debug(cleanupEx, "Failed to delete temporary User Data file {TempPath}.", tempPath);
            }

            throw;
        }
    }

    internal static void BackupCorruptFile(string configPath)
    {
        try
        {
            string backupPath = $"{configPath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Copy(configPath, backupPath, overwrite: true);
            Log.Logger.Warning("Backed up unreadable User Data to {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to back up corrupt User Data file {ConfigPath}", configPath);
        }
    }

    private static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
    {
        Log.Logger.Warning("User Data deserialization error at {Path}: {Message}",
            errorArgs.ErrorContext.Path, errorArgs.ErrorContext.Error.Message);
        errorArgs.ErrorContext.Handled = true;
    }

    private static string CreateFilePath(string fileNameWithoutExtension, string extension, string? userDataDir)
    {
        if (userDataDir != null)
        {
            string filePath = Path.Combine(userDataDir, fileNameWithoutExtension) + extension;
            string? directoryPath = Path.GetDirectoryName(filePath);
            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                Log.Logger.Debug("Specified directory of DsHidMini User Data does not exist. Creating directory.");
                Directory.CreateDirectory(directoryPath);
            }

            return filePath;
        }

        return fileNameWithoutExtension + extension;
    }

    private static T CreateDefaultConfigurationFile<T>(string fileNameWithoutExtension, string userDataDir)
        where T : new()
    {
        Log.Logger.Debug("Creating default configuration file for DsHidMini User Data.");
        T config = new();
        Save(fileNameWithoutExtension, config, userDataDir);
        return config;
    }
}
