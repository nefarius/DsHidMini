using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Devices.HumanInterfaceDevice;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;

using Microsoft.Win32.SafeHandles;

using Nefarius.Utilities.DeviceManagement.Extensions;
using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

/// <summary>
///     Outcome of an attempt to switch a Retro Fighters Defender Bluetooth Edition out of its default
///     DualShock 4 USB identity into its DualShock 3 identity, which DsHidMini can bind to.
/// </summary>
public enum DefenderBtModeSwitchResult
{
    /// <summary>
    ///     The supplied device path does not point to a Defender BT in DualShock 4 mode.
    /// </summary>
    NotADefenderBt,

    /// <summary>
    ///     The probe report was written to the HID stack. This is not proof that the controller switched;
    ///     a live Defender can ACK Feature 0x14 and stay on <c>054C:05C4</c>.
    /// </summary>
    Sent,

    /// <summary>
    ///     A matching device was found but sending the probe report failed.
    /// </summary>
    Failed,

    /// <summary>
    ///     The DualShock 4 identity disappeared and a new DualShock 3 USB identity appeared.
    /// </summary>
    Switched,

    /// <summary>
    ///     The probe was delivered but the controller stayed in DualShock 4 mode.
    /// </summary>
    IgnoredByHardware,

    /// <summary>
    ///     The DualShock 4 identity is gone and no new DualShock 3 appeared: either the USB port cycle failed,
    ///     or it succeeded without either identity coming back. Reconnect (or run as administrator so the port
    ///     can be cycled) so the pending probe is retried immediately after re-enumeration.
    /// </summary>
    NeedsReconnect
}

/// <summary>
///     Detects a Retro Fighters Defender Bluetooth Edition controller enumerated in its default DualShock 4
///     USB identity (<c>USB\VID_054C&amp;PID_05C4</c>) and replays the same HID Feature report a real PS3
///     sends it to make it detach and re-enumerate as a DualShock 3 (<c>USB\VID_054C&amp;PID_0268</c>), which
///     DsHidMini already binds to via <c>driver/dshidmini.inf</c>.
/// </summary>
/// <remarks>
///     See issue #282 and <c>docs/PS3_USB_STARTUP.md</c> ("Retro Fighters Defender") for how this sequence was
///     derived from real PS3-to-Defender-BT USB captures. The report is a verbatim replay of what a genuine PS3
///     periodically sends a real DualShock 4 as well (harmless no-op there), but detection/targeting still
///     requires the Defender-specific <see cref="DefenderBtVersionNumber" /> discriminator below (in addition
///     to VID/PID) so that a genuine DualShock 4 is never misidentified as, or targeted as, a Defender BT.
///     <para>
///         A successful <c>HidD_SetFeature</c> only means the USB SET_REPORT was ACKed. On Windows the
///         Defender often ignores a late probe after interrupt IN is already streaming; the PS3 sent this
///         report a few milliseconds after SET_IDLE. Callers must wait for the DualShock 4 identity to
///         disappear (and preferably for <c>054C:0268</c> to appear) before reporting success, and should
///         retry immediately after a USB port cycle or replug when a late probe is ignored.
///     </para>
/// </remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class DefenderBtModeSwitcher
{
    /// <summary>
    ///     Sony's USB Vendor ID, shared by the genuine DualShock 3/4 and the Defender BT's DualShock 4 identity.
    /// </summary>
    public const ushort SonyVendorId = 0x054C;

    /// <summary>
    ///     Product ID the Defender BT (and a genuine DualShock 4) enumerates as by default.
    /// </summary>
    public const ushort DualShock4ProductId = 0x05C4;

    /// <summary>
    ///     Product ID the Defender BT (and a genuine DualShock 3) enumerates as after the probe below succeeds.
    /// </summary>
    public const ushort DualShock3ProductId = 0x0268;

    /// <summary>
    ///     The <c>bcdDevice</c> / <see cref="HIDD_ATTRIBUTES.VersionNumber" /> value observed on the Defender BT
    ///     while in its DualShock 4 identity. A genuine DualShock 4 reports <c>0x0100</c> here instead, so this
    ///     is required - in addition to matching VID/PID - before a device is treated as (or targeted as) a
    ///     Defender BT. Without this check a real DualShock 4 would be misidentified and offered/subjected to
    ///     the mode switch. See <c>docs/PS3_USB_STARTUP.md</c> ("Retro Fighters Defender") for the capture this
    ///     was derived from.
    /// </summary>
    public const ushort DefenderBtVersionNumber = 0x0221;

    /// <summary>
    ///     The 17-byte <c>SET_REPORT Feature 0x14</c> payload a real PS3 sends every ~1 second while a
    ///     DualShock-4-identity device is attached. On a Defender BT this makes it detach and re-enumerate as a
    ///     DualShock 3; on a genuine DualShock 4 it is a harmless no-op (confirmed from capture).
    /// </summary>
    private static readonly byte[] Ps3ModeProbeReport =
    {
        0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    /// <summary>
    ///     The HID device interface class GUID, used to enumerate/listen for HID device arrivals.
    /// </summary>
    public static Guid HidDeviceInterfaceGuid
    {
        get
        {
            PInvoke.HidD_GetHidGuid(out Guid guid);
            return guid;
        }
    }

    /// <summary>
    ///     True if VID/PID/version match the Defender-BT DualShock 4 identity (not a genuine DualShock 4).
    /// </summary>
    internal static bool MatchesDs4Identity(ushort vendorId, ushort productId, ushort versionNumber)
    {
        return vendorId == SonyVendorId &&
               productId == DualShock4ProductId &&
               versionNumber == DefenderBtVersionNumber;
    }

    /// <summary>
    ///     True if <paramref name="devicePath" /> points to a HID device currently reporting the Defender-BT
    ///     DualShock 4 identity (VID 0x054C, PID 0x05C4, VersionNumber 0x0221) - not just any DualShock 4.
    /// </summary>
    public static bool IsDefenderBtInDs4Mode(string devicePath)
    {
        using SafeFileHandle handle = OpenDevice(devicePath);
        return !handle.IsInvalid && TryGetAttributes(handle, out HIDD_ATTRIBUTES attributes) &&
               MatchesDs4Identity(attributes.VendorID, attributes.ProductID, attributes.VersionNumber);
    }

    /// <summary>
    ///     True if <paramref name="instanceId" /> is a DualShock 3 USB identity (<c>VID_054C&amp;PID_0268</c>).
    /// </summary>
    internal static bool IsDualShock3UsbInstanceId(string? instanceId)
    {
        return instanceId is not null &&
               instanceId.Contains("VID_054C", StringComparison.OrdinalIgnoreCase) &&
               instanceId.Contains("PID_0268", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Instance IDs of DualShock 3 USB identities currently on the bus. Snapshot this before a switch
    ///     attempt so a pre-existing <c>054C:0268</c> is not mistaken for the Defender re-enumerating.
    /// </summary>
    public static IReadOnlyList<string> ListDualShock3UsbInstanceIds()
    {
        List<string> instanceIds = [];
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(
                   DeviceInterfaceIds.UsbDevice, out string? _, out string? instanceId, instance++))
        {
            if (IsDualShock3UsbInstanceId(instanceId))
            {
                instanceIds.Add(instanceId);
            }
        }

        return instanceIds;
    }

    /// <summary>
    ///     True if <paramref name="currentInstanceIds" /> contains a DualShock 3 USB identity that was not in
    ///     <paramref name="instanceIdsBefore" />.
    /// </summary>
    internal static bool HasNewlyAppearedDualShock3Usb(
        IEnumerable<string> currentInstanceIds,
        IEnumerable<string> instanceIdsBefore)
    {
        HashSet<string> before = new(instanceIdsBefore, StringComparer.OrdinalIgnoreCase);
        foreach (string instanceId in currentInstanceIds)
        {
            if (IsDualShock3UsbInstanceId(instanceId) && !before.Contains(instanceId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     True if a DualShock 3 USB identity has appeared since <paramref name="instanceIdsBefore" /> was
    ///     captured.
    /// </summary>
    public static bool HasNewlyAppearedDualShock3Usb(IEnumerable<string> instanceIdsBefore)
    {
        return HasNewlyAppearedDualShock3Usb(ListDualShock3UsbInstanceIds(), instanceIdsBefore);
    }

    /// <summary>
    ///     Sends the PS3 mode-switch probe to <paramref name="devicePath" /> if (and only if) it currently
    ///     reports the Defender-BT DualShock 4 identity.
    /// </summary>
    public static unsafe DefenderBtModeSwitchResult TrySwitchToPs3Mode(string devicePath)
    {
        using SafeFileHandle handle = OpenDevice(devicePath);

        if (handle.IsInvalid || !TryGetAttributes(handle, out HIDD_ATTRIBUTES attributes) ||
            !MatchesDs4Identity(attributes.VendorID, attributes.ProductID, attributes.VersionNumber))
        {
            return DefenderBtModeSwitchResult.NotADefenderBt;
        }

        Log.Logger.Information(
            "Sending PS3 mode-switch probe to Defender BT candidate {DevicePath}", devicePath);

        HANDLE rawHandle = new(handle.DangerousGetHandle());

        fixed (byte* buffer = Ps3ModeProbeReport)
        {
            BOOLEAN ok = PInvoke.HidD_SetFeature(rawHandle, buffer, (uint)Ps3ModeProbeReport.Length);

            if (!(bool)ok)
            {
                Log.Logger.Warning(
                    "HidD_SetFeature failed for Defender BT candidate {DevicePath}, Win32 error {Error}",
                    devicePath, Marshal.GetLastWin32Error());
                return DefenderBtModeSwitchResult.Failed;
            }
        }

        return DefenderBtModeSwitchResult.Sent;
    }

    /// <summary>
    ///     Power-cycles the USB hub port that owns <paramref name="hidDevicePath" /> so the next probe can be
    ///     sent immediately after re-enumeration, matching the PS3's post-SET_IDLE timing.
    /// </summary>
    /// <returns>False if the port could not be cycled (typically missing administrator rights).</returns>
    public static bool TryCycleUsbPort(string hidDevicePath)
    {
        try
        {
            PnPDevice? hidDevice = PnPDevice.GetDeviceByInterfaceId(hidDevicePath);
            if (hidDevice is null)
            {
                Log.Logger.Warning("Could not resolve HID interface {DevicePath} for USB port cycle", hidDevicePath);
                return false;
            }

            IPnPDevice? parent = hidDevice.Parent;
            if (parent is null || string.IsNullOrEmpty(parent.InstanceId))
            {
                Log.Logger.Warning("HID interface {DevicePath} has no USB parent to cycle", hidDevicePath);
                return false;
            }

            PnPDevice usbDevice = PnPDevice.GetDeviceByInstanceId(parent.InstanceId);
            Log.Logger.Information("Cycling USB port for Defender BT parent {InstanceId}", usbDevice.InstanceId);
            usbDevice.ToUsbPnPDevice().CyclePort();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "USB port cycle failed for Defender BT candidate {DevicePath}", hidDevicePath);
            return false;
        }
    }

    private static SafeFileHandle OpenDevice(string devicePath)
    {
        SafeFileHandle exclusive = PInvoke.CreateFile(
            devicePath,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            0,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            null
        );

        if (!exclusive.IsInvalid)
        {
            return exclusive;
        }

        exclusive.Dispose();
        return PInvoke.CreateFile(
            devicePath,
            (uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            null
        );
    }

    private static bool TryGetAttributes(SafeFileHandle handle, out HIDD_ATTRIBUTES attributes)
    {
        return PInvoke.HidD_GetAttributes(handle, out attributes);
    }
}
