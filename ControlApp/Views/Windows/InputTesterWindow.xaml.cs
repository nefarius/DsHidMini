using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class InputTesterWindow
{
    public InputTesterWindow(InputTesterViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Closed += async (_, _) => await viewModel.ShutdownAsync();
        viewModel.Start();
    }
}
