using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

public class AppSnackbarMessagesService
{
    private readonly ISnackbarService _snackbarService;

    public AppSnackbarMessagesService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    public void ShowDsHidMiniConfigurationUpdateSuccessMessage()
    {
        _snackbarService.Show(
            "DsHidMini configuration updated",
            "",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(2)
        );
    }

    public void ShowDsHidMiniConfigurationUpdateFailedMessage()
    {
        _snackbarService.Show(
            "Failed to updated DsHidMini configuration",
            "",
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(3)
        );
    }

    public void ShowProfileDeletedMessage()
    {
        _snackbarService.Show(
            "Profile deleted"
            , "Devices using this profile reverted to global mode",
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowGlobalProfileUpdatedMessage()
    {
        _snackbarService.Show(
            "Global profile updated"
            , ""
            , ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.Checkmark24),
            TimeSpan.FromSeconds(2)
        );
    }

    public void ShowProfileUpdateMessage()
    {
        _snackbarService.Show(
            "Profile updated",
            "",
            ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowDefaultProfileEditingBlockedMessage()
    {
        _snackbarService.Show(
            "ControlApp's default profile can't be modified",
            "",
            ControlAppearance.Info,
            new SymbolIcon(SymbolRegular.Info24),
            TimeSpan.FromSeconds(2)
        );
    }


    public void ShowProfileChangedCanceledMessage()
    {
        _snackbarService.Show(
            "Canceled profile changes",
            "Remember to save next time",
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowBthPS3SettingsRectifiedMessage()
    {
        _snackbarService.Show(
            "BthPS3 settings updated",
            "PSM patching is enabled and RAW PDO is disabled.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(3)
        );
    }

    public void ShowControlAppUpToDateMessage()
    {
        _snackbarService.Show(
            "ControlApp is up to date",
            "No newer version is available on Buildbot.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(3)
        );
    }

    public void ShowControlAppUpdateCheckFailedMessage()
    {
        _snackbarService.Show(
            "Update check failed",
            "Could not reach Buildbot or read the latest version. Try again later.",
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.ErrorCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowBthPS3SettingsRectifyFailedMessage()
    {
        _snackbarService.Show(
            "Failed to update BthPS3 settings",
            "Run as Administrator and confirm BthPS3 is installed.",
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowDefenderBtSwitchedToPs3ModeMessage()
    {
        _snackbarService.Show(
            "Switched to PS3 mode",
            "The controller re-enumerated as a DualShock 3. Use the normal pairing controls once it shows up.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowDefenderBtSwitchToPs3ModeFailedMessage()
    {
        _snackbarService.Show(
            "Failed to switch Defender BT to PS3 mode",
            "The device may have been disconnected. Reconnect it in DualShock 4 mode and try again.",
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowDefenderBtSwitchNeedsReconnectMessage()
    {
        _snackbarService.Show(
            "PS3 mode switch did not take effect",
            "The controller stayed in DualShock 4 mode. Unplug and replug it, or restart ControlApp as Administrator so the USB port can be reset and the probe retried immediately.",
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24),
            TimeSpan.FromSeconds(8)
        );
    }

    public void ShowDefenderBtSwitchIgnoredByHardwareMessage()
    {
        _snackbarService.Show(
            "PS3 mode switch ignored by hardware",
            "The probe was delivered and the USB port was reset, but the controller stayed in DualShock 4 mode. This firmware may not implement the PS3 identity switch.",
            ControlAppearance.Caution,
            new SymbolIcon(SymbolRegular.Warning24),
            TimeSpan.FromSeconds(8)
        );
    }

    public void ShowUsbPowerOffSucceededMessage()
    {
        _snackbarService.Show(
            "Controller turned off",
            "USB stays connected. Use Restart on the device card to wake it.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowMotionViewerFailedMessage(string detail)
    {
        _snackbarService.Show(
            "Motion viewer unavailable",
            detail,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(8)
        );
    }

    public void ShowUsbPowerOffFailedMessage(string detail)
    {
        _snackbarService.Show(
            "Failed to turn off controller",
            detail,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(8)
        );
    }

    public void ShowPairingSucceededMessage()
    {
        _snackbarService.Show(
            "Controller paired",
            "The host address was written and verified.",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.CheckmarkCircle24),
            TimeSpan.FromSeconds(5)
        );
    }

    public void ShowPairingFailedMessage(string detail)
    {
        _snackbarService.Show(
            "Failed to pair controller",
            detail,
            ControlAppearance.Danger,
            new SymbolIcon(SymbolRegular.DismissCircle24),
            TimeSpan.FromSeconds(8)
        );
    }

    public void ShowPowerCyclingDeviceMessage(bool isWireless, bool isAppElevated, bool reconnectionResult)
    {
        if (!isWireless && !isAppElevated)
        {
            _snackbarService.Show(
                "Auto USB restart denied",
                "Restarting USB controller requires the ControlApp to be running as administrator",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(8)
            );
            return;
        }

        if (reconnectionResult)
        {
            _snackbarService.Show(
                "Restarting (USB) / Disconnecting (bluetooth) device",
                "",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Info24),
                TimeSpan.FromSeconds(4)
            );
        }
        else
        {
            _snackbarService.Show(
                "Failed to restart (USB) / Disconnect (bluetooth) device",
                "Manually reconnecting the device might be required to update its HID mode.",
                ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(8)
            );
        }
    }
}