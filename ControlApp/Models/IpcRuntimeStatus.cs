using System.IO;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     How <c>DsHidMini.json</c> tells the driver whether IPC should be active.
/// </summary>
public enum DriverIpcConfigReading
{
    /// <summary>The file is absent. The driver treats that as IPC enabled.</summary>
    MissingDefaultsToEnabled,

    /// <summary>The file was read and <c>IPCEnabled</c> is true, including when the property is omitted.</summary>
    Enabled,

    /// <summary>The file was read and <c>IPCEnabled</c> is false.</summary>
    Disabled,

    /// <summary>The file exists but could not be read. A running driver keeps its previous IPC state.</summary>
    Unreadable
}

    /// <summary>
    ///     Decides when the saved IPC setting must be checked against a live driver ping.
    ///     The driver is demand-loaded, so IPC objects exist only while at least one device is connected.
    /// </summary>
    public static class IpcRuntimeStatus
    {
        public static bool ExpectsLiveIpc(
            bool shownEnabled,
            DriverIpcConfigReading driverConfig,
            bool hasConnectedDevice) =>
            hasConnectedDevice && (shownEnabled || driverConfig == DriverIpcConfigReading.Enabled);

    public static bool IsDisplayedStateStale(bool expectsLiveIpc, bool driverResponded) =>
        expectsLiveIpc && !driverResponded;

    public static string DescribeStaleState(bool shownEnabled) =>
        shownEnabled
            ? "Driver IPC is checked here, but the running driver did not answer a ping. The checkbox is the saved setting, not the live driver. Restart Windows if a driver update has not finished loading."
            : "The driver configuration enables IPC, but the running driver did not answer a ping. Restart Windows if a driver update has not finished loading.";

    public static DriverIpcConfigReading ReadDriverConfig(string driverConfigDirectory)
    {
        string path = DshmConfigSerialization.GetDriverConfigFilePath(driverConfigDirectory);
        if (!File.Exists(path))
        {
            return DriverIpcConfigReading.MissingDefaultsToEnabled;
        }

        if (!DshmConfigSerialization.TryReadDriverConfigFile(
                out DshmConfiguration? configuration,
                driverConfigDirectory)
            || configuration is null)
        {
            return DriverIpcConfigReading.Unreadable;
        }

        return configuration.IPCEnabled
            ? DriverIpcConfigReading.Enabled
            : DriverIpcConfigReading.Disabled;
    }
}
