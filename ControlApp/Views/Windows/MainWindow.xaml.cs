using System.ComponentModel;
using System.Windows.Controls;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class MainWindow : INavigationWindow
{
    private readonly DshmDevMan _dshmDevMan;
    private readonly DefenderBtStatusService _defenderBtStatusService;
    private readonly ControlAppUpdateService _updateService;
    private bool _updateCheckStarted;

    public MainWindow(
        MainWindowViewModel viewModel,
        DshmDevMan dshmDevMan, //
        DefenderBtStatusService defenderBtStatusService,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService,
        ControlAppUpdateService updateService
    )
    {
        ViewModel = viewModel;
        DataContext = this;

        _dshmDevMan = dshmDevMan;
        _defenderBtStatusService = defenderBtStatusService;
        _updateService = updateService;

        SystemThemeWatcher.Watch(this);

        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        InitializeComponent();
        ApplySavedPlacement();

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialog);

        ViewModel.OpenFromTrayRequested += (_, _) => RestoreFromTray();
        ViewModel.AppConfig.MinimizeToTrayChanged += OnMinimizeToTrayChanged;

        if (AppNotifyIcon.Menu is ContextMenu menu)
        {
            menu.DataContext = this;
        }
    }

    public MainWindowViewModel ViewModel { get; }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _dshmDevMan.StartListeningForDshmDevices();
        _defenderBtStatusService.StartListening();
        ApplyMinimizeToTraySetting();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_updateCheckStarted)
        {
            return;
        }

        _updateCheckStarted = true;
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _updateService.CheckOnStartupAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Startup update check failed.");
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (TrayWindowPolicy.ShouldHideOnMinimize(ViewModel.AppConfig.MinimizeToTray, WindowState))
        {
            PersistWindowPlacement();
            HideToTray();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        PersistWindowPlacement();

        if (TrayWindowPolicy.ShouldHideInsteadOfClose(ViewModel.AppConfig.MinimizeToTray, App.IsExiting))
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        ViewModel.AppConfig.MinimizeToTrayChanged -= OnMinimizeToTrayChanged;
        if (AppNotifyIcon.IsRegistered)
        {
            AppNotifyIcon.Unregister();
        }

        _dshmDevMan.StopListeningForDshmDevices();
        _defenderBtStatusService.StopListening();
        base.OnClosing(e);
    }

    private void AppNotifyIcon_OnLeftClick(object sender, RoutedEventArgs e)
    {
        RestoreFromTray();
    }

    private void OnMinimizeToTrayChanged(object? sender, EventArgs e)
    {
        ApplyMinimizeToTraySetting();
    }

    private void ApplyMinimizeToTraySetting()
    {
        if (ViewModel.AppConfig.MinimizeToTray)
        {
            if (!AppNotifyIcon.IsRegistered)
            {
                AppNotifyIcon.Register();
            }

            return;
        }

        if (!IsVisible)
        {
            RestoreFromTray();
        }

        if (AppNotifyIcon.IsRegistered)
        {
            AppNotifyIcon.Unregister();
        }
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void ApplySavedPlacement()
    {
        if (!WindowPlacementPolicy.TryRead(ViewModel.AppConfig, out WindowPlacementSnapshot saved))
        {
            return;
        }

        WindowPlacementSnapshot resolved = WindowPlacementPolicy.Resolve(
            saved,
            DisplayWorkAreas.GetDipWorkingAreas(),
            SystemParameters.WorkArea);

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = resolved.Left;
        Top = resolved.Top;
        Width = resolved.Width;
        Height = resolved.Height;
        if (resolved.State == WindowState.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void PersistWindowPlacement()
    {
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 ||
            double.IsNaN(bounds.Left) || double.IsNaN(bounds.Top))
        {
            return;
        }

        WindowPlacementPolicy.Write(ViewModel.AppConfig, WindowState, bounds);
        try
        {
            ViewModel.AppConfig.Save();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to persist window placement.");
        }
    }


    #region INavigationWindow methods

    public INavigationView GetNavigation()
    {
        return RootNavigation;
    }

    public bool Navigate(Type pageType)
    {
        return RootNavigation.Navigate(pageType);
    }

    public void SetPageService(INavigationViewPageProvider pageService)
    {
        RootNavigation.SetPageProviderService(pageService);
    }

    public void ShowWindow()
    {
        Show();
    }

    public void CloseWindow()
    {
        Close();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    #endregion INavigationWindow methods
}
