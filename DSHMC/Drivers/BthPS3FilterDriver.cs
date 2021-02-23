using System;
using System.Runtime.InteropServices;
using Nefarius.DsHidMini.Util;
using PInvoke;

namespace Nefarius.DsHidMini.Drivers
{
    public static class BthPS3FilterDriver
    {
        private const uint IOCTL_BTHPS3PSM_ENABLE_PSM_PATCHING = 0x002AAC04;
        private const uint IOCTL_BTHPS3PSM_DISABLE_PSM_PATCHING = 0x002AAC08;
        private const uint IOCTL_BTHPS3PSM_GET_PSM_PATCHING = 0x002A6C0C;

        private static readonly string BTHPS3PSM_CONTROL_DEVICE_PATH = "\\\\.\\BthPS3PSMControl";

        private static string ErrorMessage =>
            "BthPS3 filter driver access failed. Is Bluetooth turned on? Are the drivers installed?";

        /// <summary>
        ///     True if filter driver is currently loaded and operational, false otherwise.
        /// </summary>
        public static bool IsFilterAvailable
        {
            get
            {
                if (!BluetoothHelper.IsBluetoothRadioAvailable)
                    return false;

                using (var handle = Kernel32.CreateFile(BTHPS3PSM_CONTROL_DEVICE_PATH,
                    Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                    Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                    IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                    Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                    | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                    | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH,
                    Kernel32.SafeObjectHandle.Null
                ))
                {
                    return !handle.IsInvalid;
                }
            }
        }

        /// <summary>
        ///     Gets or sets current filter patching state.
        /// </summary>
        public static bool IsFilterEnabled
        {
            get
            {
                if (!BluetoothHelper.IsBluetoothRadioAvailable)
                    return false;

                using (var handle = Kernel32.CreateFile(BTHPS3PSM_CONTROL_DEVICE_PATH,
                    Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                    Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                    IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                    Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                    | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                    | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH,
                    Kernel32.SafeObjectHandle.Null
                ))
                {
                    if (handle.IsInvalid)
                        throw new Exception(ErrorMessage);

                    var payloadBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<BTHPS3PSM_GET_PSM_PATCHING>());
                    var payload = new BTHPS3PSM_GET_PSM_PATCHING {DeviceIndex = 0};

                    try
                    {
                        Marshal.StructureToPtr(payload, payloadBuffer, false);

                        Kernel32.DeviceIoControl(
                            handle,
                            unchecked((int) IOCTL_BTHPS3PSM_GET_PSM_PATCHING),
                            payloadBuffer,
                            Marshal.SizeOf<BTHPS3PSM_GET_PSM_PATCHING>(),
                            payloadBuffer,
                            Marshal.SizeOf<BTHPS3PSM_GET_PSM_PATCHING>(),
                            out _,
                            IntPtr.Zero
                        );

                        payload = Marshal.PtrToStructure<BTHPS3PSM_GET_PSM_PATCHING>(payloadBuffer);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(payloadBuffer);
                    }

                    return payload.IsEnabled > 0;
                }
            }
            set
            {
                using (var handle = Kernel32.CreateFile(BTHPS3PSM_CONTROL_DEVICE_PATH,
                    Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
                    Kernel32.FileShare.FILE_SHARE_READ | Kernel32.FileShare.FILE_SHARE_WRITE,
                    IntPtr.Zero, Kernel32.CreationDisposition.OPEN_EXISTING,
                    Kernel32.CreateFileFlags.FILE_ATTRIBUTE_NORMAL
                    | Kernel32.CreateFileFlags.FILE_FLAG_NO_BUFFERING
                    | Kernel32.CreateFileFlags.FILE_FLAG_WRITE_THROUGH,
                    Kernel32.SafeObjectHandle.Null
                ))
                {
                    if (handle.IsInvalid)
                        throw new Exception(ErrorMessage);

                    var payloadEnableBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<BTHPS3PSM_ENABLE_PSM_PATCHING>());
                    var payloadEnable = new BTHPS3PSM_ENABLE_PSM_PATCHING {DeviceIndex = 0};
                    var payloadDisableBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<BTHPS3PSM_DISABLE_PSM_PATCHING>());
                    var payloadDisable = new BTHPS3PSM_DISABLE_PSM_PATCHING {DeviceIndex = 0};

                    try
                    {
                        Marshal.StructureToPtr(payloadEnable, payloadEnableBuffer, false);
                        Marshal.StructureToPtr(payloadDisable, payloadDisableBuffer, false);

                        if (value)
                            Kernel32.DeviceIoControl(
                                handle,
                                unchecked((int) IOCTL_BTHPS3PSM_ENABLE_PSM_PATCHING),
                                payloadEnableBuffer,
                                Marshal.SizeOf<BTHPS3PSM_ENABLE_PSM_PATCHING>(),
                                IntPtr.Zero,
                                0,
                                out _,
                                IntPtr.Zero
                            );
                        else
                            Kernel32.DeviceIoControl(
                                handle,
                                unchecked((int) IOCTL_BTHPS3PSM_DISABLE_PSM_PATCHING),
                                payloadDisableBuffer,
                                Marshal.SizeOf<BTHPS3PSM_DISABLE_PSM_PATCHING>(),
                                IntPtr.Zero,
                                0,
                                out _,
                                IntPtr.Zero
                            );
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(payloadEnableBuffer);
                        Marshal.FreeHGlobal(payloadDisableBuffer);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BTHPS3PSM_ENABLE_PSM_PATCHING
        {
            public uint DeviceIndex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BTHPS3PSM_DISABLE_PSM_PATCHING
        {
            public uint DeviceIndex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        private struct BTHPS3PSM_GET_PSM_PATCHING
        {
            public uint DeviceIndex;

            public readonly uint IsEnabled;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0xC8)]
            public readonly string SymbolicLinkName;
        }
    }
}
