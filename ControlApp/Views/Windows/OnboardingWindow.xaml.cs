using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models.Onboarding;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

/// <summary>
///     Mandatory, unskippable first-run setup window. Shown before the main window on every launch
///     until <see cref="OnboardingCoordinator.IsCompleted" /> is <see langword="true" />. There is no
///     Skip, Later, or close-button bypass: closing this window (by any means) without finishing
///     exits the application, and the next launch resumes at the same required step.
/// </summary>
public partial class OnboardingWindow
{
    private readonly OnboardingCoordinator _coordinator;
    private readonly OnboardingViewModel _viewModel;
    private bool _confirmedExit;

    public OnboardingWindow(OnboardingViewModel viewModel, OnboardingCoordinator coordinator)
    {
        _viewModel = viewModel;
        _coordinator = coordinator;
        DataContext = viewModel;
        InitializeComponent();

        _viewModel.SetupCompleted += OnSetupCompleted;
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        _confirmedExit = true; // Not an exit, but skips the "are you sure" close prompt below.
        Close();
    }

    private void ExitApplication_OnClick(object sender, RoutedEventArgs e)
    {
        if (ConfirmExitWithoutFinishing())
        {
            _confirmedExit = true;
            Close();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_coordinator.IsCompleted || _confirmedExit)
        {
            base.OnClosing(e);
            return;
        }

        if (!ConfirmExitWithoutFinishing())
        {
            // User chose not to exit: leave the running setup untouched rather than having already
            // cancelled it before asking.
            e.Cancel = true;
            return;
        }

        if (_viewModel.IsBusy)
        {
            _viewModel.CancelCommand.Execute(null);
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SetupCompleted -= OnSetupCompleted;
        _viewModel.Dispose();
        base.OnClosed(e);

        if (!_coordinator.IsCompleted)
        {
            // Setup was not finished: this window gated the entire application startup, so
            // closing it (by any means) means the user declined setup for this launch.
            App.RequestExit();
        }
    }

    private bool ConfirmExitWithoutFinishing()
    {
        MessageBoxResult result = MessageBox.Show(
            this,
            "Setup is not finished. Your controller may not work over Bluetooth yet.\n\n" +
            "Closing now will exit DsHidMini ControlApp. You can run setup again next time you start it.",
            "Exit before finishing setup?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }
}
