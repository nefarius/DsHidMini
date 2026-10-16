using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;

using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly DshmConfigManager _dshmConfigManager;
    private readonly DshmDevMan _dshmDevMan;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    [ObservableProperty]
    private bool _isIpcRuntimeStale;

    [ObservableProperty]
    private string _ipcRuntimeStatusMessage = string.Empty;

    private int _ipcRuntimeCheckGeneration;

    private bool _isInitialized;

    public SettingsViewModel(
        DshmConfigManager dshmConfigManager,
        DshmDevMan dshmDevMan,
        BthPS3StatusService bthPs3,
        AppSnackbarMessagesService appSnackbarMessagesService)
    {
        _dshmConfigManager = dshmConfigManager;
        _dshmDevMan = dshmDevMan;
        BthPs3 = bthPs3;
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _dshmDevMan.ConnectedDeviceListUpdated += (_, _) => QueueIpcRuntimeCheck();
    }

    public BthPS3StatusService BthPs3 { get; }

    /// <summary>
    ///     True when this process can update the shared driver and profile configuration.
    /// </summary>
    public bool CanEditSharedConfiguration => _dshmConfigManager.CanPersistSharedConfiguration;

    /// <summary>
    ///     When enabled (default), the driver requests a self restart on a HID mode mismatch instead of requiring a
    ///     manual reconnect / second replug (see issue #374).
    /// </summary>
    public bool AutoRestartOnHidModeMismatch
    {
        get => _dshmConfigManager.AutoRestartOnHidModeMismatch;
        set
        {
            if (!CanEditSharedConfiguration || _dshmConfigManager.AutoRestartOnHidModeMismatch == value)
            {
                OnPropertyChanged();
                return;
            }

            _dshmConfigManager.AutoRestartOnHidModeMismatch = value;
            if (!_dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile())
            {
                Log.Logger.Error("Failed to persist AutoRestartOnHidModeMismatch.");
                _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateFailedMessage();
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     When enabled (default), the driver exposes shared-memory IPC. Changes apply at runtime
    ///     without a driver reload.
    /// </summary>
    public bool IPCEnabled
    {
        get => _dshmConfigManager.IPCEnabled;
        set
        {
            if (!CanEditSharedConfiguration || _dshmConfigManager.IPCEnabled == value)
            {
                OnPropertyChanged();
                return;
            }

            _dshmConfigManager.IPCEnabled = value;
            if (!_dshmConfigManager.SaveChangesAndUpdateDsHidMiniConfigFile())
            {
                Log.Logger.Error("Failed to persist IPCEnabled.");
                _appSnackbarMessagesService.ShowDsHidMiniConfigurationUpdateFailedMessage();
            }

            OnPropertyChanged();
            QueueIpcRuntimeCheck();
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
    ///     into PS3 (DualShock 3) mode immediately on arrival (see issue #282). A late button click is less
    ///     reliable because the PS3 sent the probe a few milliseconds after SET_IDLE.
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
        QueueIpcRuntimeCheck();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    private void InitializeViewModel()
    {
        CurrentTheme = ApplicationThemeManager.GetAppTheme();

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

    private void QueueIpcRuntimeCheck()
    {
        int generation = Interlocked.Increment(ref _ipcRuntimeCheckGeneration);
        _ = CheckIpcRuntimeAsync(generation);
    }

    private async Task CheckIpcRuntimeAsync(int generation)
    {
        try
        {
            await CheckIpcRuntimeCoreAsync(generation).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to compare saved driver IPC with a live ping.");
        }
    }

    private async Task CheckIpcRuntimeCoreAsync(int generation)
    {
        DriverIpcConfigReading reading = _dshmConfigManager.ReadDriverIpcConfig();
        bool shownEnabled = _dshmConfigManager.IPCEnabled;
        bool expectsLiveIpc = IpcRuntimeStatus.ExpectsLiveIpc(
            shownEnabled,
            reading,
            _dshmDevMan.Devices.Count > 0);
        if (!expectsLiveIpc)
        {
            PublishIpcRuntime(generation, stale: false, message: string.Empty);
            return;
        }

        bool responded = await Task.Run(IpcRuntimeProbe.TryPingDriver).ConfigureAwait(true);
        if (generation != Volatile.Read(ref _ipcRuntimeCheckGeneration))
        {
            return;
        }

        if (responded)
        {
            PublishIpcRuntime(generation, stale: false, message: string.Empty);
            return;
        }

        PublishIpcRuntime(generation, stale: true, message: IpcRuntimeStatus.DescribeStaleState(shownEnabled));

        // The driver applies IPCEnabled from disk after a short delay. Recheck once so a
        // successful hot-reload does not leave the stale warning on screen.
        await Task.Delay(TimeSpan.FromMilliseconds(400)).ConfigureAwait(true);
        if (generation != Volatile.Read(ref _ipcRuntimeCheckGeneration))
        {
            return;
        }

        reading = _dshmConfigManager.ReadDriverIpcConfig();
        shownEnabled = _dshmConfigManager.IPCEnabled;
        expectsLiveIpc = IpcRuntimeStatus.ExpectsLiveIpc(
            shownEnabled,
            reading,
            _dshmDevMan.Devices.Count > 0);
        responded = expectsLiveIpc && await Task.Run(IpcRuntimeProbe.TryPingDriver).ConfigureAwait(true);
        if (generation != Volatile.Read(ref _ipcRuntimeCheckGeneration))
        {
            return;
        }

        PublishIpcRuntime(
            generation,
            stale: IpcRuntimeStatus.IsDisplayedStateStale(expectsLiveIpc, responded),
            message: IpcRuntimeStatus.IsDisplayedStateStale(expectsLiveIpc, responded)
                ? IpcRuntimeStatus.DescribeStaleState(shownEnabled)
                : string.Empty);
    }

    private void PublishIpcRuntime(int generation, bool stale, string message)
    {
        if (generation != Volatile.Read(ref _ipcRuntimeCheckGeneration))
        {
            return;
        }

        void Apply()
        {
            if (generation != Volatile.Read(ref _ipcRuntimeCheckGeneration))
            {
                return;
            }

            IpcRuntimeStatusMessage = message;
            IsIpcRuntimeStale = stale;
        }

        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Apply);
            return;
        }

        Apply();
    }

}
