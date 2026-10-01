using System.Globalization;

using Nefarius.Utilities.DeviceManagement.PnP;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

/// <summary>
///     A USB adapter DsHidMini recognizes but does not bind. ControlApp uses this
///     to warn without opening HID handles or claiming the device.
/// </summary>
public sealed record UnsupportedAdapter(
    ushort VendorId,
    ushort ProductId,
    string DisplayName,
    string StatusTitle,
    string StatusMessage);

/// <summary>
///     Known USB adapters that stay on the inbox HID stack. Matching is VID/PID
///     from the USB instance ID only; no <c>CreateFile</c> / HID attribute probe.
/// </summary>
public static class UnsupportedAdapterDetector
{
    public const ushort TwinUsbVendorId = 0x0810;
    public const ushort TwinUsbProductId = 0x0001;

    /// <summary>
    ///     Adapters ControlApp should surface. Append new entries; do not reuse a
    ///     VID/PID that DsHidMini already binds.
    /// </summary>
    public static readonly UnsupportedAdapter[] KnownAdapters =
    [
        new(
            TwinUsbVendorId,
            TwinUsbProductId,
            "Twin USB Joystick",
            "Twin USB Joystick adapter is not supported",
            "A Twin USB Joystick (VID_0810 / PID_0001) is plugged in. DsHidMini leaves it to the Windows inbox HID driver so both ports keep working there. This adapter puts two controller ports in one USB device, the same ID is used by many non-adapter gamepads, and it only forwards digital buttons and 8-bit axes. If Game Controllers (joy.cpl) shows no input, the adapter is not talking to the pad on the PS2 side; attach the controller before plugging the adapter in.")
    ];

    public static bool IsKnownUnsupported(ushort vendorId, ushort productId) =>
        TryGetKnownAdapter(vendorId, productId) is not null;

    public static bool IsKnownUnsupportedInstanceId(string? instanceId) =>
        TryMatchInstanceId(instanceId, out _);

    public static bool TryGetKnownAdapter(ushort vendorId, ushort productId, out UnsupportedAdapter adapter)
    {
        UnsupportedAdapter? match = TryGetKnownAdapter(vendorId, productId);
        adapter = match!;
        return match is not null;
    }

    public static bool TryMatchInstanceId(string? instanceId, out UnsupportedAdapter adapter)
    {
        adapter = null!;
        return TryReadHexId(instanceId, "VID_", out ushort vendorId) &&
               TryReadHexId(instanceId, "PID_", out ushort productId) &&
               TryGetKnownAdapter(vendorId, productId, out adapter);
    }

    /// <summary>
    ///     First matching USB device currently on the bus, or <see langword="null"/>.
    /// </summary>
    public static UnsupportedAdapter? FindFirstPresent()
    {
        int instance = 0;
        while (Devcon.FindByInterfaceGuid(
                   DeviceInterfaceIds.UsbDevice, out string? _, out string? instanceId, instance++))
        {
            if (TryMatchInstanceId(instanceId, out UnsupportedAdapter adapter))
            {
                return adapter;
            }
        }

        return null;
    }

    private static UnsupportedAdapter? TryGetKnownAdapter(ushort vendorId, ushort productId)
    {
        foreach (UnsupportedAdapter adapter in KnownAdapters)
        {
            if (adapter.VendorId == vendorId && adapter.ProductId == productId)
            {
                return adapter;
            }
        }

        return null;
    }

    private static bool TryReadHexId(string? instanceId, string marker, out ushort value)
    {
        value = 0;
        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        int markerIndex = instanceId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        int valueIndex = markerIndex + marker.Length;
        return markerIndex >= 0 &&
               instanceId.Length >= valueIndex + 4 &&
               ushort.TryParse(instanceId.AsSpan(valueIndex, 4), NumberStyles.AllowHexSpecifier,
                   CultureInfo.InvariantCulture, out value);
    }
}
