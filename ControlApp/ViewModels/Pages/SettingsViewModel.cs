using System.Reflection;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;

using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private readonly DshmConfigManager _dshmConfigManager;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

    private bool _isInitialized;

    public SettingsViewModel(DshmConfigManager dshmConfigManager)
    {
        _dshmConfigManager = dshmConfigManager;
    }

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

    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
        {
            InitializeViewModel();
        }

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    private void InitializeViewModel()
    {
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"Dshm_ControlApp_WpfUi - {GetAssemblyVersion()}";

        _isInitialized = true;
    }

    private string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString()
               ?? string.Empty;
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
}