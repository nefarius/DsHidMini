// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net.NetworkInformation;

using Nefarius.DsHidMini.IPC;

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using DsHidMiniInterop ipc = new();

// Create a Stopwatch instance to track time
Stopwatch stopwatch = new();

// Counter for the number of method executions
int executionCount = 0;

// Start the stopwatch
stopwatch.Start();

// Loop until the elapsed time is 1 second (1000 milliseconds)
while (stopwatch.ElapsedMilliseconds < 1000)
{
    // Call the method you want to execute
    ipc.SendPing();

    // Increment the counter
    executionCount++;
}

// Stop the stopwatch
stopwatch.Stop();

// Output the number of executions in one second
Console.WriteLine($"Executed {executionCount} PINGs in one second.");

Console.WriteLine(ipc.SetHostAddress(1, PhysicalAddress.Parse("C0:13:37:DE:AD:BE")));
