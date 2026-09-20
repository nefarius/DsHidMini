using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Util;

using Xunit;

using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DriverConfigContractTests
{
    private static readonly string[] HidModes = ["SDF", "GPJ", "SXS", "DS4Windows", "XInput", "CGP"];

    [Fact]
    public void Serialize_UsesNativePropertyNames_AndOmitsUnsupportedKeys()
    {
        string json = SerializeDefaultProfile(SettingsContext.XInput);
        JsonNode root = JsonNode.Parse(json)!;

        Assert.True(root["IPCEnabled"]!.GetValue<bool>());
        Assert.NotNull(root["Global"]);
        Assert.NotNull(root["Devices"]);
        Assert.IsType<JsonObject>(root["Devices"]);
        Assert.NotNull(root["Global"]!["HidDeviceMode"]);
        Assert.Null(root["Global"]!["HIDDeviceMode"]);
        Assert.Null(root["Global"]!["DisableAutoPairing"]);
        Assert.Null(root["Global"]!["IsQuickDisconnectComboEnabled"]);
        Assert.Null(root["Global"]!["IsOutputDeduplicatorEnabled"]);
        Assert.Null(root["Global"]!["PairOnHotReload"]);
        Assert.Equal("XInput", root["Global"]!["HidDeviceMode"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(SettingsContext.SDF, "SDF")]
    [InlineData(SettingsContext.GPJ, "GPJ")]
    [InlineData(SettingsContext.SXS, "SXS")]
    [InlineData(SettingsContext.DS4W, "DS4Windows")]
    [InlineData(SettingsContext.XInput, "XInput")]
    [InlineData(SettingsContext.CGP, "CGP")]
    public void Serialize_EmitsExactlyOneActiveModeBlock(SettingsContext context, string expectedMode)
    {
        JsonNode global = JsonNode.Parse(SerializeDefaultProfile(context))!["Global"]!;
        Assert.Equal(expectedMode, global["HidDeviceMode"]!.GetValue<string>());

        List<string> present = HidModes.Where(mode => global[mode] is not null).ToList();
        Assert.Equal(new[] { expectedMode }, present);
    }

    [Fact]
    public void Serialize_DevicesObjectIsKeyedByMac()
    {
        DeviceSettings settings = new();
        settings.HidMode.SettingsContext = SettingsContext.GPJ;
        DshmConfiguration config = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(settings, config.Global);
        config.Devices.Add(new DshmDeviceData
        {
            DeviceAddress = "AABBCCDDEEFF",
            DeviceSettings =
            {
                DevicePairingMode = DevicePairingMode.Custom,
                CustomPairingAddress = "112233445566"
            }
        });

        JsonNode root = JsonNode.Parse(DshmConfigSerialization.Serialize(config))!;
        Assert.NotNull(root["Devices"]!["AABBCCDDEEFF"]);
        Assert.Equal("Custom", root["Devices"]!["AABBCCDDEEFF"]!["DevicePairingMode"]!.GetValue<string>());
        Assert.Equal("112233445566", root["Devices"]!["AABBCCDDEEFF"]!["CustomPairingAddress"]!.GetValue<string>());
        Assert.Null(root["Devices"]!["AABBCCDDEEFF"]!["HidDeviceMode"]);
    }

    [Fact]
    public void Serialize_ButtonMapping_MatchesDriverOffsets()
    {
        DeviceSettings settings = new();
        settings.Wireless.QuickDisconnectCombo.ButtonCombo[0] = Button.L1;
        settings.Wireless.QuickDisconnectCombo.ButtonCombo[1] = Button.R1;
        settings.Wireless.QuickDisconnectCombo.ButtonCombo[2] = Button.PS;

        JsonNode combo = JsonNode.Parse(Serialize(settings))!["Global"]!["QuickDisconnectCombo"]!;
        Assert.Equal(10, combo["Button1"]!.GetValue<int>());
        Assert.Equal(11, combo["Button2"]!.GetValue<int>());
        Assert.Equal(16, combo["Button3"]!.GetValue<int>());
        Assert.Equal(1000, combo["HoldTime"]!.GetValue<int>());
    }

    [Fact]
    public void Serialize_PolarValue_IsFloatingPoint()
    {
        DeviceSettings settings = new();
        settings.Sticks.LeftStickData.DeadZone = DshmDeadZoneConversion.DefaultUiDeadZone;
        JsonNode left = JsonNode.Parse(Serialize(settings))!["Global"]!["XInput"]!["DeadZoneLeft"]!;
        Assert.True(left["PolarValue"] is JsonValue);
        Assert.Equal(DshmDeadZoneConversion.ToPolarValue(settings.Sticks.LeftStickData.DeadZone),
            left["PolarValue"]!.GetValue<double>(), 3);
    }

    [Fact]
    public void Deserialize_NativeSample_ReadsHidDeviceModeAndActiveBlock()
    {
        string sample = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DsHidMini.json"));
        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(sample);

        Assert.True(parsed.IPCEnabled);
        Assert.Equal(HidDeviceMode.DS4Windows, parsed.Global.HidDeviceMode);
        Assert.True(parsed.Global.AutoRestartOnHidModeMismatch);
        Assert.Equal(DevicePairingMode.Auto, parsed.Global.DevicePairingMode);
        Assert.NotNull(parsed.Global.ContextSettings.RumbleSettings.HeavyRescale.IsEnabled);
        Assert.Contains("SDF", parsed.Global.UnusedModeBlocks);
        Assert.Contains("XInput", parsed.Global.UnusedModeBlocks);
        Assert.DoesNotContain("DS4Windows", parsed.Global.UnusedModeBlocks);
        Assert.NotEmpty(parsed.Devices);
    }

    [Fact]
    public void RoundTrip_DefaultSettings_PreserveHidModeAndRumbleRanges()
    {
        DeviceSettings original = new();
        original.HidMode.SettingsContext = SettingsContext.SXS;
        original.AltRumbleAdjusts.RightRumbleConversionUpperRange = 90;
        original.GeneralRumble.IsAltRumbleModeEnabled = true;
        original.GeneralRumble.AlwaysStartInNormalMode = true;
        original.GeneralRumble.IsAltModeToggleButtonComboEnabled = true;
        original.GeneralRumble.AltModeToggleButtonCombo.IsEnabled = true;

        DshmDeviceSettings driver = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(original, driver);
        string json = DshmConfigSerialization.Serialize(new DshmConfiguration { Global = driver });
        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);

        DeviceSettings restored = new();
        DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(parsed.Global, restored);

        Assert.Equal(SettingsContext.SXS, restored.HidMode.SettingsContext);
        Assert.True(restored.GeneralRumble.IsAltRumbleModeEnabled);
        Assert.True(restored.GeneralRumble.AlwaysStartInNormalMode);
        Assert.Equal(90, restored.AltRumbleAdjusts.RightRumbleConversionUpperRange);
        Assert.Equal(BluetoothOutputReportTransport.Control, restored.OutputReport.BluetoothOutputReportTransport);
    }

    [Fact]
    public void RoundTrip_CgpMode_PreservesHidModeAndOmitsPressureAndDPadSettings()
    {
        DeviceSettings original = new();
        original.HidMode.SettingsContext = SettingsContext.CGP;

        DshmDeviceSettings driver = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(original, driver);
        Assert.Equal(HidDeviceMode.CGP, driver.HidDeviceMode);
        Assert.Null(driver.ContextSettings.PressureExposureMode);
        Assert.Null(driver.ContextSettings.DPadExposureMode);

        string json = DshmConfigSerialization.Serialize(new DshmConfiguration { Global = driver });
        JsonNode global = JsonNode.Parse(json)!["Global"]!;
        Assert.Equal("CGP", global["HidDeviceMode"]!.GetValue<string>());
        Assert.Null(global["CGP"]!["PressureExposureMode"]);
        Assert.Null(global["CGP"]!["DPadExposureMode"]);

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        DeviceSettings restored = new();
        DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(parsed.Global, restored);
        Assert.Equal(SettingsContext.CGP, restored.HidMode.SettingsContext);
    }

    [Fact]
    public void ToHidDeviceModePropertyValue_Cgp_MapsToByteSix()
    {
        Assert.Equal(0x06, DshmDriverTranslationUtils.ToHidDeviceModePropertyValue(SettingsContext.CGP));
    }

    [Fact]
    public void Serialize_DefaultSettings_EmitsControlBluetoothOutputTransport()
    {
        JsonNode global = JsonNode.Parse(SerializeDefaultProfile(SettingsContext.XInput))!["Global"]!;
        Assert.Equal("Control", global["BluetoothOutputReportTransport"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(BluetoothOutputReportTransport.Control, "Control")]
    [InlineData(BluetoothOutputReportTransport.Interrupt, "Interrupt")]
    public void RoundTrip_BluetoothOutputReportTransport_PreservesValue(
        BluetoothOutputReportTransport transport,
        string expectedName)
    {
        DeviceSettings original = new();
        original.OutputReport.BluetoothOutputReportTransport = transport;

        DshmDeviceSettings driver = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(original, driver);
        string json = DshmConfigSerialization.Serialize(new DshmConfiguration { Global = driver });
        JsonNode global = JsonNode.Parse(json)!["Global"]!;
        Assert.Equal(expectedName, global["BluetoothOutputReportTransport"]!.GetValue<string>());

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        DeviceSettings restored = new();
        DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(parsed.Global, restored);
        Assert.Equal(transport, restored.OutputReport.BluetoothOutputReportTransport);
    }

    [Fact]
    public void Deserialize_MissingIpcEnabled_DefaultsTrue()
    {
        const string json = """
            {
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.True(parsed.IPCEnabled);
    }

    [Fact]
    public void Deserialize_MixedCaseIpcEnabledFalse_IsPreserved()
    {
        const string json = """
            {
              "ipcEnabled": false,
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.False(parsed.IPCEnabled);
    }

    [Fact]
    public void RoundTrip_IpcEnabledFalse_IsPreserved()
    {
        DshmConfiguration config = new() { IPCEnabled = false };
        string json = DshmConfigSerialization.Serialize(config);
        JsonNode root = JsonNode.Parse(json)!;
        Assert.False(root["IPCEnabled"]!.GetValue<bool>());

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.False(parsed.IPCEnabled);
    }

    [Fact]
    public void Deserialize_LegacyPairOnHotReload_IsIgnored()
    {
        const string json = """
            {
              "Global": {
                "HidDeviceMode": "XInput",
                "DevicePairingMode": "Auto",
                "PairOnHotReload": true
              },
              "Devices": {}
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.Equal(DevicePairingMode.Auto, parsed.Global.DevicePairingMode);
        Assert.Null(parsed.Global.GetType().GetProperty("PairOnHotReload"));
    }

    [Fact]
    public void Deserialize_LegacyConfigWithoutBluetoothTransport_KeepsControlDefault()
    {
        const string json = """
            {
              "Global": {
                "HidDeviceMode": "XInput",
                "IsOutputRateControlEnabled": true,
                "OutputRateControlPeriodMs": 150
              },
              "Devices": {}
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.Null(parsed.Global.BluetoothOutputReportTransport);

        DeviceSettings restored = new();
        DshmManagerToDriverConversion.ConvertDriverFormatToDeviceSettings(parsed.Global, restored);
        Assert.Equal(BluetoothOutputReportTransport.Control, restored.OutputReport.BluetoothOutputReportTransport);
    }

    [Fact]
    public void Overlay_BluetoothOutputReportTransport_OverridesBaseline()
    {
        DshmDeviceSettings baseline = new()
        {
            BluetoothOutputReportTransport = BluetoothOutputReportTransport.Control
        };
        DshmDeviceSettings overlay = new()
        {
            BluetoothOutputReportTransport = BluetoothOutputReportTransport.Interrupt
        };

        DshmDeviceSettings merged = DshmManagerToDriverConversion.OverlayDeviceSettings(baseline, overlay);
        Assert.Equal(BluetoothOutputReportTransport.Interrupt, merged.BluetoothOutputReportTransport);
    }

    [Fact]
    public void Deserialize_DeviceWithoutHidDeviceMode_UsesGlobalModeBlock()
    {
        const string json = """
            {
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {
                "AABBCCDDEEFF": {
                  "XInput": { "DeadZoneLeft": { "Apply": true, "PolarValue": 12.0 } },
                  "SDF": { "DeadZoneLeft": { "Apply": false } }
                }
              }
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        DshmDeviceSettings device = parsed.Devices.Single().DeviceSettings;
        Assert.Null(device.HidDeviceMode);
        Assert.Equal(12.0, device.ContextSettings.DeadZoneLeft.PolarValue);
        Assert.True(device.ContextSettings.DeadZoneLeft.Apply);
        Assert.Contains("SDF", device.UnusedModeBlocks);
        Assert.DoesNotContain("XInput", device.UnusedModeBlocks);
    }

    [Fact]
    public void Deserialize_UnknownProperties_AreIgnored()
    {
        const string json = """
            {
              "Global": {
                "HidDeviceMode": "XInput",
                "FutureDriverFlag": true,
                "XInput": { "UnknownNested": 1, "DeadZoneLeft": { "Apply": true, "PolarValue": 3.0 } }
              },
              "Devices": {}
            }
            """;

        DshmConfiguration parsed = DshmConfigSerialization.Deserialize(json);
        Assert.Equal(HidDeviceMode.XInput, parsed.Global.HidDeviceMode);
        Assert.True(parsed.Global.ContextSettings.DeadZoneLeft.Apply);
        Assert.Equal(3.0, parsed.Global.ContextSettings.DeadZoneLeft.PolarValue);
    }

    [Fact]
    public void Merge_PreservesUnrelatedGlobalNestedDeviceAndUnknownProperties()
    {
        const string baseline = """
            {
              "IPCEnabled": true,
              "Global": {
                "HidDeviceMode": "XInput",
                "AutoRestartOnHidModeMismatch": true,
                "BluetoothOutputReportTransport": "Control",
                "XInput": { "DeadZoneLeft": { "Apply": true, "PolarValue": 3.0 } }
              },
              "Devices": {}
            }
            """;
        const string desired = """
            {
              "IPCEnabled": true,
              "Global": {
                "HidDeviceMode": "XInput",
                "AutoRestartOnHidModeMismatch": false,
                "BluetoothOutputReportTransport": "Control",
                "XInput": { "DeadZoneLeft": { "Apply": true, "PolarValue": 3.0 } }
              },
              "Devices": {}
            }
            """;
        const string current = """
            {
              "IPCEnabled": true,
              "Global": {
                "HidDeviceMode": "DS4Windows",
                "AutoRestartOnHidModeMismatch": true,
                "BluetoothOutputReportTransport": "Interrupt",
                "FutureDriverFlag": true,
                "XInput": { "DeadZoneLeft": { "Apply": false, "PolarValue": 9.0 }, "UnknownNested": 1 },
                "DS4Windows": { "CustomBlock": true }
              },
              "Devices": {
                "AABBCCDDEEFF": { "FutureDeviceFlag": 2 }
              }
            }
            """;

        JsonNode merged = JsonNode.Parse(
            DshmConfigSerialization.MergeDriverConfigJson(baseline, desired, current))!;

        Assert.False(merged["Global"]!["AutoRestartOnHidModeMismatch"]!.GetValue<bool>());
        Assert.Equal("DS4Windows", merged["Global"]!["HidDeviceMode"]!.GetValue<string>());
        Assert.Equal("Interrupt", merged["Global"]!["BluetoothOutputReportTransport"]!.GetValue<string>());
        Assert.True(merged["Global"]!["FutureDriverFlag"]!.GetValue<bool>());
        Assert.False(merged["Global"]!["XInput"]!["DeadZoneLeft"]!["Apply"]!.GetValue<bool>());
        Assert.Equal(9.0, merged["Global"]!["XInput"]!["DeadZoneLeft"]!["PolarValue"]!.GetValue<double>());
        Assert.Equal(1, merged["Global"]!["XInput"]!["UnknownNested"]!.GetValue<int>());
        Assert.True(merged["Global"]!["DS4Windows"]!["CustomBlock"]!.GetValue<bool>());
        Assert.Equal(2, merged["Devices"]!["AABBCCDDEEFF"]!["FutureDeviceFlag"]!.GetValue<int>());
    }

    [Fact]
    public void Merge_ConflictOnSamePath_ControlAppWins()
    {
        const string baseline = """
            {
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;
        const string desired = """
            {
              "Global": { "HidDeviceMode": "GPJ" },
              "Devices": {}
            }
            """;
        const string current = """
            {
              "Global": { "HidDeviceMode": "SDF", "FutureDriverFlag": true },
              "Devices": {}
            }
            """;

        JsonNode merged = JsonNode.Parse(
            DshmConfigSerialization.MergeDriverConfigJson(baseline, desired, current))!;

        Assert.Equal("GPJ", merged["Global"]!["HidDeviceMode"]!.GetValue<string>());
        Assert.True(merged["Global"]!["FutureDriverFlag"]!.GetValue<bool>());
    }

    [Fact]
    public void Merge_AddsAndRemovesPropertiesChangedByApp()
    {
        const string baseline = """
            {
              "Global": { "HidDeviceMode": "XInput", "ObsoleteFlag": true },
              "Devices": { "AABBCCDDEEFF": { "DevicePairingMode": "Auto" } }
            }
            """;
        const string desired = """
            {
              "Global": { "HidDeviceMode": "XInput", "NewFlag": 4 },
              "Devices": {}
            }
            """;
        const string current = """
            {
              "Global": { "HidDeviceMode": "XInput", "ObsoleteFlag": true, "KeepMe": "yes" },
              "Devices": { "AABBCCDDEEFF": { "DevicePairingMode": "Custom" }, "112233445566": {} }
            }
            """;

        JsonNode merged = JsonNode.Parse(
            DshmConfigSerialization.MergeDriverConfigJson(baseline, desired, current))!;

        Assert.Null(merged["Global"]!["ObsoleteFlag"]);
        Assert.Equal(4, merged["Global"]!["NewFlag"]!.GetValue<int>());
        Assert.Equal("yes", merged["Global"]!["KeepMe"]!.GetValue<string>());
        Assert.Null(merged["Devices"]!["AABBCCDDEEFF"]);
        Assert.NotNull(merged["Devices"]!["112233445566"]);
    }

    [Fact]
    public void Merge_NoAppChanges_KeepsCurrentDocument()
    {
        const string generated = """
            {
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;
        const string current = """
            {
              "Global": { "HidDeviceMode": "SXS", "FutureDriverFlag": true },
              "Devices": { "AABBCCDDEEFF": {} }
            }
            """;

        JsonNode merged = JsonNode.Parse(
            DshmConfigSerialization.MergeDriverConfigJson(generated, generated, current))!;

        Assert.Equal("SXS", merged["Global"]!["HidDeviceMode"]!.GetValue<string>());
        Assert.True(merged["Global"]!["FutureDriverFlag"]!.GetValue<bool>());
        Assert.NotNull(merged["Devices"]!["AABBCCDDEEFF"]);
    }

    [Fact]
    public void Merge_ArrayReplacement_IsAtomic()
    {
        const string baseline = """
            { "Global": { "Tags": [ "a", "b" ] }, "Devices": {} }
            """;
        const string desired = """
            { "Global": { "Tags": [ "c" ] }, "Devices": {} }
            """;
        const string current = """
            { "Global": { "Tags": [ "a", "manual" ] }, "Devices": {} }
            """;

        JsonNode merged = JsonNode.Parse(
            DshmConfigSerialization.MergeDriverConfigJson(baseline, desired, current))!;

        JsonArray tags = merged["Global"]!["Tags"]!.AsArray();
        Assert.Single(tags);
        Assert.Equal("c", tags[0]!.GetValue<string>());
    }

    [Fact]
    public void Merge_IpcEnabledCaseAlias_WritesCanonicalAndReloadReadsUpdatedValue()
    {
        const string baseline = """
            {
              "IPCEnabled": true,
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;
        const string desired = """
            {
              "IPCEnabled": false,
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;
        const string current = """
            {
              "ipcEnabled": true,
              "Global": { "HidDeviceMode": "XInput" },
              "Devices": {}
            }
            """;

        string mergedJson = DshmConfigSerialization.MergeDriverConfigJson(baseline, desired, current);
        JsonNode merged = JsonNode.Parse(mergedJson)!;

        Assert.False(merged["IPCEnabled"]!.GetValue<bool>());
        Assert.Null(merged["ipcEnabled"]);

        DshmConfiguration reloaded = DshmConfigSerialization.Deserialize(mergedJson);
        Assert.False(reloaded.IPCEnabled);
    }

    private static string SerializeDefaultProfile(SettingsContext context)
    {
        DeviceSettings settings = new();
        settings.HidMode.SettingsContext = context;
        return Serialize(settings);
    }

    private static string Serialize(DeviceSettings settings)
    {
        DshmConfiguration config = new();
        DshmManagerToDriverConversion.ConvertDeviceSettingsToDriverFormat(settings, config.Global);
        return DshmConfigSerialization.Serialize(config);
    }
}
