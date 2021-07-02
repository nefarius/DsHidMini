using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Nefarius.DsHidMini.Util.WPF
{
    internal class BoolMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return values.Cast<bool>().All(v => v);
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}