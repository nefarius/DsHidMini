using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig.Enums;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using Xunit;

using Button = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums.Button;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DriverConfigContractTests
{
    private static readonly string[] HidModes = ["SDF", "GPJ", "SXS", "DS4Windows", "XInput"];

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
