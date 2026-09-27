using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models.Onboarding;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

/// <summary>
///     First-run setup window. Shown before the main window on every launch until
///     <see cref="OnboardingCoordinator.IsSatisfied" /> is <see langword="true" />. Closing
///     this window without finishing or accepting the skip warning exits the application,
///     and the next launch resumes at the same required step.
/// </summary>
public partial class OnboardingWindow
{
    private readonly IContentDialogService _contentDialogService = new ContentDialogService();
    private readonly OnboardingCoordinator _coordinator;
    private readonly OnboardingViewModel _viewModel;
    private bool _confirmedExit;

    public OnboardingWindow(OnboardingViewModel viewModel, OnboardingCoordinator coordinator)
    {
        _viewModel = viewModel;
        _coordinator = coordinator;
        DataContext = viewModel;
        InitializeComponent();

        _contentDialogService.SetDialogHost(RootContentDialog);
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

    private async void SkipSetup_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.CanSkip)
        {
            return;
        }

        if (!await ConfirmSkipResponsibilityAsync().ConfigureAwait(true))
        {
            return;
        }

        _viewModel.SkipCommand.Execute(null);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_coordinator.IsSatisfied || _confirmedExit)
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

        if (!_coordinator.IsSatisfied)
        {
            // Setup was not finished or skipped: this window gated the entire application
            // startup, so closing it (by any means) means the user declined setup for this launch.
            App.RequestExit();
        }
    }

    private bool ConfirmExitWithoutFinishing()
    {
        System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
            this,
            "Setup is not finished. Your controller may not work over Bluetooth yet.\n\n" +
            "Closing now will exit DsHidMini ControlApp. You can run setup again next time you start it.",
            "Exit before finishing setup?",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);

        return result == System.Windows.MessageBoxResult.Yes;
    }

    private async Task<bool> ConfirmSkipResponsibilityAsync()
    {
        System.Windows.Controls.TextBlock body = new()
        {
            Text =
                "You are about to skip first-run setup.\n\n" +
                "Getting everything to work is your responsibility and yours alone. " +
                "If you skip this step, support will be declined.\n\n" +
                "Only continue if you understand that you are on your own.",
            TextWrapping = TextWrapping.Wrap
        };

        ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
            new SimpleContentDialogCreateOptions
            {
                Title = "Skip setup?",
                Content = body,
                PrimaryButtonText = "I understand and skip",
                CloseButtonText = "Go back",
                DefaultButton = ContentDialogButton.Close
            });

        return result == ContentDialogResult.Primary;
    }
}
