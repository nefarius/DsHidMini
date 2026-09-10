using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

using Microsoft.Win32.SafeHandles;

using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

/// <summary>
///     Resolves the Windows XInput user index (0-3) for a DsHidMini device in XInput HID mode.
///     USB typically exposes an XUSB interface that can be matched by PnP container ID (same LED IOCTL as
///     XInputBridge). Bluetooth XInput HID devices often have no XUSB interface and only the machine-wide
///     container ID, so resolution also walks HID children and, when unique, the occupied XInput slots.
/// </summary>
internal static class XInputSlotResolver
{
    private const byte InvalidXInputUserId = 0xFF;

    /// <summary>
    ///     Windows reports this container for devices that are not part of a unique physical device group
    ///     (typical for BTHPS3 Bluetooth stacks). It must not be used to pair siblings.
    /// </summary>
    private static readonly Guid LocalMachineContainerId = Guid.Parse("00000000-0000-0000-FFFF-FFFFFFFFFFFF");

    /// <summary>
    ///     Short-lived negative cache to avoid hammering PnP/XUSB on repeated misses without permanently
    ///     blocking resolution after transient enumeration or IOCTL races.
    /// </summary>
    private static readonly TimeSpan NegativeResolutionCacheTtl = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Win32 IOCTL to read Xbox 360 controller LED / ring-of-light state (<c>IOCTL_XUSB_GET_LED_STATE</c>).
    /// </summary>
    // Same IOCTL (0x8000E008) as XInputBridge GlobalState::SymlinkToUserIndex.
    private const uint IoctlXusbGetLedState = 0x8000E008;

    private static readonly ConcurrentDictionary<string, byte> ResolutionCacheByInstanceId =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<string, DateTime> NegativeResolutionExpiryByInstanceId =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Cache-generation token incremented when caches are invalidated to prevent stale writes after a clear.
    /// </summary>
    private static long _cacheGeneration;

    /// <summary>
    ///     XUSB device interface class GUID (see XInputBridge/Macros.h).
    /// </summary>
    internal static readonly Guid XusbDeviceInterfaceGuid =
        Guid.Parse("{EC87F1E3-C13B-4100-B5F7-8B84D54260CB}");

    private static readonly Guid HidDeviceInterfaceGuid =
        Guid.Parse("{4D1E55B2-F16F-11CF-88CB-001111000030}");

    private static SetupApiWrapper.DevPropKey ToDevPropKey(DevicePropertyKey key) =>
        new(key.CategoryGuid, key.PropertyIdentifier);

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
    ///     Clears cached XInput user-index lookups when device topology may have changed.
    /// </summary>
    public static void InvalidateResolutionCache()
    {
        Interlocked.Increment(ref _cacheGeneration);
        ResolutionCacheByInstanceId.Clear();
        NegativeResolutionExpiryByInstanceId.Clear();
    }

    /// <summary>
    ///     Returns the XInput user index (0-3) for this DsHidMini device, or false if it cannot be determined.
    ///     Call only when the device is in XInput HID mode.
    /// </summary>
    /// <param name="dshmDevice">The DsHidMini PnP device to resolve.</param>
    /// <param name="userIndex">Receives the XInput user index (0-3) on success.</param>
    /// <param name="ignoreNegativeCache">
    ///     When true, skip the short-lived miss cache so a scheduled retry can re-query PnP/XUSB.
    ///     Successful slot entries and cache-generation guards stay in effect.
    /// </param>
    internal static bool TryGetXInputUserIndex(PnPDevice dshmDevice, out byte userIndex,
        bool ignoreNegativeCache = false)
    {
        userIndex = InvalidXInputUserId;
        string instanceId = dshmDevice.InstanceId;

        if (ResolutionCacheByInstanceId.TryGetValue(instanceId, out byte cached))
        {
            userIndex = cached;
            return true;
        }

        if (!ignoreNegativeCache
            && NegativeResolutionExpiryByInstanceId.TryGetValue(instanceId, out DateTime negUntil)
            && DateTime.UtcNow < negUntil)
        {
            return false;
        }

        // Capture generation before resolving to prevent stale writes after InvalidateResolutionCache
        long generationSnapshot = Interlocked.Read(ref _cacheGeneration);

        if (TryResolveViaXusbContainer(dshmDevice, out userIndex)
            || TryResolveViaXinputHidChild(dshmDevice, out userIndex)
            || TryResolveViaUniqueOccupiedXinputSlot(dshmDevice, out userIndex))
        {
            if (Interlocked.Read(ref _cacheGeneration) == generationSnapshot)
            {
                NegativeResolutionExpiryByInstanceId.TryRemove(instanceId, out _);
                ResolutionCacheByInstanceId[instanceId] = userIndex;
            }

            return true;
        }

        if (Interlocked.Read(ref _cacheGeneration) == generationSnapshot)
        {
            NegativeResolutionExpiryByInstanceId[instanceId] = DateTime.UtcNow.Add(NegativeResolutionCacheTtl);
        }

        userIndex = InvalidXInputUserId;
        return false;
    }

    private static bool TryResolveViaXusbContainer(PnPDevice dshmDevice, out byte userIndex)
    {
        userIndex = InvalidXInputUserId;
        if (!TryGetBaseContainerId(dshmDevice.InstanceId, out Guid dshmContainer)
            || !IsUniqueContainerId(dshmContainer))
        {
            return false;
        }

        foreach (string xusbPath in EnumeratePresentDeviceInterfacePaths(XusbDeviceInterfaceGuid))
        {
            if (!TryGetDeviceInstanceIdFromInterfacePath(xusbPath, out string? xusbInstanceId))
            {
                continue;
            }

            if (!TryGetBaseContainerId(xusbInstanceId, out Guid xusbContainer)
                || xusbContainer != dshmContainer)
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

    private static bool TryResolveViaXinputHidChild(PnPDevice dshmDevice, out byte userIndex)
    {
        userIndex = InvalidXInputUserId;
        foreach (string hidPath in EnumeratePresentDeviceInterfacePaths(HidDeviceInterfaceGuid))
        {
            if (!TryGetDeviceInstanceIdFromInterfacePath(hidPath, out string? hidInstanceId)
                || !IsXInputHidInstanceId(hidInstanceId)
                || !TryGetParentInstanceId(hidInstanceId, out string? parentId)
                || !InstanceIdsEqual(parentId, dshmDevice.InstanceId))
            {
                continue;
            }

            if (TrySymlinkToUserIndex(hidPath, out byte idx) && idx != InvalidXInputUserId)
            {
                userIndex = idx;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveViaUniqueOccupiedXinputSlot(PnPDevice dshmDevice, out byte userIndex)
    {
        userIndex = InvalidXInputUserId;
        if (!HasXInputHidChild(dshmDevice))
        {
            return false;
        }

        int occupiedCount = 0;
        byte occupiedIndex = InvalidXInputUserId;
        for (byte i = 0; i < 4; i++)
        {
            if (!TryIsXinputSlotOccupied(i))
            {
                continue;
            }

            occupiedCount++;
            occupiedIndex = i;
        }

        if (occupiedCount != 1)
        {
            return false;
        }

        userIndex = occupiedIndex;
        return true;
    }

    private static bool HasXInputHidChild(PnPDevice dshmDevice)
    {
        foreach (string hidPath in EnumeratePresentDeviceInterfacePaths(HidDeviceInterfaceGuid))
        {
            if (TryGetDeviceInstanceIdFromInterfacePath(hidPath, out string? hidInstanceId)
                && IsXInputHidInstanceId(hidInstanceId)
                && TryGetParentInstanceId(hidInstanceId, out string? parentId)
                && InstanceIdsEqual(parentId, dshmDevice.InstanceId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUniqueContainerId(Guid containerId) =>
        containerId != Guid.Empty && containerId != LocalMachineContainerId;

    private static bool IsXInputHidInstanceId(string instanceId) =>
        instanceId.Contains("VID_045E", StringComparison.OrdinalIgnoreCase)
        && instanceId.Contains("PID_02FF", StringComparison.OrdinalIgnoreCase);

    private static bool InstanceIdsEqual(string? left, string? right) =>
        !string.IsNullOrEmpty(left)
        && !string.IsNullOrEmpty(right)
        && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetParentInstanceId(string instanceId, out string? parentId)
    {
        parentId = null;
        try
        {
            PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId);
            parentId = device.GetProperty<string>(DevicePropertyKey.Device_Parent);
            return !string.IsNullOrEmpty(parentId);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryIsXinputSlotOccupied(byte userIndex)
    {
        try
        {
            return XInputGetState(userIndex, out _) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XinputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XinputState
    {
        public uint PacketNumber;
        public XinputGamepad Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint userIndex, out XinputState state);

    /// <summary>
    ///     Yields symbolic link paths for all present device interfaces of the given class.
    /// </summary>
    private static IEnumerable<string> EnumeratePresentDeviceInterfacePaths(Guid interfaceClassGuid)
    {
        uint lenChars = 0;
        Guid g = interfaceClassGuid;
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

        long byteCountLong = (long)lenChars * sizeof(char);
        if (byteCountLong > int.MaxValue)
        {
            yield break;
        }

        int byteCount = (int)byteCountLong;
        IntPtr buffer = Marshal.AllocHGlobal(byteCount);
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

            foreach (string path in ParseDoubleNullTerminatedUnicode(buffer, byteCount))
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

    /// <summary>
    ///     Parses a CONFIGMG multi-string buffer (double-null-terminated UTF-16) into individual strings.
    /// </summary>
    /// <param name="buffer">Pointer to the buffer returned by <c>CM_Get_Device_Interface_ListW</c>.</param>
    /// <param name="byteLength">Size of the buffer in bytes.</param>
    /// <returns>Each non-empty string segment in order until a zero-length segment ends the list.</returns>
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
            offset += sizeof(char) * (s.Length + 1);
        }
    }

    private static bool TryGetDeviceInstanceIdFromInterfacePath(string deviceInterfacePath,
        [NotNullWhen(true)] out string? instanceId)
    {
        instanceId = null;
        SetupApiWrapper.DevPropKey key = ToDevPropKey(DevicePropertyKey.Device_InstanceId);
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

        SetupApiWrapper.DevPropKey key = ToDevPropKey(DevicePropertyKey.Device_BaseContainerId);
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
        using SafeFileHandle handle = PInvoke.CreateFile(
            symlink,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL
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