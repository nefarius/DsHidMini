using System;
using System.Windows.Forms;

using Microsoft.Deployment.WindowsInstaller;

using Nefarius.Utilities.DeviceManagement.PnP;

using WixSharp;

namespace Nefarius.DsHidMini.Setup;

internal class InstallScript
{
    private static void Main()
    {
        Project project = new("DsHidMini",
            new Dir(@"%ProgramFiles%\Nefarius Software Solutions\DsHidMini",
                new File("InstallScript.cs")),
            new ManagedAction(CustomActions.MyManagedAction, Return.check,
                When.After,
                Step.InstallInitialize,
                Condition.NOT_Installed));

        project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
        project.LicenceFile = "DsHidMini.licence.rtf";
        project.DefaultRefAssemblies.Add(typeof(Devcon).Assembly.Location);

        //project.SourceBaseDir = "<input dir path>";
        //project.OutDir = "<output dir path>";

        project.BuildMsi();
    }
}

public static class CustomActions
{
    [CustomAction]
    public static ActionResult MyManagedAction(Session session)
    {
        bool found = Devcon.FindByInterfaceGuid(DeviceInterfaceIds.HidDevice, out _);

        MessageBox.Show(found.ToString(), "Embedded Managed CA");

        return ActionResult.Success;
    }
}