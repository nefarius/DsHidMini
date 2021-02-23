using Microsoft.Win32;

namespace Nefarius.DsHidMini.Drivers
{
    public static class BthPS3ProfileDriver
    {
        private static string ParametersPath => "SYSTEM\\CurrentControlSet\\Services\\BthPS3\\Parameters";

        /// <summary>
        ///     Gets or sets the RawPDO setting.
        /// </summary>
        public static bool RawPDO
        {
            get
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ParametersPath))
                {
                    if (int.TryParse(key?.GetValue("RawPDO").ToString(), out var result))
                    {
                        return result > 0;
                    }

                    return false;
                }
            }
            set
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ParametersPath, true))
                {
                    key?.SetValue("RawPDO", value ? 1 : 0, RegistryValueKind.DWord);
                }
            }
        }
    }
}
