using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Pages;

public partial class DevicesPage : INavigableView<DevicesViewModel>
{
    public DevicesPage(DevicesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public DevicesViewModel ViewModel { get; }
}