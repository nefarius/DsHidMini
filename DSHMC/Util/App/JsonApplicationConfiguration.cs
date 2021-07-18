//-----------------------------------------------------------------------
// <copyright file="JsonApplicationConfiguration.cs" company="Visual JSON Editor">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://visualjsoneditor.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JsonSchema = NJsonSchema.JsonSchema;

namespace Nefarius.DsHidMini.Util.App
{
    /// <summary>Provides methods to load and save the application configuration. </summary>
    public static class JsonApplicationConfiguration
    {
        private const string ConfigExtension = ".json";

        /// <summary>Loads the application configuration. </summary>
        /// <typeparam name="T">The type of the application configuration. </typeparam>
        /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
        /// <param name="alwaysCreateNewSchemaFile">Defines if the schema file should always be generated and overwritten. </param>
        /// <param name="storeInAppData">Defines if the configuration file should be loaded from the user's AppData directory. </param>
        /// <returns>The configuration object. </returns>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        public static T Load<T>(string fileNameWithoutExtension, bool alwaysCreateNewSchemaFile, bool storeInAppData) where T : new()
        {
            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);

            if (!File.Exists(configPath))
                return CreateDefaultConfigurationFile<T>(fileNameWithoutExtension, storeInAppData);

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath, Encoding.UTF8));
        }

        /// <summary>Saves the configuration. </summary>
        /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
        /// <param name="configuration">The configuration object to store. </param>
        /// <param name="storeInAppData">Defines if the configuration file should be stored in the user's AppData directory. </param>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        public static void Save<T>(string fileNameWithoutExtension, T configuration, bool storeInAppData) where T : new()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());

            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);
            File.WriteAllText(configPath, JsonConvert.SerializeObject(configuration, Formatting.Indented, settings), Encoding.UTF8);
        }

        private static string CreateFilePath(string fileNameWithoutExtension, string extension, bool storeInAppData)
        {
            if (storeInAppData)
            {
                var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var filePath = Path.Combine(appDataDirectory, fileNameWithoutExtension) + extension;

                var directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                return filePath;
            }
            return fileNameWithoutExtension + extension;
        }

        private static T CreateDefaultConfigurationFile<T>(string fileNameWithoutExtension, bool storeInAppData) where T : new()
        {
            var config = new T();
            var configData = JsonConvert.SerializeObject(config, Formatting.Indented);
            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);

            File.WriteAllText(configPath, configData, Encoding.UTF8);
            return config;
        }
    }
}