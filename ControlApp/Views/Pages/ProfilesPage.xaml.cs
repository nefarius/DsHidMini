using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Pages;

public partial class ProfilesPage : INavigableView<ProfilesViewModel>
{
    public ProfilesPage(ProfilesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public ProfilesViewModel ViewModel { get; }
}