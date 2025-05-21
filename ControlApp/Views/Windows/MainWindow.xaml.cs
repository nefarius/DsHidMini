using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class MainWindow : INavigationWindow
{
    private readonly DshmDevMan _dshmDevMan;

    public MainWindow(
        MainWindowViewModel viewModel,
        DshmDevMan dshmDevMan, //
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        ViewModel = viewModel;
        DataContext = this;

        _dshmDevMan = dshmDevMan;

        SystemThemeWatcher.Watch(this);

        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialog);
    }

    public MainWindowViewModel ViewModel { get; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        InitializeComponent();
        _dshmDevMan.StartListeningForDshmDevices();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        _dshmDevMan.StopListeningForDshmDevices();
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