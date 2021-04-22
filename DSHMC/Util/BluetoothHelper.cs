using System;
using System.Net.NetworkInformation;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using PInvoke;

namespace Nefarius.DsHidMini.Util
{
    public static class BluetoothHelper
    {
        private const uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000C;

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

        public static void DisconnectRemoteDevice(PhysicalAddress device)
        {
            var radioParams = new BLUETOOTH_FIND_RADIO_PARAMS();
            radioParams.Init();

            var findHandle = BluetoothFindFirstRadio(ref radioParams, out var radioHandle);

            if (findHandle == IntPtr.Zero) 
                return;

            var payloadSize = Marshal.SizeOf<ulong>();
            var payload = Marshal.AllocHGlobal(payloadSize);

            Marshal.WriteInt64(
                payload,
                (long) BitConverter.ToUInt64(device.GetAddressBytes(), 0)
            );

            try
            {
                using (var safeHandle = new Kernel32.SafeObjectHandle(radioHandle))
                {
                    Kernel32.DeviceIoControl(
                        safeHandle,
                        unchecked((int)IOCTL_BTH_DISCONNECT_DEVICE),
                        payload,
                        payloadSize,
                        IntPtr.Zero,
                        0,
                        out _,
                        IntPtr.Zero
                    );
                }
            }
            finally
            {
                Marshal.FreeHGlobal(payload);
            }

            BluetoothFindRadioClose(findHandle);
            CloseHandle(radioHandle);
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