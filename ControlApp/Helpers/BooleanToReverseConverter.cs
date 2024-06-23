using System.Globalization;
using System.Windows.Data;

namespace Nefarius.DsHidMini.ControlApp.Helpers
{
    public class BooleanToReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return !(bool?)value ?? true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
         => !(value as bool?);
    }
}