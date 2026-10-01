using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class ControllerDiagnosticExportWindow
{
    public ControllerDiagnosticExportWindow(ControllerDiagnosticExportViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Closing += (_, _) => viewModel.RequestCancel();
    }
}
