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

    public void ShowPowerCyclingDeviceMessage(bool isWireless, bool isAppElevated, bool reconnectionResult)
    {
        string temp = "";
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