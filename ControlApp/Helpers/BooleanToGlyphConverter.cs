using System.Globalization;
using System.Windows.Data;

namespace Nefarius.DsHidMini.ControlApp.Helpers;

/// <summary>
///     Converts a boolean pass/fail state to a plain checkmark/cross glyph. Deliberately not
///     color-only, so the check result is understandable without relying on color perception.
/// </summary>
public class BooleanToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "\u2714" : "\u2716";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
