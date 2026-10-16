namespace Nefarius.DsHidMini.ControlApp.Models.Util;

/// <summary>
///     Decides which ControlApp actions can run without an elevated token.
///     Shared configuration is intentionally absent: that decision follows the files' actual access.
/// </summary>
public static class ControlAppActionPolicy
{
    public static bool CanRestartDevice(bool isWireless, bool isElevated) =>
        isWireless || isElevated;

    public static bool CanSwitchDefenderBt(bool isDetected, bool isSwitching, bool isElevated) =>
        isDetected && !isSwitching && isElevated;

    public static bool CanAutoRepairBthPs3Settings(
        bool isElevated,
        bool filterAvailable,
        bool settingsIncorrect) =>
        isElevated && filterAvailable && settingsIncorrect;
}
