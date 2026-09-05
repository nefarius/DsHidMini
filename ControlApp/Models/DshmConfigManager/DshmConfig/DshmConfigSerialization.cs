using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;

internal static class DshmConfigSerialization
{
    public const string DriverFolderName = "DsHidMini";
    public const string DriverFileName = "DsHidMini.json";

    private static readonly string[] ModeBlockNames = ["SDF", "GPJ", "SXS", "DS4Windows", "XInput"];

    public static JsonSerializerOptions DshmConfigSerializerOptions { get; } = CreateSerializerOptions();

    public static string GetDriverConfigDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), DriverFolderName);

    public static string GetDriverConfigFilePath(string? directory = null) =>
        Path.Combine(directory ?? GetDriverConfigDirectory(), DriverFileName);

    public static string Serialize(DshmConfiguration config) =>
        JsonSerializer.Serialize(config, DshmConfigSerializerOptions);

    public static DshmConfiguration Deserialize(string json)
    {
        DshmConfiguration? parsed = JsonSerializer.Deserialize<DshmConfiguration>(json, DshmConfigSerializerOptions);
        if (parsed is null)
        {
            throw new JsonException("Driver configuration deserialized to null.");
        }

        return parsed;
    }

    public static bool TryReadDriverConfigFile(out DshmConfiguration? configuration, string? directory = null)
    {
        configuration = null;
        string path = GetDriverConfigFilePath(directory);
        if (!File.Exists(path))
        {
            return false;
        }

        configuration = Deserialize(File.ReadAllText(path));
        return true;
    }

    /// <summary>
    ///     Attempts to update the DsHidMini configuration file on disk by serializing a DshmConfiguration object into the
    ///     proper dshidmini v3 format.
    /// </summary>
    public static bool UpdateDsHidMiniConfigFile(DshmConfiguration dshmConfig, string? directory = null)
    {
        Log.Logger.Debug("Starting serialization of DsHidMini config object and saving to disk");
        try
        {
            string targetDirectory = directory ?? GetDriverConfigDirectory();
            Directory.CreateDirectory(targetDirectory);
            File.WriteAllText(GetDriverConfigFilePath(targetDirectory), Serialize(dshmConfig));
            return true;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Serialization or saving to disk failed.");
            return false;
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DshmConfigCustomJsonConverter());
        return options;
    }

    internal static DshmDeviceSettings ParseDeviceSettings(JsonElement element)
    {
        DshmDeviceSettings settings = new();

        settings.HidDeviceMode = ReadEnum<HidDeviceMode>(element, "HidDeviceMode")
                                 ?? ReadEnum<HidDeviceMode>(element, "HIDDeviceMode");
        settings.AutoRestartOnHidModeMismatch = ReadBool(element, "AutoRestartOnHidModeMismatch");
        settings.DevicePairingMode = ReadEnum<DevicePairingMode>(element, "DevicePairingMode");
        settings.PairOnHotReload = ReadBool(element, "PairOnHotReload");
        settings.CustomPairingAddress = ReadString(element, "CustomPairingAddress");
        settings.DisableWirelessIdleTimeout = ReadBool(element, "DisableWirelessIdleTimeout");
        settings.IsOutputRateControlEnabled = ReadBool(element, "IsOutputRateControlEnabled");
        settings.OutputRateControlPeriodMs = ReadByte(element, "OutputRateControlPeriodMs");
        settings.WirelessIdleTimeoutPeriodMs = ReadInt(element, "WirelessIdleTimeoutPeriodMs");

        if (TryGetProperty(element, "QuickDisconnectCombo", out JsonElement combo))
        {
            settings.QuickDisconnectCombo = ParseButtonCombo(combo);
        }

        string? activeModeName = settings.HidDeviceMode?.ToString();
        foreach (string modeName in ModeBlockNames)
        {
            if (!TryGetProperty(element, modeName, out JsonElement modeBlock))
            {
                continue;
            }

            if (string.Equals(modeName, activeModeName, StringComparison.Ordinal))
            {
                settings.ContextSettings = ParseHidModeSettings(modeBlock);
                settings.ContextSettings.HidDeviceMode = settings.HidDeviceMode;
            }
            else
            {
                settings.UnusedModeBlocks.Add(modeName);
            }
        }

        return settings;
    }

    private static DshmHidModeSettings ParseHidModeSettings(JsonElement element)
    {
        DshmHidModeSettings settings = new()
        {
            PressureExposureMode = ReadEnum<PressureMode>(element, "PressureExposureMode"),
            DPadExposureMode = ReadEnum<DPadExposureMode>(element, "DPadExposureMode")
        };

        if (TryGetProperty(element, "DeadZoneLeft", out JsonElement leftDz))
        {
            settings.DeadZoneLeft = ParseDeadZone(leftDz);
        }

        if (TryGetProperty(element, "DeadZoneRight", out JsonElement rightDz))
        {
            settings.DeadZoneRight = ParseDeadZone(rightDz);
        }

        if (TryGetProperty(element, "RumbleSettings", out JsonElement rumble))
        {
            settings.RumbleSettings = ParseRumble(rumble);
        }

        if (TryGetProperty(element, "LEDSettings", out JsonElement leds))
        {
            settings.LEDSettings = ParseLeds(leds);
        }

        if (TryGetProperty(element, "FlipAxis", out JsonElement flip))
        {
            settings.FlipAxis = new DshmDeviceSettings.AxesFlipping
            {
                LeftX = ReadBool(flip, "LeftX"),
                LeftY = ReadBool(flip, "LeftY"),
                RightX = ReadBool(flip, "RightX"),
                RightY = ReadBool(flip, "RightY")
            };
        }

        return settings;
    }

    private static DshmDeviceSettings.DeadZoneSettings ParseDeadZone(JsonElement element) =>
        new()
        {
            Apply = ReadBool(element, "Apply"),
            PolarValue = ReadDouble(element, "PolarValue")
        };

    private static DshmDeviceSettings.AllRumbleSettings ParseRumble(JsonElement element)
    {
        DshmDeviceSettings.AllRumbleSettings rumble = new()
        {
            DisableLeft = ReadBool(element, "DisableLeft"),
            DisableRight = ReadBool(element, "DisableRight")
        };

        if (TryGetProperty(element, "HeavyRescale", out JsonElement heavy))
        {
            rumble.HeavyRescale = new DshmDeviceSettings.HeavyRescaleSettings
            {
                IsEnabled = ReadBool(heavy, "IsEnabled"),
                RescaleMinRange = ReadByte(heavy, "RescaleMinRange"),
                RescaleMaxRange = ReadByte(heavy, "RescaleMaxRange")
            };
        }

        if (TryGetProperty(element, "AlternativeMode", out JsonElement alt))
        {
            rumble.AlternativeMode = new DshmDeviceSettings.AlternativeModeSettings
            {
                IsEnabled = ReadBool(alt, "IsEnabled"),
                RescaleMinRange = ReadByte(alt, "RescaleMinRange"),
                RescaleMaxRange = ReadByte(alt, "RescaleMaxRange")
            };

            if (TryGetProperty(alt, "ForcedRight", out JsonElement forced))
            {
                rumble.AlternativeMode.ForcedRight = new DshmDeviceSettings.ForcedRightAdjusts
                {
                    IsHeavyThresholdEnabled = ReadBool(forced, "IsHeavyThresholdEnabled"),
                    HeavyThreshold = ReadByte(forced, "HeavyThreshold"),
                    IsLightThresholdEnabled = ReadBool(forced, "IsLightThresholdEnabled"),
                    LightThreshold = ReadByte(forced, "LightThreshold")
                };
            }

            if (TryGetProperty(alt, "ToggleCombo", out JsonElement toggle))
            {
                rumble.AlternativeMode.ToggleCombo = ParseButtonCombo(toggle);
            }
        }

        return rumble;
    }

    private static DshmDeviceSettings.AllLEDSettings ParseLeds(JsonElement element)
    {
        DshmDeviceSettings.AllLEDSettings leds = new()
        {
            Mode = ReadEnum<LEDsMode>(element, "Mode"),
            Authority = ReadEnum<DSHM_LEDsAuthority>(element, "Authority")
        };

        if (TryGetProperty(element, "CustomPatterns", out JsonElement patterns))
        {
            leds.CustomPatterns = new DshmDeviceSettings.LEDsCustoms
            {
                LEDFlags = ReadByte(patterns, "LEDFlags"),
                Player1 = ParseLedPlayer(patterns, "Player1"),
                Player2 = ParseLedPlayer(patterns, "Player2"),
                Player3 = ParseLedPlayer(patterns, "Player3"),
                Player4 = ParseLedPlayer(patterns, "Player4")
            };
        }

        return leds;
    }

    private static DshmDeviceSettings.SingleLEDCustoms ParseLedPlayer(JsonElement parent, string name)
    {
        if (!TryGetProperty(parent, name, out JsonElement player))
        {
            return new DshmDeviceSettings.SingleLEDCustoms();
        }

        return new DshmDeviceSettings.SingleLEDCustoms
        {
            TotalDuration = ReadByte(player, "TotalDuration"),
            BasePortionDuration = ReadUShort(player, "BasePortionDuration"),
            OffPortionMultiplier = ReadByte(player, "OffPortionMultiplier"),
            OnPortionMultiplier = ReadByte(player, "OnPortionMultiplier")
        };
    }

    private static DshmDeviceSettings.ButtonCombo ParseButtonCombo(JsonElement element) =>
        new()
        {
            IsEnabled = ReadBool(element, "IsEnabled"),
            HoldTime = ReadInt(element, "HoldTime"),
            Button1 = ReadInt(element, "Button1"),
            Button2 = ReadInt(element, "Button2"),
            Button3 = ReadInt(element, "Button3")
        };

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static TEnum? ReadEnum<TEnum>(JsonElement element, string name) where TEnum : struct, Enum
    {
        if (!TryGetProperty(element, name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return Enum.TryParse(value.GetString(), ignoreCase: true, out TEnum parsed) ? parsed : null;
    }

    private static bool? ReadBool(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out JsonElement value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.TryGetInt32(out int parsed) ? parsed : (int)value.GetDouble();
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out JsonElement value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.GetDouble();
    }

    private static byte? ReadByte(JsonElement element, string name)
    {
        int? value = ReadInt(element, name);
        if (value is null)
        {
            return null;
        }

        return (byte)Math.Clamp(value.Value, 0, 255);
    }

    private static ushort? ReadUShort(JsonElement element, string name)
    {
        int? value = ReadInt(element, name);
        if (value is null)
        {
            return null;
        }

        return (ushort)Math.Clamp(value.Value, 0, ushort.MaxValue);
    }

    /// <summary>
    ///     Serializes a DshmConfiguration object into the DsHidMini v3 format: <c>Devices</c> is an object keyed by MAC.
    /// </summary>
    public class DshmConfigCustomJsonConverter : JsonConverter<DshmConfiguration>
    {
        public override DshmConfiguration Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            DshmConfiguration configuration = new();

            if (TryGetProperty(root, "Global", out JsonElement global))
            {
                configuration.Global = ParseDeviceSettings(global);
            }

            if (TryGetProperty(root, "Devices", out JsonElement devices) &&
                devices.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty deviceProperty in devices.EnumerateObject())
                {
                    configuration.Devices.Add(new DshmDeviceData
                    {
                        DeviceAddress = deviceProperty.Name,
                        DeviceSettings = ParseDeviceSettings(deviceProperty.Value)
                    });
                }
            }

            return configuration;
        }

        public override void Write(Utf8JsonWriter writer, DshmConfiguration instance, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(instance.Global));
            JsonSerializer.Serialize(writer, instance.Global, options);

            writer.WritePropertyName(nameof(instance.Devices));
            writer.WriteStartObject();
            foreach (DshmDeviceData device in instance.Devices)
            {
                if (string.IsNullOrWhiteSpace(device.DeviceAddress))
                {
                    throw new JsonException("Expected non-null, non-empty device address.");
                }

                writer.WritePropertyName(device.DeviceAddress);
                JsonSerializer.Serialize(writer, device.DeviceSettings, options);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }
}
