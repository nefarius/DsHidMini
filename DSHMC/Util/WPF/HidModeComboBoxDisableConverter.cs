using System;
using System.Globalization;
using System.Windows.Data;
using Nefarius.DsHidMini.Drivers;

namespace Nefarius.DsHidMini.Util.WPF
{
    public class HidModeComboBoxDisableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            //
            // Switching to this mode while under the "wrong" INF will produce 
            // very confusing results, so it is disabled for user selection
            // 
            return (DsHidDeviceMode) value == DsHidDeviceMode.XInputHIDCompatible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}