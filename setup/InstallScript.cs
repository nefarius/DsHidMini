#nullable enable
using System;
using System.Buffers;
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
using CliWrap.Buffered;

using Nefarius.DsHidMini.Setup.Dialogs;
using Nefarius.DsHidMini.Setup.Util;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.Exceptions;
using Nefarius.Utilities.DeviceManagement.PnP;

using Newtonsoft.Json;

using WixSharp;

using WixToolset.Dtf.WindowsInstaller;

using File = WixSharp.File;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    public const string ProductName = "Nefarius DsHidMini Driver";

    public static Uri BetaArticleUrl =
        new Uri("https://docs.nefarius.at/projects/DsHidMini/Experimental/Version-3-Beta/");

    private static void Main()
    {
        // grab main app version
        Version version = Version.Parse(BuildVariables.SetupVersion);
        const string driverPath = @"..\artifacts\drivers\dshidmini_x64\dshidmini.dll";
        Version driverVersion = Version.Parse(FileVersionInfo.GetVersionInfo(driverPath).FileVersion);

        const string filterPath = @"..\artifacts\igfilter\nssmkig_x64\nssmkig.sys";
        Version filterVersion = Version.Parse(FileVersionInfo.GetVersionInfo(filterPath).FileVersion);

        const string nefconDir = @".\nefcon";

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
            Id = "BthPS3Feature",
            Description = "When selected, downloads the latest version of the " +
                          "Nefarius BthPS3 Bluetooth Drivers for wireless connectivity. " + 
                          "You need to go through the BthPS3 installation AFTER this installation has finished."
        };

        Feature donationFeature = new("Make a donation", true, true)
        {
            Id = "DonationFeature", Description = "Opens the donation page after setup is finished."
        };

        // TODO: enable after Beta is over
        //driversFeature.Add(donationFeature);
        driversFeature.Add(bthPs3Feature);
        driversFeature.Display = FeatureDisplay.expand;

        ManagedProject project = new(ProductName,
            // included files
            new InstallDir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir(driversFeature, "nefcon")
                {
                    Files = new DirFiles(driversFeature, "*.*").GetFiles(nefconDir),
                    Dirs = GetSubDirectories(driversFeature, nefconDir).ToArray()
                },
                new Dir(driversFeature, "drivers",
                    new Files(driversFeature, @"..\artifacts\drivers\*.*"),
                    new Files(driversFeature, @"..\artifacts\igfilter\*.*")
                ),
                new File(driversFeature, "nefarius_DsHidMini_Updater.exe")
            ),
            // install drivers
            new ElevatedManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallFiles,
                Condition.NOT_Installed),
            // custom reboot prompt message
            new Error("9000",
                "Driver installation succeeded but a reboot is required to be fully operational. " +
                "After the setup is finished, please reboot the system before using the software."),
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
            // open donation page
            new ManagedAction(CustomActions.OpenDonationPage, Return.check,
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
            ManagedUI = new ManagedUI(),
            OutFileName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{version}",
            Version = version,
            Platform = Platform.x64,
            GUID = new Guid("25784100-B9AA-4205-8D54-CA53717F6AC5"),
            LicenceFile = "EULA.rtf",
            WildCardDedup = Project.UniqueFileNameDedup,
            MajorUpgradeStrategy = MajorUpgradeStrategy.Default,
            BannerImage = "DsHidMini.dialog_banner.bmp",
            BackgroundImage = "DsHidMini.dialog_background.bmp",
            CAConfigFile = "CustomActions.config"
        };

        project.ManagedUI.InstallDialogs.Add<WelcomeDialog>()
            .Add<LicenceDialog>()
            .Add<FeaturesDialog>()
            .Add<ProgressDialog>()
            .Add<BetaArticleDialog>()
            .Add<ExitDialog>();

        project.ManagedUI.ModifyDialogs.Add<MaintenanceTypeDialog>()
            .Add<FeaturesDialog>()
            .Add<ProgressDialog>()
            .Add<ExitDialog>();

        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);
        // embed types of CliWrap
        project.DefaultRefAssemblies.Add(typeof(Cli).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(ValueTask).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(IAsyncDisposable).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Unsafe).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(BuffersExtensions).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(ArrayPool<>).Assembly.Location);
        // embed types for web calls
        project.DefaultRefAssemblies.Add(typeof(WebClient).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(JsonSerializer).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Binder).Assembly.Location);

        project.AfterInstall += ProjectOnAfterInstall;

        project.ControlPanelInfo.ProductIcon = @"..\assets\FireShock.ico";
        project.ControlPanelInfo.Manufacturer = "Nefarius Software Solutions e.U.";
        project.ControlPanelInfo.HelpLink = "https://docs.nefarius.at/Community-Support/";
        project.ControlPanelInfo.UrlInfoAbout = "https://github.com/nefarius/DsHidMini";
        project.ControlPanelInfo.NoModify = true;

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

    /// <summary>
    ///     Recursively resolves all subdirectories and their containing files.
    /// </summary>
    private static List<Dir> GetSubDirectories(Feature feature, string directory)
    {
        List<Dir> subDirectoryInfosCollection = new();

        foreach (string subDirectory in Directory.GetDirectories(directory))
        {
            string subDirectoryName = subDirectory.Remove(0, subDirectory.LastIndexOf('\\') + 1);
            Dir newDir =
                new(feature, subDirectoryName, new Files(feature, subDirectory + @"\*.*")) { Name = subDirectoryName };
            subDirectoryInfosCollection.Add(newDir);

            // Recursively traverse nested directories
            GetSubDirectories(feature, subDirectory);
        }

        return subDirectoryInfosCollection;
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
        session.Log($"installDir = {installDir}");
        string driversDir = Path.Combine(installDir.FullName, "drivers");
        session.Log($"driversDir = {driversDir}");
        string nefconDir = Path.Combine(installDir.FullName, "nefcon");
        session.Log($"nefconDir = {nefconDir}");
        string archShortName = ArchitectureInfo.PlatformShortName;
        session.Log($"archShortName = {archShortName}");

        string nefconcPath = Path.Combine(nefconDir, archShortName, "nefconc.exe");
        session.Log($"nefconcPath = {nefconcPath}");

        string dshidminiDriverDir = Path.Combine(driversDir, $"dshidmini_{archShortName}");
        session.Log($"dshidminiDriverDir = {dshidminiDriverDir}");
        string igfilterDriverDir = Path.Combine(driversDir, $"nssmkig_{archShortName}");
        session.Log($"igfilterDriverDir = {igfilterDriverDir}");

        string dshidminiInfPath = Path.Combine(dshidminiDriverDir, "dshidmini.inf");
        session.Log($"dshidminiInfPath = {dshidminiInfPath}");
        string igfilterInfPath = Path.Combine(igfilterDriverDir, "igfilter.inf");
        session.Log($"igfilterInfPath = {igfilterInfPath}");

        BufferedCommandResult? result = Cli.Wrap(nefconcPath)
            .WithArguments(builder => builder
                .Add("--inf-default-install")
                .Add("--inf-path")
                .Add(igfilterInfPath)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync()
            .GetAwaiter()
            .GetResult();

        session.Log($"igfilter command stdout: {result.StandardOutput}");
        session.Log($"igfilter command stderr: {result.StandardError}");

        if (result?.ExitCode == 3010)
        {
            rebootRequired = true;
        }

        if (result?.ExitCode != 0 && result?.ExitCode != 3010)
        {
            session.Log(
                $"Filter installer failed with exit code: {result?.ExitCode}, message: {Win32Exception.GetMessageFor(result?.ExitCode)}");

            return ActionResult.Failure;
        }

        if (!Devcon.Install(dshidminiInfPath, out bool dshidminiRebootRequired))
        {
            int error = Marshal.GetLastWin32Error();
            session.Log(
                $"Driver installation failed with win32 error: {error}, message: {Win32Exception.GetMessageFor(error)}");

            return ActionResult.Failure;
        }

        if (dshidminiRebootRequired)
        {
            rebootRequired = true;
        }

        Devcon.Refresh();

        if (rebootRequired)
        {
            Record record = new(1);
            record[1] = "9000";

            session.Message(
                InstallMessage.User | (InstallMessage)MessageButtons.OK | (InstallMessage)MessageIcon.Information,
                record);
        }

        return ActionResult.Success;
    }

    /// <summary>
    ///     Download and install BthPS3.
    /// </summary>
    [CustomAction]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static ActionResult InstallBthPS3(Session session)
    {
        if (!session.IsFeatureEnabled("BthPS3Feature"))
        {
            return ActionResult.Success;
        }

        try
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using WebClient client = new();
            client.Headers.Add("X-Vicius-OS-Architecture", ArchitectureInfo.PlatformShortName);

            string json = client.DownloadString("https://vicius.api.nefarius.systems/api/nefarius/BthPS3/updates.json");
            UpdateResponse? response = JsonConvert.DeserializeObject<UpdateResponse>(json);
            Release? latestRelease = response!.Releases.OrderByDescending(r => r.Version).First();
            Uri downloadUrl = new(latestRelease.DownloadUrl);
            session.Log($"BthPS3 download URL: {downloadUrl}");

            CommandResult? result = Cli.Wrap("explorer")
                .WithArguments(downloadUrl.ToString())
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            session.Log(
                $"Download URL launch {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

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
        try
        {
            Process.Start(InstallScript.BetaArticleUrl.ToString());
        }
        catch (Exception ex)
        {
            session.Log(
                $"Beta article launch failed, exception: {ex}");
        }

        return ActionResult.Success;
    }

    /// <summary>
    ///     Open donations page in default browser.
    /// </summary>
    [CustomAction]
    public static ActionResult OpenDonationPage(Session session)
    {
        if (!session.IsFeatureEnabled("DonationFeature"))
        {
            return ActionResult.Success;
        }

        CommandResult? result = Cli.Wrap("explorer")
            .WithArguments("https://docs.nefarius.at/Donations/")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        session.Log(
            $"Donations page launch {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

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

        CommandResult? result = Cli.Wrap(updaterPath)
            .WithArguments(builder => builder
                .Add("--install")
                .Add("--silent")
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        session.Log(
            $"Updater registration {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

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

            CommandResult? result = Cli.Wrap(updaterPath)
                .WithArguments(builder => builder
                    .Add("--uninstall")
                    .Add("--silent")
                )
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();

            session.Log(
                $"Updater de-registration {(result.IsSuccess ? "succeeded" : "failed")}, exit code: {result.ExitCode}");

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
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of dshidmini package {driverPackage} failed with error {ex}");
            }
        }

        // remove all old copies of igfilter
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("igfilter.inf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of igfilter package {driverPackage} failed with error {ex}");
            }
        }

        // 
        // Remove conflicting drivers
        // 

        // https://docs.nefarius.at/projects/DsHidMini/How-to-Install/#scptoolkit
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("ds3controller.inf", StringComparison.OrdinalIgnoreCase) ||
                     p.Contains("ds3controller_", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of SCP DS3 package {driverPackage} failed with error {ex}");
            }
        }

        // https://docs.nefarius.at/projects/DsHidMini/How-to-Install/#fireshock
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("fireshock.inf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of fireshock package {driverPackage} failed with error {ex}");
            }
        }

        // https://docs.nefarius.at/projects/DsHidMini/SIXAXIS.SYS-to-DsHidMini-Guide/
        foreach (string driverPackage in allDriverPackages.Where(p =>
                     p.Contains("sixaxis.inf", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                DriverStore.RemoveDriver(driverPackage);
            }
            catch (Exception ex)
            {
                session.Log($"Removal of sixaxis package {driverPackage} failed with error {ex}");
            }
        }

        bool rebootRequired = false;
        int instance = 0;
        // uninstall live copies of drivers in use by connected or orphaned devices
        while (Devcon.FindByInterfaceGuid(DsHidMiniDriver.DeviceInterfaceGuid, out PnPDevice device, instance++, false))
        {
            try
            {
                device.Uninstall(out bool reboot);

                if (reboot)
                {
                    rebootRequired = true;
                }
            }
            catch (Exception ex)
            {
                session.Log($"Removal of device instance {device} failed with error {ex}");
            }
        }

        return rebootRequired;
    }
}