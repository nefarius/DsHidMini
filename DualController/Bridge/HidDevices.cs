using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using DualController.Core;
using Microsoft.Win32.SafeHandles;

namespace DualController.Bridge;

internal sealed record HidDevice(string Path, ushort Product, bool Bluetooth,
    ushort InputLength, ushort OutputLength, ushort FeatureLength, string Serial);

internal static class HidDevices
{
    private const uint PresentInterfaces = 0x12;
    private const uint ReadWrite = 0xC0000000, ShareReadWrite = 3, OpenExisting = 3;
    private const uint Overlapped = 0x40000000;

    public static void ValidateLayouts()
    {
        if (Marshal.SizeOf<Attributes>() != 12 || Marshal.SizeOf<Caps>() != 64
            || Marshal.SizeOf<DeviceInfo>() != (IntPtr.Size == 8 ? 32 : 28)
            || Marshal.SizeOf<InterfaceData>() != (IntPtr.Size == 8 ? 32 : 28))
            throw new InvalidOperationException("Windows HID interop structure size mismatch.");
    }

    public static SafeFileHandle Open(string path, bool readWrite = true) =>
        CreateFile(path, readWrite ? ReadWrite : 0, ShareReadWrite,
            IntPtr.Zero, OpenExisting, readWrite ? Overlapped : 0, IntPtr.Zero);

    public static List<HidDevice> Enumerate()
    {
        HidD_GetHidGuid(out Guid guid);
        IntPtr set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, PresentInterfaces);
        if (set == new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastWin32Error());
        var result = new List<HidDevice>();
        try
        {
            for (uint index = 0; ; index++)
            {
                var data = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref data))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 259) break;
                    throw new Win32Exception(error);
                }
                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0,
                    out uint required, IntPtr.Zero);
                if (required < 8 || required > 65536) continue;
                IntPtr buffer = Marshal.AllocHGlobal((int)required);
                IntPtr infoBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<DeviceInfo>());
                try
                {
                    Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
                    var info = new DeviceInfo { Size = Marshal.SizeOf<DeviceInfo>() };
                    Marshal.StructureToPtr(info, infoBuffer, false);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, buffer, required,
                        out _, infoBuffer)) continue;
                    info = Marshal.PtrToStructure<DeviceInfo>(infoBuffer);
                    string? path = Marshal.PtrToStringUni(buffer + 4);
                    if (path is null) continue;
                    using SafeFileHandle handle = Open(path, false);
                    if (handle.IsInvalid) continue;
                    var attrs = new Attributes { Size = Marshal.SizeOf<Attributes>() };
                    if (!HidD_GetAttributes(handle, ref attrs)) continue;
                    if (!DeviceOrigin.IsPhysicalDs4(Ancestors(info.DevInst),
                        attrs.Vendor, attrs.Product, out bool bt)) continue;
                    if (!HidD_GetPreparsedData(handle, out IntPtr preparsed)) continue;
                    try
                    {
                        if (HidP_GetCaps(preparsed, out Caps caps) != 0x00110000) continue;
                        if (caps.UsagePage != 1 || caps.Usage != 5) continue;
                        if (caps.InputLength is < 10 or > 1024) continue;
                        byte[] serialBuffer = new byte[256];
                        string serial = HidD_GetSerialNumberString(handle, serialBuffer, serialBuffer.Length)
                            ? Encoding.Unicode.GetString(serialBuffer).TrimEnd('\0').Trim() : "";
                        result.Add(new(path, attrs.Product, bt, caps.InputLength,
                            caps.OutputLength, caps.FeatureLength, serial));
                    }
                    finally { HidD_FreePreparsedData(preparsed); }
                }
                finally { Marshal.FreeHGlobal(buffer); Marshal.FreeHGlobal(infoBuffer); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        // Prefer wired if Windows exposes the same MAC via USB and Bluetooth.
        // Never merge distinct controllers whose serial number is unavailable.
        return result.OrderBy(d => d.Bluetooth).GroupBy(d =>
            string.IsNullOrWhiteSpace(d.Serial) ? d.Path : d.Serial.Replace(":", "").Replace("-", ""),
            StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
    }

    private static IEnumerable<string> Ancestors(uint devInst)
    {
        for (int level = 0; level < 32; level++)
        {
            var id = new StringBuilder(512);
            if (CM_Get_Device_ID(devInst, id, id.Capacity, 0) == 0) yield return id.ToString();
            else yield break; // Do not trust incomplete ancestry.
            if (CM_Get_Parent(out uint parent, devInst, 0) != 0) yield break;
            devInst = parent;
        }
    }

    public static bool EnableBluetoothReports(SafeFileHandle handle, ushort featureLength)
    {
        // Calibration feature 0x05 switches native DS4 Bluetooth into the
        // extended 0x11 input mode. Motion calibration itself is not consumed.
        byte[] feature = new byte[Math.Max(41, (int)featureLength)];
        feature[0] = 5;
        return HidD_GetFeature(handle, feature, feature.Length);
    }

    public static bool WriteOutput(SafeFileHandle handle, byte[] report) =>
        HidD_SetOutputReport(handle, report, report.Length);

    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceData { public int Size; public Guid ClassGuid; public uint Flags; public UIntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInfo { public int Size; public Guid ClassGuid; public uint DevInst; public UIntPtr Reserved; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Attributes { public int Size; public ushort Vendor, Product, Version; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Caps
    {
        public ushort Usage, UsagePage, InputLength, OutputLength, FeatureLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)] public ushort[] Reserved;
        public ushort LinkNodes, InputButtons, InputValues, InputIndices,
            OutputButtons, OutputValues, OutputIndices, FeatureButtons, FeatureValues, FeatureIndices;
    }

    [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetAttributes(SafeFileHandle handle, ref Attributes attrs);
    [DllImport("hid.dll")][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
    [DllImport("hid.dll")][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, out Caps caps);
    [DllImport("hid.dll")][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetSerialNumberString(SafeFileHandle handle, [Out] byte[] buffer, int length);
    [DllImport("hid.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_GetFeature(SafeFileHandle handle, [In, Out] byte[] report, int length);
    [DllImport("hid.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_SetOutputReport(SafeFileHandle handle, byte[] report, int length);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share,
        IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string? enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info,
        ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data,
        IntPtr detail, uint size, out uint required, IntPtr info);
    [DllImport("setupapi.dll")]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_Device_ID(uint devInst, StringBuilder id, int length, uint flags);
    [DllImport("cfgmgr32.dll")]
    private static extern uint CM_Get_Parent(out uint parent, uint devInst, uint flags);
}
