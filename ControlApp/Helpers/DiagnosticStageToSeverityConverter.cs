using System.Globalization;
using System.Windows.Data;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Helpers;

/// <summary>
///     Maps a <see cref="BluetoothDiagnosticStage" /> to an <see cref="InfoBarSeverity" /> so the
///     status banner's color and icon always match what actually happened.
/// </summary>
public class DiagnosticStageToSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            BluetoothDiagnosticStage.Completed => InfoBarSeverity.Success,
            BluetoothDiagnosticStage.PreflightBlocked => InfoBarSeverity.Warning,
            BluetoothDiagnosticStage.PairingFailed => InfoBarSeverity.Warning,
            BluetoothDiagnosticStage.Faulted => InfoBarSeverity.Error,
            BluetoothDiagnosticStage.Cancelled => InfoBarSeverity.Informational,
            _ => InfoBarSeverity.Informational
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
