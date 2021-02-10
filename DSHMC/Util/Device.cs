using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.Util
{
    public enum DeviceLocationFlags
    {
        /// <summary>
        ///     The function retrieves the device instance handle for the specified device only if the device is currently
        ///     configured in the device tree.
        /// </summary>
        Normal,

        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in
        ///     the device tree or the device is a nonpresent device that is not currently configured in the device tree.
        /// </summary>
        Phantom,

        /// <summary>
        ///     The function retrieves a device instance handle for the specified device if the device is currently configured in
        ///     the device tree or in the process of being removed from the device tree. If the device is in the process of being
        ///     removed, the function cancels the removal of the device.
        /// </summary>
        CancelRemove
    }

    /// <summary>
    ///     Describes an instance of a PNP device.
    /// </summary>
    public partial class Device
    {
        private readonly uint _instanceHandle;

        protected Device(string instanceId, DeviceLocationFlags flags)
        {
            InstanceId = instanceId;
            var iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_FLAG.CM_LOCATE_DEVNODE_NORMAL;

            switch (flags)
            {
                case DeviceLocationFlags.Normal:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_FLAG.CM_LOCATE_DEVNODE_NORMAL;
                    break;
                case DeviceLocationFlags.Phantom:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_FLAG.CM_LOCATE_DEVNODE_PHANTOM;
                    break;
                case DeviceLocationFlags.CancelRemove:
                    iFlags = SetupApiWrapper.CM_LOCATE_DEVNODE_FLAG.CM_LOCATE_DEVNODE_CANCELREMOVE;
                    break;
            }

            var ret = SetupApiWrapper.CM_Locate_DevNode(
                ref _instanceHandle,
                instanceId,
                iFlags
            );

            if (ret == SetupApiWrapper.ConfigManagerResult.NoSuchDevinst)
                throw new ArgumentException("The supplied instance wasn't found.", nameof(flags));

            if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            uint nBytes = 256;
            
            var ptrInstanceBuf = Marshal.AllocHGlobal((int) nBytes);

            try
            {
                ret = SetupApiWrapper.CM_Get_Device_ID(
                    _instanceHandle,
                    ptrInstanceBuf,
                    nBytes,
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                DeviceId = (Marshal.PtrToStringUni(ptrInstanceBuf) ?? string.Empty).ToUpper();
            }
            finally
            {
                if (ptrInstanceBuf != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptrInstanceBuf);
            }
        }

        /// <summary>
        ///     The instance ID of the device.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        ///     The device ID.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        ///     Attempts to restart this device.
        /// </summary>
        public void Restart()
        {
            var ret = SetupApiWrapper.CM_Query_And_Remove_SubTree(
                _instanceHandle,
                IntPtr.Zero, IntPtr.Zero, 0,
                SetupApiWrapper.CM_QUERY_AND_REMOVE_SUBTREE_FLAGS.CM_REMOVE_NO_RESTART
            );

            ret = SetupApiWrapper.CM_Setup_DevNode(
                _instanceHandle,
                SetupApiWrapper.CM_SETUP_DEVINST_FLAGS.CM_SETUP_DEVNODE_READY
            );
        }

        /// <summary>
        ///     Return device identified by instance ID.
        /// </summary>
        /// <param name="instanceId">The instance ID of the device.</param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInstanceId(string instanceId)
        {
            return GetDeviceByInstanceId(instanceId, DeviceLocationFlags.Normal);
        }

        /// <summary>
        ///     Return device identified by instance ID.
        /// </summary>
        /// <param name="instanceId">The instance ID of the device.</param>
        /// <param name="flags">
        ///     <see cref="DeviceLocationFlags" />
        /// </param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInstanceId(string instanceId, DeviceLocationFlags flags)
        {
            return new Device(instanceId, flags);
        }

        /// <summary>
        ///     Return device identified by instance ID/path (symbolic link).
        /// </summary>
        /// <param name="symbolicLink">The device interface path/ID/symbolic link name.</param>
        /// <param name="flags">
        ///     <see cref="DeviceLocationFlags" />
        /// </param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInterfaceId(string symbolicLink, DeviceLocationFlags flags)
        {
            var instanceId = GetInstanceIdFromInterfaceId(symbolicLink);

            return GetDeviceByInstanceId(instanceId, flags);
        }

        /// <summary>
        ///     Resolves Interface ID/Symbolic link/Device path to Instance ID.
        /// </summary>
        /// <param name="symbolicLink">The device interface path/ID/symbolic link name.</param>
        /// <returns>The Instance ID.</returns>
        public static string GetInstanceIdFromInterfaceId(string symbolicLink)
        {
            var property = DevicePropertyDevice.InstanceId.ToNativeType();

            var buffer = IntPtr.Zero;
            uint sizeRequired = 2048;

            try
            {
                buffer = Marshal.AllocHGlobal((int) sizeRequired);

                var ret = SetupApiWrapper.CM_Get_Device_Interface_Property(
                    symbolicLink,
                    ref property,
                    out _,
                    buffer,
                    ref sizeRequired,
                    0
                );

                if (ret != SetupApiWrapper.ConfigManagerResult.Success)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        ///     Return device identified by instance ID/path (symbolic link).
        /// </summary>
        /// <param name="symbolicLink">The device interface path/ID/symbolic link name.</param>
        /// <returns>A <see cref="Device" />.</returns>
        public static Device GetDeviceByInterfaceId(string symbolicLink)
        {
            return GetDeviceByInterfaceId(symbolicLink, DeviceLocationFlags.Normal);
        }
    }
}