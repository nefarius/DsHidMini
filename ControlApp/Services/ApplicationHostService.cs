// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Hosting;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Onboarding;
using Nefarius.DsHidMini.ControlApp.Views.Pages;
using Nefarius.DsHidMini.ControlApp.Views.Windows;

using Wpf.Ui;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Managed host of the application.
/// </summary>
public class ApplicationHostService : IHostedService
{
    private readonly DefenderBtStatusService _defenderBtStatusService;
    private readonly DshmDevMan _dshmDevMan;
    private readonly OnboardingCoordinator _onboardingCoordinator;
    private readonly IServiceProvider _serviceProvider;
    private INavigationWindow _navigationWindow;

    public ApplicationHostService(
        IServiceProvider serviceProvider,
        DshmDevMan dshmDevMan,
        DefenderBtStatusService defenderBtStatusService,
        OnboardingCoordinator onboardingCoordinator)
    {
        _serviceProvider = serviceProvider;
        _dshmDevMan = dshmDevMan;
        _defenderBtStatusService = defenderBtStatusService;
        _onboardingCoordinator = onboardingCoordinator;
    }

    /// <summary>
    ///     Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    /// <summary>
    ///     Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _dshmDevMan.StopListeningForDshmDevices();
        _defenderBtStatusService.StopListening();
        await Task.CompletedTask;
    }

    /// <summary>
    ///     Creates main window during activation.
    /// </summary>
    private async Task HandleActivationAsync()
    {
        await Task.CompletedTask;

        if (!Application.Current.Windows.OfType<MainWindow>().Any())
        {
            if (_onboardingCoordinator.ShouldShowFirstRunWizard)
            {
                OnboardingWindow onboardingWindow = (OnboardingWindow)_serviceProvider.GetService(typeof(OnboardingWindow))!;
                onboardingWindow.ShowDialog();

                if (!_onboardingCoordinator.IsCompleted)
                {
                    // The onboarding window gates the entire startup; declining it (by any means)
                    // means the app should not proceed to the main window this launch.
                    App.RequestExit();
                    return;
                }
            }
            else if (_onboardingCoordinator.IsDeveloperMode && !_onboardingCoordinator.IsCompleted)
            {
                Log.Logger.Information(
                    "Skipping first-run setup because ControlApp is running in developer mode.");
            }

            _navigationWindow = (
                _serviceProvider.GetService(typeof(MainWindow)) as INavigationWindow
            )!;
            _navigationWindow!.ShowWindow();

            _navigationWindow.Navigate(typeof(DevicesPage));
        }

        await Task.CompletedTask;
    }
}