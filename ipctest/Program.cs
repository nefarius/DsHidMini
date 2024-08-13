// See https://aka.ms/new-console-template for more information

using Nefarius.DsHidMini.IPC;

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using var ipc = new DsHidMiniInterop();

ipc.SendPing();

