using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

namespace DsHidMiniControl.Util
{
    public class DeviceNotificationListener
    {
        #region Win32

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr hRecipient,
            IntPtr NotificationFilter,
            uint Flags);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterDeviceNotification(IntPtr Handle);

        private const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000; // Device event when a device or piece of media has been inserted and becomes available
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // Device event when a device or piece of media has been physically removed

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public uint dbch_size;
            public uint dbch_devicetype;
            public uint dbch_reserved;
        }

        private const uint DBT_DEVTYP_VOLUME = 0x00000002;
        private const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_VOLUME
        {
            public uint dbcv_size;
            public uint dbcv_devicetype;
            public uint dbcv_reserved;
            public uint dbcv_unitmask;
            public ushort dbcv_flags;
        }

        private const ushort DBTF_MEDIA = 0x0001; // Media in drive changed.
        private const ushort DBTF_NET = 0x0002; // Network drive is changed.

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public uint dbcc_size;
            public uint dbcc_devicetype;
            public uint dbcc_reserved;
            public Guid dbcc_classguid;

            // To get value from lParam of WM_DEVICECHANGE, this length must be longer than 1.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string dbcc_name;
        }

        // Identifier: GUID_DEVINTERFACE_USB_DEVICE
        // Class GUID: {A5DCBF10-6530-11D2-901F-00C04FB951ED}
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        #endregion


        #region Start/End

        private HwndSource _handleSource;
        private IntPtr _notificationHandle;

        public void StartListen(Window window)
        {
            _handleSource = PresentationSource.FromVisual(window) as HwndSource;
            if (_handleSource != null)
            {
                RegisterUsbDeviceNotification(_handleSource.Handle);
                _handleSource.AddHook(WndProc);
            }
        }

        public void EndListen()
        {
            if (_handleSource != null)
            {
                UnregisterUsbDeviceNotification();
                _handleSource.RemoveHook(WndProc);
            }
        }

        private void RegisterUsbDeviceNotification(IntPtr windowHandle)
        {
            var dbcc = new DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = (uint)Marshal.SizeOf(typeof(DEV_BROADCAST_DEVICEINTERFACE)),
                dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE,
            };

            var notificationFilter = Marshal.AllocHGlobal(Marshal.SizeOf(dbcc));
            Marshal.StructureToPtr(dbcc, notificationFilter, true);

            _notificationHandle = RegisterDeviceNotification(windowHandle, notificationFilter, DEVICE_NOTIFY_WINDOW_HANDLE);
            if (_notificationHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register USB device notifications.");
        }

        private void UnregisterUsbDeviceNotification()
        {
            if (_notificationHandle != IntPtr.Zero)
                UnregisterDeviceNotification(_notificationHandle);
        }

        #endregion


        private enum VolumeType : ushort
        {
            Other = 0,
            Media = DBTF_MEDIA,
            Net = DBTF_NET,
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch ((int)wParam)
                {
                    case DBT_DEVICEARRIVAL:
                        Debug.WriteLine("Device added.");
                        HandleMessage(lParam);
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        Debug.WriteLine("Device removed.");
                        HandleMessage(lParam);
                        break;
                }
            }

            return IntPtr.Zero;
        }

        private void HandleMessage(IntPtr lParam)
        {
            var hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_HDR));

            switch (hdr.dbch_devicetype)
            {
                case DeviceNotificationListener.DBT_DEVTYP_VOLUME:
                    var volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_VOLUME));

                    // Index 0 represents drive A, index 1 represents drive B, and so on.
                    var driveIndices = FindDriveIndices(volume.dbcv_unitmask).Take(26).ToArray();
                    var driveLetters = ConvertIndicesToLetters(driveIndices);

                    var volumeType = (VolumeType)Enum.ToObject(typeof(VolumeType), volume.dbcv_flags);

                    Debug.WriteLine("Volume changed. Drive:{0} Type:{1}", String.Join(",", driveLetters), volumeType);
                    break;

                case DeviceNotificationListener.DBT_DEVTYP_DEVICEINTERFACE:
                    var deviceInterface = (DEV_BROADCAST_DEVICEINTERFACE)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_DEVICEINTERFACE));

                    var vid = GetVid(deviceInterface.dbcc_name);
                    var pid = GetPid(deviceInterface.dbcc_name);

                    Debug.WriteLine("USB device changed. VID:{0} PID:{1}", vid, pid);
                    break;
            }
        }

        private static IEnumerable<int> FindDriveIndices(uint value)
        {
            return new BitArray(new[] { (int)value }) // Up to 31 drive indices can be accepted.
                .Cast<bool>()
                .Select((x, index) => x ? index : -1)
                .Where(x => x >= 0);
        }

        private static IEnumerable<char> ConvertIndicesToLetters(int[] indices)
        {
            return Enumerable.Range('A', 'Z' - 'A' + 1)
                .Select((x, index) => new { Letter = (char)x, Index = index })
                .Where(x => indices.Contains(x.Index))
                .Select(x => x.Letter);
        }

        private static readonly Regex _patternVid = new Regex("VID_[0-9A-Z]{4}", RegexOptions.Compiled);
        private static readonly Regex _patternPid = new Regex("PID_[0-9A-Z]{4}", RegexOptions.Compiled);

        private static string GetVid(string source)
        {
            var match = _patternVid.Match(source);
            return match.Success ? match.Value : String.Empty;
        }

        private static string GetPid(string source)
        {
            var match = _patternPid.Match(source);
            return match.Success ? match.Value : String.Empty;
        }
    }
}