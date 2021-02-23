using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Nefarius.DsHidMini.Util
{
    public static class BluetoothHelper
    {
        public static bool IsBluetoothRadioAvailable
        {
            get
            {
                var radioParams = new BLUETOOTH_FIND_RADIO_PARAMS();
                radioParams.Init();

                var findHandle = BluetoothFindFirstRadio(ref radioParams, out var radioHandle);

                if (findHandle == IntPtr.Zero) return false;

                BluetoothFindRadioClose(findHandle);
                CloseHandle(radioHandle);

                return true;
            }
        }

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern IntPtr
            BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, out IntPtr phRadio);

        [DllImport("BluetoothApis.dll", SetLastError = true)]
        private static extern bool BluetoothFindRadioClose(IntPtr hFind);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            public uint dwSize;

            public void Init()
            {
                dwSize = (uint) Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAMS));
            }
        }
    }
}