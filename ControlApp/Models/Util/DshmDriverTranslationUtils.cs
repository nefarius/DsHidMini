using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.Models.Util;

internal static class DshmDriverTranslationUtils
{
    public static readonly Dictionary<int, SettingsContext> HidDeviceMode = new()
    {
        { 0x01, SettingsContext.SDF },
        { 0x02, SettingsContext.GPJ },
        { 0x03, SettingsContext.SXS },
        { 0x04, SettingsContext.DS4W },
        { 0x05, SettingsContext.XInput }
    };

    /// <summary>
    ///     Converts a <see cref="SettingsContext" /> HID mode back into the byte value the driver stores in
    ///     DEVPKEY_DsHidMini_RW_HidDeviceMode (see <see cref="HidDeviceMode" /> for the forward mapping).
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown if <paramref name="context" /> is not one of the concrete HID device modes (e.g. Unknown, General,
    ///     Global).
    /// </exception>
    public static byte ToHidDeviceModePropertyValue(SettingsContext context)
    {
        return (byte)HidDeviceMode.First(pair => pair.Value == context).Key;
    }
}