// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

using Nefarius.DsHidMini.IPC;
#if INPUT_TEST
using Nefarius.DsHidMini.IPC.Models.Public;
#endif
#if RUNTIME_OUTPUT_TEST
using Nefarius.DsHidMini.IPC.Models.Public;
#endif

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using DsHidMiniInterop ipc = new();

#if INPUT_TEST
DS3_RAW_INPUT_REPORT report = new();
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
#elif RUNTIME_OUTPUT_TEST
            // Volatile runtime output (issue #379). Rebuild without this define
            // for the default ping throughput loop.
            uint playerStatus = ipc.SetPlayerIndex(1, 1);
            uint rumbleStatus = ipc.SetRumble(1, 0x40, 0x00);
            uint altStatus = ipc.SetAlternateRumbleMode(1, false);
            uint ledStatus = Ds3LedPattern.TryFromPlayerIndex(1, out Ds3LedPattern pattern)
                ? ipc.SetLedPattern(1, pattern)
                : 0xC000000D;
            SetHostResult pairResult = ipc.PairToCurrentHost(1);
            Console.WriteLine(
                $"SetPlayerIndex=0x{playerStatus:X} SetRumble=0x{rumbleStatus:X} SetAlternateRumbleMode=0x{altStatus:X} SetLedPattern=0x{ledStatus:X} PairToCurrentHost={pairResult} flags={Ds3PlayerLeds.TryGetFlags(1, out byte flags) && flags == Ds3PlayerLeds.Led1}");
            break;
#else
            ipc.SendPing();
#endif

            executionCount++;
        }

        stopwatch.Stop();

#if INPUT_TEST
        Console.WriteLine($"Read {executionCount} input reports in one second.");
#elif RUNTIME_OUTPUT_TEST
        Console.WriteLine("Sent one volatile LED/rumble/alternate-mode/pairing command set.");
#else
        Console.WriteLine($"Executed {executionCount} PINGs in one second.");
#endif
    }
} while (Console.ReadKey(true).Key != ConsoleKey.Escape);
