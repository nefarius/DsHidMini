using Nefarius.DsHidMini.IPC.Models.Drivers;

namespace Nefarius.DsHidMini.ControlApp.Models;

/// <summary>
///     Capability policy for DsHidMini hardware families. ControlApp uses this
///     instead of inferring limits from display names or virtual HID IDs.
/// </summary>
public static class DsDeviceCapabilities
{
    public const ushort SonyVendorId = 0x054C;
    public const ushort SixaxisProductId = 0x0268;
    public const ushort NavigationProductId = 0x042F;

    public static DsDeviceType FromHardwareIds(ushort vendorId, ushort productId)
    {
        if (vendorId != SonyVendorId)
        {
            return DsDeviceType.Unknown;
        }

        return productId switch
        {
            NavigationProductId => DsDeviceType.Navigation,
            SixaxisProductId => DsDeviceType.Sixaxis,
            _ => DsDeviceType.Unknown
        };
    }

    public static bool HasRumble(DsDeviceType type) => type != DsDeviceType.Navigation;

    public static bool HasSingleLed(DsDeviceType type) => type == DsDeviceType.Navigation;

    public static bool IsNavigation(DsDeviceType type) => type == DsDeviceType.Navigation;

    public static string DisplayName(DsDeviceType type) =>
        type switch
        {
            DsDeviceType.Navigation => "Navigation Controller",
            DsDeviceType.Sixaxis => "DualShock 3 / SIXAXIS",
            DsDeviceType.Motion => "Motion Controller",
            DsDeviceType.Wireless => "DualShock 4",
            _ => "DS3 Compatible HID Device"
        };

    public static string HidModeGuidance(DsDeviceType type) =>
        IsNavigation(type)
            ? "XInput is recommended. SDF, GPJ, SXS, and DS4Windows stay available for partial-input compatibility, but missing buttons, the right stick, extra pressure axes, rumble, and extra LEDs stay neutral or have no effect."
            : string.Empty;
}
