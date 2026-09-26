namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Identifies one deterministic, hardware-independent condition checked before a Bluetooth
///     diagnostic run or the mandatory first-run setup proceeds to the physical pair/unplug steps.
/// </summary>
public enum PreflightCheckId
{
    BluetoothRadioOperable,
    BthPS3Installed,
    BthPS3VersionSupported,
    BthPS3FilterAvailable,
    BthPS3SettingsCorrect,
    DriverIpcAvailable,
    DriverVersionSupportsIpc,
    UsbControllerPresent,
    ControllerReportsGenuineAddress,
    PairingModeAllowsPairing
}
