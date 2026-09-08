using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Pages;

public partial class AboutPage : INavigableView<AboutViewModel>
{
    public AboutPage(AboutViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public AboutViewModel ViewModel { get; }
}
