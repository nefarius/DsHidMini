﻿global using Nefarius.DsHidMini.Setup.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Microsoft.Deployment.WindowsInstaller;

using Nefarius.DsHidMini.Setup.Util;
using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using WixSharp;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    private static void Main()
    {
        // grab main app version
        Version version = Version.Parse("3.0.0"); // TODO: make configurable

        ManagedProject project = new("DsHidMini",
            new InstallDir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir("drivers",
                    new Files(@"..\artifacts\drivers\*.*"),
                    new Files(@"..\artifacts\igfilter\*.*")
                )
            ),
            new ManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallFinalize,
                Condition.NOT_Installed)
        )
        {
            Version = version,
            Platform = Platform.x64,
            GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b"),
            LicenceFile = "EULA.rtf",
            WildCardDedup = Project.UniqueFileNameDedup,
            MajorUpgradeStrategy = MajorUpgradeStrategy.Default
        };

        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);
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
            CustomActions.UninstallDrivers();
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
        bool rebootRequired = UninstallDrivers();

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

    public static bool UninstallDrivers()
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