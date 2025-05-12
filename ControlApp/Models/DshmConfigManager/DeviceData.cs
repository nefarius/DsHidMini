using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

using Newtonsoft.Json;

namespace Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

public class DeviceData
{
    public DeviceData(string deviceMac)
    {
        DeviceMac = deviceMac;
    }

    public string DeviceMac { get; set; } = "0000000000";
    public string CustomName { get; set; } = "DualShock 3";
    public Guid GuidOfProfileToUse { get; set; } = ProfileData.DefaultGuid;
    public BluetoothPairingMode BluetoothPairingMode { get; set; } = BluetoothPairingMode.Auto;
    public string? PairingAddress { get; set; } = "";

    [JsonIgnore] // PairOnHotReload should only be enabled temporarely to prevent pairing requests from being repeteadly sent on hot-reload
    public bool PairOnHotReload { get; set; } = false;

    public SettingsModes SettingsMode { get; set; } = SettingsModes.Global;

    /// <summary>
    ///     Settings used when Device is in Custom Mode
    /// </summary>
    public DeviceSettings Settings { get; set; } = new();
}