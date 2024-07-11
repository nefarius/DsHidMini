using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace Nefarius.DsHidMini.ControlApp.Models;

public class Main
{
    public static bool IsAdministrator()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void StartAsAdmin(string fileName)
    {
        Process proc = new Process { StartInfo = { FileName = fileName, UseShellExecute = true, Verb = "runas" } };

        proc.Start();
    }

    public static void RestartAsAdmin()
    {
        if (!IsAdministrator())
        {
            Console.WriteLine("restarting as admin");
            StartAsAdmin(Assembly.GetExecutingAssembly().GetName().Name);
            Application.Current.Shutdown();
        }
    }
}