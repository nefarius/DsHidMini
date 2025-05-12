using Nefarius.DsHidMini.ControlApp.ViewModels.Pages;

using Wpf.Ui.Abstractions.Controls;

namespace Nefarius.DsHidMini.ControlApp.Views.Pages;

public partial class SettingsPage : INavigableView<SettingsViewModel>
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }
}