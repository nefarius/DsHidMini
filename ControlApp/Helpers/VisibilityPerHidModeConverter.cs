using System.Globalization;
using System.Windows.Data;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.Enums;

namespace Nefarius.DsHidMini.ControlApp.Helpers
{
    public class VisibilityPerHidModeConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            int VisibilityPerHIDModeFlags = System.Convert.ToInt32((string)parameter, 2);
            SettingsContext context = (SettingsContext)value;
            int amountToBitShift = 0;

            switch (context)
            {
                case SettingsContext.SDF:
                    amountToBitShift = 0;
                    break;
                case SettingsContext.GPJ:
                    amountToBitShift = 1;
                    break;
                case SettingsContext.SXS:
                    amountToBitShift = 2;
                    break;
                case SettingsContext.DS4W:
                    amountToBitShift = 3;
                    break;
                case SettingsContext.XInput:
                    amountToBitShift = 4;
                    break;
                case SettingsContext.General:
                    amountToBitShift = 5;
                    break;
                case SettingsContext.Global:
                    amountToBitShift = 6;
                    break;
                default:
                    return false;
            }

            return (((VisibilityPerHIDModeFlags >> amountToBitShift) & 1U) == 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}