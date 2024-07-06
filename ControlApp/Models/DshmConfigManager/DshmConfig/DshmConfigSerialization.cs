using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

using Serilog;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig
{
    internal static class DshmConfigSerialization
    {
        private const string DISK = @"C:\";

        private const string DSHM_FOLDER_PATH_IN_DISK = @"ProgramData\DsHidMini\";
        public static string DshmFolderFullPath { get; } = $@"{DISK}{DSHM_FOLDER_PATH_IN_DISK}";

        public static string DshmFileNameWithoutExtension { get; } = $@"DsHidMini";

        public static string DshmConfigFileFormat { get; } = $@".json";



        /// <summary>
        /// Attempts to update the DsHidMini configuration file on disk by serializing a DshmConfiguration object into the proper dshidmini v3 format 
        /// </summary>
        /// <param name="dshmConfig">The DshmConfiguration object containing the desired dshidmini v3 settings</param>
        /// <returns>True if the update occurred successfully, false otherwise</returns>
        public static bool UpdateDsHidMiniConfigFile(DshmConfiguration dshmConfig)
        {
            Log.Logger.Debug("Starting serialization of DsHidMini config object and saving to disk");
            try
            {
                string dshmSerializedConfiguration = JsonSerializer.Serialize(dshmConfig, DshmConfigSerializerOptions);
                System.IO.Directory.CreateDirectory(DshmFolderFullPath);
                System.IO.File.WriteAllText($@"{DshmFolderFullPath}{DshmFileNameWithoutExtension}{DshmConfigFileFormat}", dshmSerializedConfiguration);
                return true;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Serialization or saving to disk failed.");
                return false;
            }
        }

        public static JsonSerializerOptions DshmConfigSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,

            Converters =
            {
                new JsonStringEnumConverter(),
                new DshmConfigCustomJsonConverter(),
            }
        };

        /// <summary>
        /// A custom converter necessary to serialiaze a DshmConfiguration object into the proper DsHidMini v3 format.
        /// Removes starting and ending brackets from the Devices list and makes each object in the list have it's object name be their MAC address.
        /// </summary>
        public class DshmConfigCustomJsonConverter : JsonConverter<DshmConfiguration>
        {
            public override DshmConfiguration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(
                Utf8JsonWriter writer, DshmConfiguration instance, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(instance.Global));
                var serializedGlobal = JsonSerializer.Serialize(instance.Global, options);
                writer.WriteRawValue(serializedGlobal);

                //JsonSerializer.Serialize(writer, new { instance.Global }, options);

                writer.WritePropertyName(nameof(instance.Devices));
                writer.WriteStartObject();
                foreach (DshmDeviceData device in instance.Devices)
                {
                    if (string.IsNullOrEmpty(device.DeviceAddress?.Trim()))
                        throw new JsonException("Expected non-null, non-empty Name");
                    writer.WritePropertyName(device.DeviceAddress);

                    var serializedCustomSettings = JsonSerializer.Serialize(device.DeviceSettings, options);
                    writer.WriteRawValue(serializedCustomSettings);
                }
                writer.WriteEndObject();

                writer.WriteEndObject();

            }
        }
    }
}
