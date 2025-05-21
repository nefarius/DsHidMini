using System.IO;
using System.Windows.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;
using Nefarius.DsHidMini.ControlApp.Views.Pages;
using Nefarius.DsHidMini.ControlApp.Views.Windows;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace Nefarius.DsHidMini.ControlApp;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost AppHost = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c =>
        {
            c.SetBasePath(Path.GetDirectoryName(Environment.ProcessPath!)!);
        })
        .ConfigureServices((context, services) =>
        {
            services.AddHostedService<ApplicationHostService>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddNavigationViewPageProvider();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            services.AddSingleton<DeviceNotificationListener>();
            services.AddSingleton<AppSnackbarMessagesService>();

            services.AddSingleton<DshmDevMan>();
            services.AddSingleton<DshmConfigManager>();

            services.AddSingleton<DevicesPage>();
            services.AddSingleton<DevicesViewModel>();
            services.AddSingleton<ProfilesPage>();
            services.AddSingleton<ProfilesViewModel>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<Main>();

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .WriteTo.File(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "DsHidMini\\Log\\ControlAppLog.txt"))
                .CreateLogger();

            services.AddSerilog(Log.Logger);

            services.AddHttpClient("Buildbot", client =>
            {
                client.BaseAddress = new Uri("https://buildbot.nefarius.at/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(context.HostingEnvironment.ApplicationName);
            });
        }).Build();

    /// <summary>
    ///     Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        Log.Logger.Information("App startup");
        AppHost.Start();
    }

    /// <summary>
    ///     Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        Log.Logger.Information("App exiting");
        await AppHost.StopAsync();
        AppHost.Dispose();
    }

    /// <summary>
    ///     Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0

        Log.Logger.Fatal(e.Exception, "Unhandled exception");
    }
}