using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Nefarius.Utilities.DeviceManagement.PnP;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

using Microsoft.Win32.SafeHandles;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

/// <summary>
///     Resolves the Windows XInput user index (0-3) for a DsHidMini device in XInput HID mode by matching the
///     controller's XUSB interface (via PnP base container ID) and issuing the same LED IOCTL as XInputBridge
///     GlobalState::SymlinkToUserIndex.
/// </summary>
internal static class XInputSlotResolver
{
    private const byte InvalidXInputUserId = 0xFF;

    /// <summary>
    ///     XUSB device interface class GUID (see XInputBridge/Macros.h).
    /// </summary>
    private static readonly Guid XusbInterfaceClassGuid =
        Guid.Parse("{EC87F1E3-C13B-4100-B5F7-8B84D54260CB}");

    /// <summary>
    ///     DEVPKEY_Device_InstanceId - used with CM_Get_Device_Interface_Property.
    /// </summary>
    private static readonly SetupApiWrapper.DevPropKey DevpKeyDeviceInstanceId = new(
        0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x94, 0x39, 0x5d, 256);

    /// <summary>
    ///     DEVPKEY_Device_BaseContainerId - used with CM_Get_DevNode_Property.
    /// </summary>
    private static readonly SetupApiWrapper.DevPropKey DevpKeyDeviceBaseContainerId = new(
        0x80497100, 0x8c73, 0x48ea, 0x87, 0x08, 0x33, 0xa1, 0x6b, 0x18, 0x77, 0xfa, 2);

    private const uint IoctlXusbGetLedState = 0x8000E008;

    /// <summary>
    ///     Mirrors XINPUT_LED_TO_PORT_MAP in XInputBridge/GlobalState.cpp.
    /// </summary>
    private static readonly byte[] XinputLedToPortMap =
    [
        InvalidXInputUserId,
        InvalidXInputUserId,
        0,
        1,
        2,
        3,
        0,
        1,
        2,
        3,
        InvalidXInputUserId,
        InvalidXInputUserId,
        InvalidXInputUserId,
        InvalidXInputUserId,
        InvalidXInputUserId,
        InvalidXInputUserId,
        InvalidXInputUserId
    ];

    /// <summary>
    ///     Returns the XInput user index (0-3) for this DsHidMini device, or false if it cannot be determined.
    ///     Call only when the device is in XInput HID mode.
    /// </summary>
    internal static bool TryGetXInputUserIndex(PnPDevice dshmDevice, out byte userIndex)
    {
        userIndex = InvalidXInputUserId;
        if (!TryGetBaseContainerId(dshmDevice.InstanceId, out Guid dshmContainer))
        {
            return false;
        }

        foreach (string xusbPath in EnumeratePresentXusbDeviceInterfacePaths())
        {
            if (!TryGetDeviceInstanceIdFromInterfacePath(xusbPath, out string? xusbInstanceId))
            {
                continue;
            }

            if (!TryGetBaseContainerId(xusbInstanceId, out Guid xusbContainer) || xusbContainer != dshmContainer)
            {
                continue;
            }

            if (TrySymlinkToUserIndex(xusbPath, out byte idx) && idx != InvalidXInputUserId)
            {
                userIndex = idx;
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumeratePresentXusbDeviceInterfacePaths()
    {
        uint lenChars = 0;
        Guid g = XusbInterfaceClassGuid;
        SetupApiWrapper.ConfigManagerResult r = SetupApiWrapper.CM_Get_Device_Interface_List_SizeW(
            ref lenChars,
            ref g,
            null,
            SetupApiWrapper.CM_GET_DEVICE_INTERFACE_LIST_PRESENT
        );

        if (r != SetupApiWrapper.ConfigManagerResult.Success || lenChars <= 1)
        {
            yield break;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)(lenChars * 2));
        try
        {
            r = SetupApiWrapper.CM_Get_Device_Interface_ListW(
                ref g,
                null,
                buffer,
                lenChars,
                SetupApiWrapper.CM_GET_DEVICE_INTERFACE_LIST_PRESENT
            );

            if (r != SetupApiWrapper.ConfigManagerResult.Success)
            {
                yield break;
            }

            int byteLen = (int)(lenChars * 2);
            foreach (string path in ParseDoubleNullTerminatedUnicode(buffer, byteLen))
            {
                if (!string.IsNullOrEmpty(path))
                {
                    yield return path;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IEnumerable<string> ParseDoubleNullTerminatedUnicode(IntPtr buffer, int byteLength)
    {
        int offset = 0;
        while (offset < byteLength)
        {
            string? s = Marshal.PtrToStringUni(IntPtr.Add(buffer, offset));
            if (string.IsNullOrEmpty(s))
            {
                yield break;
            }

            yield return s;
            offset += 2 * (s.Length + 1);
        }
    }

    private static bool TryGetDeviceInstanceIdFromInterfacePath(string deviceInterfacePath,
        [NotNullWhen(true)] out string? instanceId)
    {
        instanceId = null;
        SetupApiWrapper.DevPropKey key = DevpKeyDeviceInstanceId;
        uint bufferSize = 0;
        SetupApiWrapper.ConfigManagerResult r = SetupApiWrapper.CM_Get_Device_Interface_Property(
            deviceInterfacePath,
            ref key,
            out SetupApiWrapper.DevPropType _,
            IntPtr.Zero,
            ref bufferSize,
            0
        );

        if (r != SetupApiWrapper.ConfigManagerResult.BufferSmall)
        {
            return false;
        }

        IntPtr buf = Marshal.AllocHGlobal((int)bufferSize);
        try
        {
            r = SetupApiWrapper.CM_Get_Device_Interface_Property(
                deviceInterfacePath,
                ref key,
                out _,
                buf,
                ref bufferSize,
                0
            );

            if (r != SetupApiWrapper.ConfigManagerResult.Success)
            {
                return false;
            }

            instanceId = Marshal.PtrToStringUni(buf);
            return !string.IsNullOrEmpty(instanceId);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    private static bool TryGetBaseContainerId(string deviceInstanceId, out Guid containerId)
    {
        containerId = default;
        uint devInst = 0;
        if (SetupApiWrapper.CM_Locate_DevNode(
                ref devInst,
                deviceInstanceId,
                SetupApiWrapper.CM_LOCATE_DEVNODE_FLAG.CM_LOCATE_DEVNODE_NORMAL
            ) != SetupApiWrapper.ConfigManagerResult.Success)
        {
            return false;
        }

        SetupApiWrapper.DevPropKey key = DevpKeyDeviceBaseContainerId;
        uint size = 0;
        SetupApiWrapper.ConfigManagerResult r = SetupApiWrapper.CM_Get_DevNode_Property(
            devInst,
            ref key,
            out SetupApiWrapper.DevPropType propType,
            IntPtr.Zero,
            ref size,
            0
        );

        if (r != SetupApiWrapper.ConfigManagerResult.BufferSmall)
        {
            return false;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            r = SetupApiWrapper.CM_Get_DevNode_Property(
                devInst,
                ref key,
                out propType,
                buffer,
                ref size,
                0
            );

            if (r != SetupApiWrapper.ConfigManagerResult.Success || propType != SetupApiWrapper.DevPropType.Guid)
            {
                return false;
            }

            byte[] guidBytes = new byte[16];
            Marshal.Copy(buffer, guidBytes, 0, 16);
            containerId = new Guid(guidBytes);
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    ///     Port of GlobalState::SymlinkToUserIndex.
    /// </summary>
    private static unsafe bool TrySymlinkToUserIndex(string symlink, out byte userIndex)
    {
        userIndex = InvalidXInputUserId;
        using SafeFileHandle? handle = PInvoke.CreateFile(
            symlink,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            null
        );

        if (handle.IsInvalid)
        {
            return false;
        }

        byte[] gamepadStateRequest0101 = { 0x01, 0x01, 0x00 };
        byte[] ledStateData = new byte[3];

        BOOL ok;
        fixed (byte* inPtr = gamepadStateRequest0101)
        fixed (byte* outPtr = ledStateData)
        {
            Span<byte> inSpan = new(inPtr, 3);
            Span<byte> outSpan = new(outPtr, 3);
            ok = PInvoke.DeviceIoControl(
                handle,
                IoctlXusbGetLedState,
                inSpan,
                outSpan,
                out _,
                null
            );
        }

        if (!ok)
        {
            return false;
        }

        byte ledState = ledStateData[2];
        byte[] map = XinputLedToPortMap;
        if (ledState >= map.Length)
        {
            return false;
        }

        byte mapped = map[ledState];
        if (mapped == InvalidXInputUserId)
        {
            return false;
        }

        userIndex = mapped;
        return true;
    }
}
