using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;
using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using DshmConfigManagerType = Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfigManager;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <inheritdoc cref="IPreflightProbe" />
public sealed class BluetoothPreflightProbe : IPreflightProbe
{
    private readonly BthPS3StatusService _bthPs3Status;
    private readonly DshmConfigManagerType _configManager;
    private readonly DshmDevMan _devMan;

    public BluetoothPreflightProbe(
        BthPS3StatusService bthPs3Status,
        DshmDevMan devMan,
        DshmConfigManagerType configManager)
    {
        _bthPs3Status = bthPs3Status;
        _devMan = devMan;
        _configManager = configManager;
    }

    public IReadOnlyList<PreflightCheckResult> Run(PnPDevice? candidateDevice = null)
    {
        List<PreflightCheckResult> results = new();

        _bthPs3Status.Refresh();

        results.Add(new PreflightCheckResult(
            PreflightCheckId.BluetoothRadioOperable,
            _bthPs3Status.IsRadioOperable,
            "Bluetooth is on",
            _bthPs3Status.IsRadioOperable
                ? "A Bluetooth radio was detected and is usable."
                : "Turn on Bluetooth on this PC, then try again."));

        results.Add(new PreflightCheckResult(
            PreflightCheckId.BthPS3Installed,
            _bthPs3Status.IsInstalled,
            "Bluetooth controller support is installed",
            _bthPs3Status.IsInstalled
                ? $"BthPS3 {_bthPs3Status.InstalledVersionDisplay} is installed."
                : "Install BthPS3 to use this controller over Bluetooth."));

        results.Add(new PreflightCheckResult(
            PreflightCheckId.BthPS3VersionSupported,
            !_bthPs3Status.IsInstalled || _bthPs3Status.IsVersionSupported,
            "Bluetooth controller support is up to date",
            _bthPs3Status.IsVersionSupported
                ? $"Version {_bthPs3Status.InstalledVersionDisplay} meets the minimum of {_bthPs3Status.RequiredVersionDisplay}."
                : $"Update BthPS3 to {_bthPs3Status.RequiredVersionDisplay} or newer."));

        results.Add(new PreflightCheckResult(
            PreflightCheckId.BthPS3FilterAvailable,
            !_bthPs3Status.IsInstalled || _bthPs3Status.IsFilterAvailable,
            "Bluetooth filter is loaded",
            _bthPs3Status.IsFilterAvailable
                ? "The BthPS3PSM filter is loaded on the Bluetooth radio."
                : "The BthPS3PSM filter is not loaded. Bluetooth may be off, or it failed to load."));

        results.Add(new PreflightCheckResult(
            PreflightCheckId.BthPS3SettingsCorrect,
            !_bthPs3Status.IsFilterAvailable || !_bthPs3Status.AreSettingsIncorrect,
            "Bluetooth controller settings are correct",
            !_bthPs3Status.AreSettingsIncorrect
                ? "Required BthPS3 settings (RAW PDO off, PSM patching on) are already correct."
                : "Required settings need correcting. Run this setup as Administrator to fix them automatically.",
            CanAutoRepair: _bthPs3Status.IsFilterAvailable && _bthPs3Status.AreSettingsIncorrect));

        candidateDevice ??= FindEligibleUsbController();

        results.Add(new PreflightCheckResult(
            PreflightCheckId.UsbControllerPresent,
            candidateDevice is not null,
            "Controller is connected with USB",
            candidateDevice is not null
                ? "A DualShock 3 (or compatible) controller is connected with a USB cable."
                : "Connect the controller to this PC with a USB cable."));

        if (candidateDevice is null)
        {
            // Remaining device-scoped checks cannot run without a candidate device.
            return results;
        }

        Version? driverVersion = DsHidMiniDriverCompatibility.TryGetInstalledVersion(candidateDevice);
        bool driverSupportsIpc = !DsHidMiniDriverCompatibility.IsOlderThanIpcMinimum(driverVersion);
        results.Add(new PreflightCheckResult(
            PreflightCheckId.DriverVersionSupportsIpc,
            driverSupportsIpc,
            "Controller driver is up to date",
            driverSupportsIpc
                ? $"Driver version {DsHidMiniDriverCompatibility.FormatInstalledVersion(candidateDevice)} supports diagnostics."
                : DsHidMiniDriverCompatibility.DescribeMissingIpcSlot(candidateDevice)));

        results.Add(new PreflightCheckResult(
            PreflightCheckId.DriverIpcAvailable,
            DsHidMiniInterop.IsAvailable,
            "Driver communication is available",
            DsHidMiniInterop.IsAvailable
                ? "The driver's internal communication channel is available."
                : "Enable IPC in ControlApp Settings, or reconnect the controller."));

        bool addressSynthesized = candidateDevice.GetProperty<bool>(DsHidMiniDriver.DeviceAddressSynthesizedProperty);
        results.Add(new PreflightCheckResult(
            PreflightCheckId.ControllerReportsGenuineAddress,
            !addressSynthesized,
            "Controller reports its own Bluetooth address",
            addressSynthesized
                ? "This controller (or clone) never reported its own Bluetooth address, so pairing is unavailable."
                : "The controller reported a usable Bluetooth address."));

        string? deviceAddress = candidateDevice.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty);
        BluetoothPairingMode pairingMode = string.IsNullOrEmpty(deviceAddress)
            ? BluetoothPairingMode.Auto
            : _configManager.GetDeviceData(deviceAddress).BluetoothPairingMode;
        results.Add(new PreflightCheckResult(
            PreflightCheckId.PairingModeAllowsPairing,
            pairingMode != BluetoothPairingMode.Disabled,
            "Pairing is allowed for this controller",
            pairingMode != BluetoothPairingMode.Disabled
                ? "Bluetooth pairing is enabled for this controller in ControlApp."
                : "Bluetooth pairing mode is set to Disabled for this controller. Change it on the Devices page."));

        return results;
    }

    public PnPDevice? FindEligibleUsbController()
    {
        foreach (PnPDevice device in _devMan.Devices)
        {
            try
            {
                string enumerator = device.GetProperty<string>(DevicePropertyKey.Device_EnumeratorName) ?? "USB";
                bool isWireless = !enumerator.Equals("USB", StringComparison.InvariantCultureIgnoreCase);
                if (isWireless)
                {
                    continue;
                }

                string? address = device.GetProperty<string>(DsHidMiniDriver.DeviceAddressProperty);
                if (string.IsNullOrEmpty(address))
                {
                    continue;
                }

                bool synthesized = device.GetProperty<bool>(DsHidMiniDriver.DeviceAddressSynthesizedProperty);
                if (synthesized)
                {
                    continue;
                }

                DsDeviceType deviceType = DsDeviceType.Unknown;
                try
                {
                    deviceType = (DsDeviceType)device.GetProperty<byte>(DsHidMiniDriver.DeviceTypeProperty);
                }
                catch (Exception)
                {
                    // Older driver builds do not publish this property; treat as unknown/eligible.
                }

                if (deviceType != DsDeviceType.Unknown && !DsDeviceCapabilities.SupportsBluetooth(deviceType))
                {
                    continue;
                }

                return device;
            }
            catch (Exception ex)
            {
                Log.Logger.Debug(ex, "Skipping device '{InstanceId}' while looking for an eligible USB controller.",
                    device.InstanceId);
            }
        }

        return null;
    }

    public bool TryAutoRepair(PreflightCheckId id)
    {
        if (id != PreflightCheckId.BthPS3SettingsCorrect)
        {
            return false;
        }

        return _bthPs3Status.TryRectifySettings();
    }
}
