global using Nefarius.DsHidMini.Setup.Util;

using System;
using System.Collections.Generic;
using System.Linq;

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
        ManagedProject project = new("DsHidMini",
            new Dir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new Dir("drivers",
                    new Files(@"..\artifacts\drivers\*.*"),
                    new Files(@"..\artifacts\igfilter\*.*")
                )
            ),
            new ManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallInitialize,
                Condition.NOT_Installed)
        );

        project.Platform = Platform.x64;
        project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
        project.LicenceFile = "EULA.rtf";
        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);
        project.AfterInstall += ProjectOnAfterInstall;

        //project.SourceBaseDir = "<input dir path>";
        //project.OutDir = "<output dir path>";

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
        bool rebootRequired = UninstallDrivers();

        // TODO: implement me!

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