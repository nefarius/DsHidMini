using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using CliWrap;

using Microsoft.Deployment.WindowsInstaller;

using Nefarius.DsHidMini.Setup.Util;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using WixSharp;

using File = WixSharp.File;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    private const string ProductName = "Nefarius DsHidMini Driver";

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

        Feature fullSetup = new();

        ManagedProject project = new(ProductName,
            // included files
            new InstallDir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir("drivers",
                    new Files(@"..\artifacts\drivers\*.*"),
                    new Files(@"..\artifacts\igfilter\*.*")
                ),
                new File("nefarius_DsHidMini_Updater.exe")
            ),
            // install action
            new ManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed),
            // registry values
            new RegKey(fullSetup, RegistryHive.LocalMachine,
                $@"Software\Nefarius Software Solutions e.U.\{ProductName}",
                new RegValue("Path", "[INSTALLDIR]") { Win64 = true },
                new RegValue("Version", version.ToString()) { Win64 = true },
                new RegValue("DriverVersion", driverVersion.ToString()) { Win64 = true },
                new RegValue("FilterVersion", filterVersion.ToString()) { Win64 = true }
            ) { Win64 = true }
        )
        {
            OutFileName = $"Nefarius_DsHidMini_Drivers_x64_arm64_v{version}",
            Version = version,
            Platform = Platform.x64,
            GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
            LicenceFile = "EULA.rtf",
            WildCardDedup = Project.UniqueFileNameDedup,
            MajorUpgradeStrategy = MajorUpgradeStrategy.Default
        };

        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Cli).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(ValueTask).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(IAsyncDisposable).Assembly.Location);
        project.DefaultRefAssemblies.Add(typeof(Unsafe).Assembly.Location);
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
    ///     Put uninstall logic here.
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

        _ = Cli.Wrap("explorer")
            .WithArguments("https://docs.nefarius.at/projects/DsHidMini/Experimental/Version-3-Beta/")
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();

        return ActionResult.Success;
    }

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

        bool rebootRequired = false;
        int instance = 0;
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