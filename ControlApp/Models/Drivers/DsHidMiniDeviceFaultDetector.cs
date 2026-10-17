using System.Runtime.InteropServices;

using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     Enumerates present HID-class devnodes. A failed DsHidMini start still leaves the devnode
///     here after its device interface is gone, which is the Code 10 case the normal device list misses.
/// </summary>
internal static class DsHidMiniDeviceFaultDetector
{
    /// <summary>
    ///     GUID_DEVCLASS_HIDCLASS from the DsHidMini INF.
    /// </summary>
    internal static readonly Guid HidClassGuid = new("745a17a0-74d3-11d0-b6fe-00a0c90f57da");

    public static IReadOnlyList<DsHidMiniDevNodeSnapshot> EnumeratePresent()
    {
        const uint flags = SetupApiWrapper.CM_GETIDLIST_FILTER_CLASS |
                           SetupApiWrapper.CM_GETIDLIST_FILTER_PRESENT;
        string classId = HidClassGuid.ToString("B");
        uint length = 0;
        if (SetupApiWrapper.CM_Get_Device_ID_List_Size(ref length, classId, flags) !=
            SetupApiWrapper.ConfigManagerResult.Success || length <= 1)
        {
            return [];
        }

        int byteCount = checked((int)length * sizeof(char));
        IntPtr buffer = Marshal.AllocHGlobal(byteCount);
        try
        {
            if (SetupApiWrapper.CM_Get_Device_ID_List(classId, buffer, length, flags) !=
                SetupApiWrapper.ConfigManagerResult.Success)
            {
                return [];
            }

            List<DsHidMiniDevNodeSnapshot> nodes = [];
            foreach (string instanceId in ParseDoubleNullTerminatedUnicode(buffer, byteCount))
            {
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    continue;
                }

                try
                {
                    PnPDevice device = PnPDevice.GetDeviceByInstanceId(instanceId);
                    nodes.Add(Read(device, instanceId));
                }
                catch (Exception ex)
                {
                    Log.Logger.Debug(ex,
                        "Skipping HID devnode {InstanceId} while scanning for failed DsHidMini devices.",
                        instanceId);
                }
            }

            return nodes;
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
            string? value = Marshal.PtrToStringUni(IntPtr.Add(buffer, offset));
            if (string.IsNullOrEmpty(value))
            {
                yield break;
            }

            yield return value;
            offset += sizeof(char) * (value.Length + 1);
        }
    }

    private static DsHidMiniDevNodeSnapshot Read(PnPDevice device, string instanceId)
    {
        return new DsHidMiniDevNodeSnapshot(
            instanceId,
            TryReadString(device, DevicePropertyKey.NAME),
            TryReadString(device, DevicePropertyKey.Device_DriverInfSection),
            TryReadUInt32(device, DevicePropertyKey.Device_ProblemCode),
            TryReadUInt32(device, DevicePropertyKey.Device_ProblemStatus));
    }

    private static string? TryReadString(PnPDevice device, DevicePropertyKey key)
    {
        try
        {
            string? value = device.GetProperty<string>(key);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Device property {Property} was unavailable for {InstanceId}.", key, device.InstanceId);
            return null;
        }
    }

    private static uint? TryReadUInt32(PnPDevice device, DevicePropertyKey key)
    {
        try
        {
            return device.GetProperty<uint>(key);
        }
        catch (Exception)
        {
            try
            {
                return unchecked((uint)device.GetProperty<int>(key));
            }
            catch (Exception ex)
            {
                Log.Logger.Debug(ex, "Device property {Property} was unavailable for {InstanceId}.", key,
                    device.InstanceId);
                return null;
            }
        }
    }
}
