using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

/// <summary>
///     Guided window that walks the user through pairing, unplugging, and reconnecting a
///     controller over Bluetooth, then explains exactly where it stopped and what to do next.
/// </summary>
public partial class BluetoothDiagnosticWindow
{
    private readonly BluetoothDiagnosticViewModel _viewModel;

    public BluetoothDiagnosticWindow(BluetoothDiagnosticViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.IsBusy)
        {
            _viewModel.CancelCommand.Execute(null);
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
