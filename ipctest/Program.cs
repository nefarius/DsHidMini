// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Nefarius.DsHidMini.IPC;
#if INPUT_TEST
using Nefarius.DsHidMini.IPC.Models.Public;
#endif

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using DsHidMiniInterop ipc = new();

#if INPUT_TEST
Ds3RawInputReport report = new();
#endif

Stopwatch stopwatch = new();

do
{
    while (!Console.KeyAvailable)
    {
        int executionCount = 0;

        stopwatch.Restart();

        while (stopwatch.ElapsedMilliseconds < 1000)
        {
#if INPUT_TEST
            bool success = ipc.GetRawInputReport(1, ref report, TimeSpan.FromMilliseconds(50));

            if (success && report.Buttons.Cross)
            {
                Console.WriteLine("Cross pressed");
            }
#else
            ipc.SendPing();
#endif

            executionCount++;
        }

        stopwatch.Stop();

#if INPUT_TEST
        Console.WriteLine($"Read {executionCount} input reports in one second.");
#else
        Console.WriteLine($"Executed {executionCount} PINGs in one second.");
#endif
    }
} while (Console.ReadKey(true).Key != ConsoleKey.Escape);