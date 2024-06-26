#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using CliWrap;

using Microsoft.Deployment.WindowsInstaller;

using Nefarius.DsHidMini.Setup.Util;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Newtonsoft.Json;

using WixSharp;

using File = WixSharp.File;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    public const string ProductName = "Nefarius DsHidMini Driver";

    private static void Main()
    {
        // grab main app version
        Version version = Version.Parse(BuildVariables.SetupVersion);
        string driverPath = Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\artifacts\drivers\dshidmini_x64\dshidmini.dll");
        Version driverVersion = Version.Parse(FileVersionInfo.GetVersionInfo(driverPath).FileVersion);

        string filterPath = Path.Combine(AppContext.BaseDirectory,
            @"..\..\..\..\artifacts\igfilter\nssmkig_x64\nssmkig.sys");
        Version filterVersion = Version.Parse(FileVersionInfo.GetVersionInfo(filterPath).FileVersion);

        Console.WriteLine($"Setup version: {version}");
        Console.WriteLine($"Driver version: {driverVersion}");
        Console.WriteLine($"Filter version: {filterVersion}");

        Feature driversFeature = new("DsHidMini Drivers", true, false)
        {
            Description = "Installs the Nefarius DsHidMini drivers for PS3 peripherals. " +
                          "This is a mandatory core component and can't be de-selected."
        };

        Feature bthPs3Feature = new("BthPS3 Wireless Drivers", false, true)
        {
            Description = "When selected, downloads the latest version of the " +
                          "Nefarius BthPS3 Bluetooth Drivers for wireless connectivity."
        };

        driversFeature.Add(bthPs3Feature);
        driversFeature.Display = FeatureDisplay.expand;

        ManagedProject project = new(ProductName,
            // included files
            new InstallDir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir(driversFeature, "drivers",
                    new Files(driversFeature, @"..\artifacts\drivers\*.*"),
                    new Files(driversFeature, @"..\artifacts\igfilter\*.*")
                ),
                new File(driversFeature, "nefarius_DsHidMini_Updater.exe")
            ),
            // install drivers
            new ManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed),
            // install BthPS3
            new ManagedAction(CustomActions.InstallBthPS3, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed),
            // register updater
            new ManagedAction(CustomActions.RegisterUpdater, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed),
            // open beta article
            new ManagedAction(CustomActions.OpenBetaArticle, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed),
            // remove updater cleanly
            new ManagedAction(CustomActions.DeregisterUpdater, Return.check,
                When.Before,
                Step.RemoveFiles,
                Condition.Installed),
            // registry values
            new RegKey(driversFeature, RegistryHive.LocalMachine,
                $@"Software\Nefarius Software Solutions e.U.\{ProductName}",
                new RegValue("Path", "[INSTALLDIR]") { Win64 = true },
                new RegValue("Version", version.ToString()) { Win64 = true },
                new RegValue("DriverVersion", driverVersion.ToString()) { Win64 = true },
                new RegValue("FilterVersion", filterVersion.ToString()) { Win64 = true }
            ) { Win64 = true }
        )
        {
            UI = WUI.WixUI_FeatureTree,
            OutFileName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{version}",
            Version = version,
            Platform = Platform.x64,
            GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
            LicenceFile = "EULA.rtf",
            WildCardDedup = Project.UniqueFileNameDedup,
            MajorUpgradeStrategy = MajorUpgradeStrategy.Default,
            BannerImage = "DsHidMini.dialog_banner.bmp",
            ValidateBackgroundImage = false,
            BackgroundImage = "DsHidMini.dialog_background.bmp",
            CAConfigFile = "CustomActions.config"
        };

        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);
        // embed types of CliWrap
        project.DefaultRefAssemblies.Add(typeof(Cli).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(ValueTask).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(IAsyncDisposable).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Unsafe).Assembly.Location);
        // embed types for web calls
        project.DefaultRefAssemblies.Add(typeof(WebClient).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(JsonSerializer).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Binder).Assembly.Location);

        project.AfterInstall += ProjectOnAfterInstall;

        //project.SourceBaseDir = "<input dir path>";
        //project.OutDir = "<output dir path>";

        project.ControlPanelInfo.ProductIcon =
            Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\assets\FireShock.ico");
        project.ControlPanelInfo.Manufacturer = "Nefarius Software Solutions e.U.";
        project.ControlPanelInfo.HelpLink = "https://docs.nefarius.at/Community-Support/";
        project.ControlPanelInfo.UrlInfoAbout = "https://github.com/nefarius/DsHidMini";

        project.MajorUpgradeStrategy.PreventDowngradingVersions.OnlyDetect = false;

        project.ResolveWildCards();

        project.BuildMsi();
    }

    /// <summary>
    ///     Put uninstall logic that doesn't access packaged files in here.
    /// </summary>
    private static void ProjectOnAfterInstall(SetupEventArgs e)
    {
        if (e.IsUninstalling)
        {
            CustomActions.UninstallDrivers(e.Session);
        }
    }
}

public static class CustomActions
{
    /// <summary>
    ///     Put install logic here.
    /// </summary>
    [CustomAction]
    public static ActionResult InstallDrivers(Session session)
    {
        // clean out whatever has been on the machine before
        bool rebootRequired = UninstallDrivers(session);

        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        string driversDir = Path.Combine(installDir.FullName, "drivers");
        string archShortName = RuntimeInformation.OSArchitecture.ToString();

        string dshidminiDriverDir = Path.Combine(driversDir, $"dshidmini_{archShortName}");
        string igfilterDriverDir = Path.Combine(driversDir, $"nssmkig_{archShortName}");

        string dshidminiInfPath = Path.Combine(dshidminiDriverDir, "dshidmini.inf");
        string igfilterInfPath = Path.Combine(igfilterDriverDir, "igfilter.inf");

        if (!Devcon.Install(igfilterInfPath, out bool igfilterRebootRequired))
        {
            return ActionResult.Failure;
        }

        if (igfilterRebootRequired)
        {
            rebootRequired = true;
        }

        if (!Devcon.Install(dshidminiInfPath, out bool dshidminiRebootRequired))
        {
            return ActionResult.Failure;
        }

        if (dshidminiRebootRequired)
        {
            rebootRequired = true;
        }

        Devcon.Refresh();

        session.SetMode(InstallRunMode.RebootAtEnd, rebootRequired);

        return ActionResult.Success;
    }

    /// <summary>
    ///     Download and install BthPS3.
    /// </summary>
    [CustomAction]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static ActionResult InstallBthPS3(Session session)
    {
        FeatureInfo bthPs3Feature =
            session.Features.Single(f => f.Name.Contains("BthPS3", StringComparison.OrdinalIgnoreCase));

        // not selected by user
        if (bthPs3Feature.RequestState == InstallState.Unknown)
        {
            return ActionResult.Success;
        }

        try
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using WebClient client = new();
            client.Headers.Add("X-Vicius-OS-Architecture", RuntimeInformation.OSArchitecture.ToString());

            string json = client.DownloadString("https://vicius.api.nefarius.systems/api/nefarius/BthPS3/updates.json");
            UpdateResponse? response = JsonConvert.DeserializeObject<UpdateResponse>(json);
            Release? latestRelease = response!.Releases.OrderByDescending(r => r.Version).First();
            Uri downloadUrl = new(latestRelease.DownloadUrl);
            session.Log($"BthPS3 download URL: {downloadUrl}");

            _ = Cli.Wrap("explorer")
                .WithArguments(downloadUrl.ToString())
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log($"BthPS3 download/install failed: {ex}");
        }

        return ActionResult.Success;
    }

    /// <summary>
    ///     Open beta article in default browser.
    /// </summary>
    [CustomAction]
    public static ActionResult OpenBetaArticle(Session session)
    {
        _ = Cli.Wrap("explorer")
            .WithArguments("https://docs.nefarius.at/projects/DsHidMini/Experimental/Version-3-Beta/")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        return ActionResult.Success;
    }

    /// <summary>
    ///     Register the auto-updater.
    /// </summary>
    [CustomAction]
    public static ActionResult RegisterUpdater(Session session)
    {
        DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
        string updaterPath = Path.Combine(installDir.FullName, "nefarius_DsHidMini_Updater.exe");

        _ = Cli.Wrap(updaterPath)
            .WithArguments(builder => builder
                .Add("--install")
                .Add("--silent")
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        return ActionResult.Success;
    }

    /// <summary>
    ///     De-register the auto-updater.
    /// </summary>
    [CustomAction]
    public static ActionResult DeregisterUpdater(Session session)
    {
        try
        {
            DirectoryInfo installDir = new(session.Property("INSTALLDIR"));
            string updaterPath = Path.Combine(installDir.FullName, "nefarius_DsHidMini_Updater.exe");

            _ = Cli.Wrap(updaterPath)
                .WithArguments(builder => builder
                    .Add("--uninstall")
                    .Add("--silent")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            return ActionResult.Success;
        }
        catch
        {
            //
            // Not failing a removal here
            // 
            return ActionResult.Success;
        }
    }

    /// <summary>
    ///     Uninstalls and cleans all driver residue.
    /// </summary>
    public static bool UninstallDrivers(Session session)
    {
        List<string> allDriverPackages = DriverStore.ExistingDrivers.ToList();

        // remove all old copies of DsHidMini
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("dshidmini.inf", StringComparison.OrdinalIgnoreCase)))
        {
            DriverStore.RemoveDriver(driverPackage);
        }

        // remove all old copies of igfilter
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("igfilter.inf", StringComparison.OrdinalIgnoreCase)))
        {
            DriverStore.RemoveDriver(driverPackage);
        }

        // 
        // Remove conflicting drivers
        // 

        // https://docs.nefarius.at/projects/DsHidMini/How-to-Install/#scptoolkit
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("ds3controller.inf", StringComparison.OrdinalIgnoreCase) ||
                     p.Contains("ds3controller_", StringComparison.OrdinalIgnoreCase)))
        {
            DriverStore.RemoveDriver(driverPackage);
        }

        // https://docs.nefarius.at/projects/DsHidMini/How-to-Install/#fireshock
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("fireshock.inf", StringComparison.OrdinalIgnoreCase)))
        {
            DriverStore.RemoveDriver(driverPackage);
        }

        // https://docs.nefarius.at/projects/DsHidMini/SIXAXIS.SYS-to-DsHidMini-Guide/
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("sixaxis.inf", StringComparison.OrdinalIgnoreCase)))
        {
            DriverStore.RemoveDriver(driverPackage);
        }

        bool rebootRequired = false;
        int instance = 0;
        // uninstall live copies of drivers in use by connected or orphaned devices
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out PnPDevice device, instance++, false))
        {
            device.Uninstall(out bool reboot);

            if (reboot)
            {
                rebootRequired = true;
            }
        }

        return rebootRequired;
    }
}