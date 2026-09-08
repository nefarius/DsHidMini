using Microsoft.Win32;

namespace Nefarius.DsHidMini.ControlApp.Models.Drivers;

/// <summary>
///     Reads and writes DsHidMini WUDF service parameters under HKLM.
/// </summary>
internal interface IDshmDriverParametersStore
{
    /// <summary>
    ///     Reads the <c>IPCEnabled</c> DWORD. A missing key or value is reported as success with
    ///     <paramref name="value" /> set to <see langword="null" />.
    /// </summary>
    /// <returns><see langword="false" /> if the registry could not be queried or the value is not a DWORD.</returns>
    bool TryReadIpcEnabled(out int? value);

    /// <summary>
    ///     Writes <c>IPCEnabled</c> as a DWORD. Requires administrator rights.
    /// </summary>
    void WriteIpcEnabled(int value);
}

/// <summary>
///     Registry-backed <see cref="IDshmDriverParametersStore" /> for the installed DsHidMini driver.
/// </summary>
internal sealed class DshmDriverParametersStore : IDshmDriverParametersStore
{
    internal const string ParametersKeyPath =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services\dshidmini\Parameters";

    internal const string IpcEnabledValueName = "IPCEnabled";

    public bool TryReadIpcEnabled(out int? value)
    {
        value = null;

        try
        {
            using RegistryKey? key = RegistryHelpers.GetRegistryKey(ParametersKeyPath);
            if (key is null)
            {
                return true;
            }

            object? raw = key.GetValue(IpcEnabledValueName);
            if (raw is null)
            {
                return true;
            }

            if (!TryCoerceDword(raw, out int coerced))
            {
                Log.Logger.Warning(
                    "DsHidMini {ValueName} has an unexpected type {ValueType}.",
                    IpcEnabledValueName,
                    raw.GetType().FullName);
                return false;
            }

            value = coerced;
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to read {ValueName} from DsHidMini parameters.", IpcEnabledValueName);
            return false;
        }
    }

    public void WriteIpcEnabled(int value)
    {
        using RegistryKey? key = RegistryHelpers.GetRegistryKey(ParametersKeyPath, writable: true);
        if (key is null)
        {
            throw new InvalidOperationException("DsHidMini parameters registry key was not found.");
        }

        key.SetValue(IpcEnabledValueName, value, RegistryValueKind.DWord);
    }

    private static bool TryCoerceDword(object raw, out int value)
    {
        switch (raw)
        {
            case int i:
                value = i;
                return true;
            case uint u:
                value = unchecked((int)u);
                return true;
            default:
                value = 0;
                return false;
        }
    }
}
