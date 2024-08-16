// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using DsHidMiniInterop ipc = new();

Ds3RawInputReport report = new();

do
{
    while (!Console.KeyAvailable)
    {
        Stopwatch stopwatch = new();

        int executionCount = 0;

        stopwatch.Restart();

        while (stopwatch.ElapsedMilliseconds < 1000)
        {
            bool success = ipc.GetRawInputReport(1, ref report, TimeSpan.FromMilliseconds(50));

            if (success && report.Buttons.Cross)
            {
                Console.WriteLine("Cross pressed");
            }

            executionCount++;
        }

        stopwatch.Stop();

        Console.WriteLine($"Read {executionCount} input reports in one second.");
    }
} while (Console.ReadKey(true).Key != ConsoleKey.Escape);