// See https://aka.ms/new-console-template for more information

using System.Net.NetworkInformation;

using Nefarius.DsHidMini.IPC;

if (!DsHidMiniInterop.IsAvailable)
{
    Console.WriteLine("DsHidMini IPC not available, make sure that at least one controller is connected");
    return;
}

using var ipc = new DsHidMiniInterop();

//ipc.SendPing();
Console.WriteLine(ipc.SetHostAddress(1, PhysicalAddress.Parse("C0:13:37:DE:AD:BE")));
