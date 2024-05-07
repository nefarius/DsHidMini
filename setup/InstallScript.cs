using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Deployment.WindowsInstaller;

using Nefarius.Utilities.DeviceManagement.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using WixSharp;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    private static void Main()
    {
        Project project = new("DsHidMini",
            new Dir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new File("InstallScript.cs") /* TODO: dummy demo file, remove */),
            new ManagedAction(CustomActions.InstallDrivers, Return.check,
                When.After,
                Step.InstallInitialize,
                Condition.NOT_Installed));

        project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
        project.LicenceFile = "EULA.rtf";
        // embed types of Nefarius.Utilities.DeviceManagement
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);

        //project.SourceBaseDir = "<input dir path>";
        //project.OutDir = "<output dir path>";

        project.BuildMsi();
    }
}

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
}

public static class DsHidMiniDriver
{
    /// <summary>
    ///     Interface GUID common to all devices the DsHidMini driver supports.
    /// </summary>
    public static Guid DeviceInterfaceGuid => Guid.Parse("{399ED672-E0BD-4FB3-AB0C-4955B56FB86A}");
}

public static class CustomActions
{
    [CustomAction]
    public static ActionResult InstallDrivers(Session session)
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

        // TODO: implement me!

        session.SetMode(InstallRunMode.RebootAtEnd, rebootRequired);

        return ActionResult.Success;
    }
}