using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class ControllerDiagnosticExportWindow
{
    public ControllerDiagnosticExportWindow(ControllerDiagnosticExportViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Closing += (_, e) =>
        {
            // Do not close mid-export; the user must cancel first.
            if (viewModel.IsRunning)
            {
                e.Cancel = true;
            }
        };
    }
}
