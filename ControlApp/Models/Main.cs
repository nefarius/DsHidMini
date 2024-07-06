using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Nefarius.DsHidMini.ControlApp.Models;
public class Main
{
    public static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void StartAsAdmin(string fileName)
    {
        var proc = new Process
        {
            StartInfo =
        {
            FileName = fileName,
            UseShellExecute = true,
            Verb = "runas"
        }
        };

        proc.Start();
    }

    public static void RestartAsAdmin()
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("restarting as admin");
            StartAsAdmin(Assembly.GetExecutingAssembly().GetName().Name);
            App.Current.Shutdown();
            return;
        }
    }
}
