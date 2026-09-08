using System.Collections.ObjectModel;
using System.Reflection;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.Views.Pages;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;

    [ObservableProperty]
    private string _applicationTitle = $"DsHidMini ControlApp {GetDisplayVersion()}";

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = new()
    {
        new NavigationViewItem
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(SettingsPage)
        },
        new NavigationViewItem
        {
            Content = "About",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
            TargetPageType = typeof(AboutPage)
        }
    };

    [ObservableProperty]
    private bool _isAppElevated = SecurityUtil.IsElevated;

    [ObservableProperty]
    private ObservableCollection<object> _menuItems = new()
    {
        new NavigationViewItem
        {
            Content = "Devices",
            Icon = new SymbolIcon { Symbol = SymbolRegular.XboxController48 },
            TargetPageType = typeof(DevicesPage)
        },
        new NavigationViewItem
        {
            Content = "Profiles",
            Icon = new SymbolIcon { Symbol = SymbolRegular.DocumentOnePage20 },
            TargetPageType = typeof(ProfilesPage)
        }
    };

    public MainWindowViewModel(AppSnackbarMessagesService appSnackbarMessagesService)
    {
        _appSnackbarMessagesService = appSnackbarMessagesService;
        if (IsAppElevated)
        {
            ApplicationTitle += " [Administrator]";
        }
    }

    public ApplicationConfiguration AppConfig => ApplicationConfiguration.Instance;

    public event EventHandler? OpenFromTrayRequested;

    internal static string GetDisplayVersion()
    {
        string? informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            int metadataSeparator = informational.IndexOf('+');
            return metadataSeparator >= 0 ? informational[..metadataSeparator] : informational;
        }

        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }

    [RelayCommand]
    public void RestartAsAdmin()
    {
        Main.RestartAsAdmin();
    }

    [RelayCommand]
    private void OpenFromTray()
    {
        OpenFromTrayRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ExitFromTray()
    {
        Nefarius.DsHidMini.ControlApp.App.RequestExit();
    }


    /*
    /// <summary>
    ///     Helper to check if run with elevated privileges.
    /// </summary>
    public bool IsElevated => SecurityUtil.IsElevated;

    /// <summary>
    ///     True if run as regular, non-administrative user.
    /// </summary>
    public bool IsMissingPermissions => !IsElevated;

    /// <summary>
    ///     Is it possible to edit the selected device.
    /// </summary>
    public bool IsEditable => IsElevated && HasDeviceSelected && !IsRestarting;

    /// <summary>
    ///     Is the selected device in the process of getting restarted.
    /// </summary>
    public bool IsRestarting { get; set; } = false;

    /// <summary>
    ///     Version to display in window title.
    /// </summary>
    public string Version => $"Version: {Assembly.GetEntryAssembly().GetName().Version}";


    private static string ParametersKey =>
        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WUDF\\Services\\dshidmini\\Parameters";

    /// <summary>
    ///     Indicates if verbose logging is on or off.
    /// </summary>
    public bool VerboseOn
    {
        get
        {
            using (var key = RegistryHelpers.GetRegistryKey(ParametersKey))
            {
                if (key == null) return false;
                var value = key.GetValue("VerboseOn");
                return value != null && (int) value > 0;
            }
        }
        set
        {
            using (var key = RegistryHelpers.GetRegistryKey(ParametersKey, true))
            {
                key?.SetValue("VerboseOn", value ? 1 : 0, RegistryValueKind.DWord);
            }
        }
    }
    */
}