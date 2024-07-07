using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.Setup.Util;

public static class ArchitectureInfo
{
    public static bool IsArm64
    {
        get
        {
            IntPtr handle = Process.GetCurrentProcess().Handle;
            IsWow64Process2(handle, out ushort processMachine, out ushort nativeMachine);

            return nativeMachine == 0xaa64;
        }
    }

    public static string PlatformShortName =>
        IsArm64
            ? "arm64"
            : RuntimeInformation.OSArchitecture.ToString();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(
        IntPtr process,
        out ushort processMachine,
        out ushort nativeMachine
    );
}