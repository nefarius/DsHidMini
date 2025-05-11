﻿//-----------------------------------------------------------------------
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

/// <summary>Provides methods to load and save the application configuration. </summary>
public static class JsonDshmUserData
{
    private const string ConfigExtension = ".json";

    /// <summary>Loads the application configuration. </summary>
    /// <typeparam name="T">The type of the application configuration. </typeparam>
    /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
    /// <param name="alwaysCreateNewSchemaFile">Defines if the schema file should always be generated and overwritten. </param>
    /// <param name="userDataDir">Defines the directory the UserData file will be loaded from. </param>
    /// <returns>The configuration object. </returns>
    /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
    public static T Load<T>(string fileNameWithoutExtension, bool alwaysCreateNewSchemaFile, string userDataDir)
        where T : new()
    {
        Log.Logger.Debug($"Loading DsHidMini User Data from {fileNameWithoutExtension} file in {userDataDir}.");
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, userDataDir);

        if (!File.Exists(configPath))
        {
            Log.Logger.Debug("User Data file does not exist in the specified directory. Creating new User Data.");
            return CreateDefaultConfigurationFile<T>(fileNameWithoutExtension, userDataDir);
        }

        // Handle errors on deserialization
        JsonSerializerSettings settings = new() { Error = HandleDeserializationError };
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath, Encoding.UTF8), settings);
    }

    /// <summary>Saves the configuration. </summary>
    /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
    /// <param name="configuration">The configuration object to store. </param>
    /// <param name="userDataDir">Defines the directory the UserData file will be stored. </param>
    /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
    public static void Save<T>(string fileNameWithoutExtension, T configuration, string userDataDir) where T : new()
    {
        Log.Logger.Debug($"Saving DsHidMini User Data to {fileNameWithoutExtension} in dir {userDataDir}");
        JsonSerializerSettings settings = new();
        settings.Converters.Add(new StringEnumConverter());

        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, userDataDir);
        File.WriteAllText(configPath, JsonConvert.SerializeObject(configuration, Formatting.Indented, settings),
            Encoding.UTF8);
    }

    private static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
    {
        string currentError = errorArgs.ErrorContext.Error.Message;
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
        T? config = new();
        string configData = JsonConvert.SerializeObject(config, Formatting.Indented);
        string configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, userDataDir);

        File.WriteAllText(configPath, configData, Encoding.UTF8);
        return config;
    }
}