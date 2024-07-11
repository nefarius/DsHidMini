using System.Diagnostics;

namespace Nefarius.DsHidMini.ControlApp.Models;

public class Main
{
    private static void StartAsAdmin(string fileName)
    {
        Process proc = new() { StartInfo = { FileName = fileName, UseShellExecute = true, Verb = "runas" } };

        proc.Start();
    }

    public static void RestartAsAdmin()
    {
        if (SecurityUtil.IsElevated)
        {
            return;
        }

        Debug.WriteLine("restarting as admin");
        StartAsAdmin(Environment.ProcessPath!);
        Application.Current.Shutdown();
    }
}