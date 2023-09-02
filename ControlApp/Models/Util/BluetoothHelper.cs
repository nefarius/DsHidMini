using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Bluetooth;

namespace Nefarius.DsHidMini.ControlApp.Models.Util
{
    public static class BluetoothHelper
    {
        private const uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000C;

        public static bool IsBluetoothRadioAvailable
        {
            get
            {
                BLUETOOTH_FIND_RADIO_PARAMS radioParams;
                radioParams.dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>();

                var findHandle = PInvoke.BluetoothFindFirstRadio(radioParams, out var radioHandle);

                if (findHandle == 0) return false;

                PInvoke.BluetoothFindRadioClose(findHandle);
                radioHandle.Dispose();

                return true;
            }
        }

        /// <summary>
        ///     Instruct host radio to disconnect a given remote device.
        /// </summary>
        /// <param name="device">The MAC address of the remote device.</param>
        public static unsafe void DisconnectRemoteDevice(PhysicalAddress device)
        {
            BLUETOOTH_FIND_RADIO_PARAMS radioParams;
            radioParams.dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>();

            var findHandle = PInvoke.BluetoothFindFirstRadio(radioParams, out var radioHandle);

            if (findHandle == 0)
                return;

            var payloadSize = Marshal.SizeOf<ulong>();
            var payload = Marshal.AllocHGlobal(payloadSize);
            var raw = new byte[] { 0x00, 0x00 }.Concat(device.GetAddressBytes()).Reverse().ToArray();
            var value = (long)BitConverter.ToUInt64(raw, 0);

            Marshal.WriteInt64(payload, value);

            try
            {
                PInvoke.DeviceIoControl(
                    radioHandle,
                    IOCTL_BTH_DISCONNECT_DEVICE,
                    payload.ToPointer(),
                    (uint)payloadSize,
                    null,
                    0,
                    null,
                    null
                );
            }
            finally
            {
                Marshal.FreeHGlobal(payload);
                PInvoke.BluetoothFindRadioClose(findHandle);
                radioHandle.Dispose();
            }
        }
    }
}