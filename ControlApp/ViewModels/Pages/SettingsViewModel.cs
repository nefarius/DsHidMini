using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly DshmConfigManager _dshmConfigManager;
    private readonly ControlAppUpdateService _updateService;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    private bool _isInitialized;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isCheckingForUpdates;

    public SettingsViewModel(
        DshmConfigManager dshmConfigManager,
        BthPS3StatusService bthPs3,
        AppSnackbarMessagesService appSnackbarMessagesService,
        ControlAppUpdateService updateService)
    {
        _dshmConfigManager = dshmConfigManager;
        BthPs3 = bthPs3;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _updateService = updateService;
    }

    public BthPS3StatusService BthPs3 { get; }

    /// <summary>
    ///     When enabled (default), the driver requests a self restart on a HID mode mismatch instead of requiring a
    ///     manual reconnect / second replug (see issue #374).
    /// </summary>
    public bool AutoRestartOnHidModeMismatch
    {
        get => _dshmConfigManager.AutoRestartOnHidModeMismatch;
        set
        {
            if (_dshmConfigManager.AutoRestartOnHidModeMismatch == value)
            {
                return;
            }

            _dshmConfigManager.AutoRestartOnHidModeMismatch = value;
            if (!_dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile())
            {
                Log.Logger.Error("Failed to persist AutoRestartOnHidModeMismatch.");
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     When enabled, Minimize and Close hide ControlApp to the notification area instead of exiting
    ///     (see issue #80).
    /// </summary>
    public bool MinimizeToTray
    {
        get => ApplicationConfiguration.Instance.MinimizeToTray;
        set
        {
            if (ApplicationConfiguration.Instance.MinimizeToTray == value)
            {
                return;
            }

            ApplicationConfiguration.Instance.MinimizeToTray = value;
            try
            {
                ApplicationConfiguration.Instance.Save();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to persist MinimizeToTray.");
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     When enabled, a Retro Fighters Defender Bluetooth Edition detected in DualShock 4 mode is switched
    ///     into PS3 (DualShock 3) mode automatically instead of requiring the "Switch to PS3 mode" button on the
    ///     Devices page (see issue #282).
    /// </summary>
    public bool AutoSwitchDefenderBtToPs3Mode
    {
        get => ApplicationConfiguration.Instance.AutoSwitchDefenderBtToPs3Mode;
        set
        {
            if (ApplicationConfiguration.Instance.AutoSwitchDefenderBtToPs3Mode == value)
            {
                return;
            }

            ApplicationConfiguration.Instance.AutoSwitchDefenderBtToPs3Mode = value;
            try
            {
                ApplicationConfiguration.Instance.Save();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to persist AutoSwitchDefenderBtToPs3Mode.");
            }

            OnPropertyChanged();
        }
    }

    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
        {
            InitializeViewModel();
        }

        BthPs3.Refresh();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    private void InitializeViewModel()
    {
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"DsHidMini ControlApp {MainWindowViewModel.GetDisplayVersion()}";

        _isInitialized = true;
    }

    [RelayCommand]
    private void OnChangeTheme(string parameter)
    {
        switch (parameter)
        {
            case "theme_light":
                if (CurrentTheme == ApplicationTheme.Light)
                {
                    break;
                }

                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                CurrentTheme = ApplicationTheme.Light;

                break;

            default:
                if (CurrentTheme == ApplicationTheme.Dark)
                {
                    break;
                }

                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                CurrentTheme = ApplicationTheme.Dark;

                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdates()
    {
        IsCheckingForUpdates = true;
        try
        {
            UpdateCheckOutcome outcome = await _updateService.CheckNowAsync();
            switch (outcome)
            {
                case UpdateCheckOutcome.UpToDate:
                    _appSnackbarMessagesService.ShowControlAppUpToDateMessage();
                    break;
                case UpdateCheckOutcome.Failed:
                    _appSnackbarMessagesService.ShowControlAppUpdateCheckFailedMessage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Manual ControlApp update check failed.");
            _appSnackbarMessagesService.ShowControlAppUpdateCheckFailedMessage();
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private bool CanCheckForUpdates()
    {
        return !IsCheckingForUpdates;
    }

    [RelayCommand]
    private void RefreshBthPs3()
    {
        BthPs3.Refresh();
    }

    [RelayCommand]
    private void RectifyBthPs3Settings()
    {
        if (BthPs3.TryRectifySettings())
        {
            _appSnackbarMessagesService.ShowBthPS3SettingsRectifiedMessage();
        }
        else
        {
            _appSnackbarMessagesService.ShowBthPS3SettingsRectifyFailedMessage();
        }
    }
}
