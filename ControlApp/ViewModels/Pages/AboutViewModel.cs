using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

public partial class AboutViewModel : ObservableObject, INavigationAware
{
    private readonly AppSnackbarMessagesService _appSnackbarMessagesService;
    private readonly ControlAppUpdateService _updateService;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isCheckingForUpdates;

    private bool _isInitialized;

    public AboutViewModel(
        AppSnackbarMessagesService appSnackbarMessagesService,
        ControlAppUpdateService updateService)
    {
        _appSnackbarMessagesService = appSnackbarMessagesService;
        _updateService = updateService;
    }

    public string CopyrightNotice { get; } = "Copyright (c) 2020–2026, Benjamin Höglinger-Stelzer";

    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
        {
            AppVersion = $"DsHidMini ControlApp {MainWindowViewModel.GetDisplayVersion()}";
            _isInitialized = true;
        }

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
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
}
