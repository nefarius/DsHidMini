using System.Diagnostics;

namespace Nefarius.DsHidMini.ControlApp.Models;

public class Main
{
    private static void StartAsAdmin(string fileName, string arguments)
    {
        Process proc = new()
        {
            StartInfo =
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas"
            }
        };

        proc.Start();
    }

    /// <summary>
    ///     Starts an elevated successor and exits this process.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> if this process is already elevated, or the elevated
    ///     successor was launched and this process is shutting down.
    ///     <see langword="false" /> if the user declined UAC or the handoff failed.
    /// </returns>
    public static bool RestartAsAdmin()
    {
        if (SecurityUtil.IsElevated)
        {
            return true;
        }

        Debug.WriteLine("restarting as admin");
        string token = Guid.NewGuid().ToString("N");
        using EventWaitHandle ready = SingleInstanceLifetime.CreateHandoffReadyEvent(token);
        return RestartAsAdminFlow.Run(
            () => StartAsAdmin(
                Environment.ProcessPath!,
                SingleInstanceLifetime.HandoffArgumentPrefix + token),
            Nefarius.DsHidMini.ControlApp.App.ReleaseSingleInstanceOwnership,
            Nefarius.DsHidMini.ControlApp.App.ReacquireSingleInstanceOwnership,
            Nefarius.DsHidMini.ControlApp.App.RequestExit,
            () => ready.WaitOne(TimeSpan.FromSeconds(15)));
    }
}
