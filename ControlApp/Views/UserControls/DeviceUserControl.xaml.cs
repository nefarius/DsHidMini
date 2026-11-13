using System.Windows.Controls;

using Nefarius.DsHidMini.ControlApp.ViewModels.UserControls.DeviceSettings;

namespace Nefarius.DsHidMini.ControlApp.Views.UserControls;

/// <summary>
///     Interaction logic for DeviceSettingsEditor.xaml
/// </summary>
public partial class DeviceUserControl : UserControl
{
    public DeviceUserControl()
    {
        InitializeComponent();
    }

    private void CemuhookListenAddress_DropDownOpened(object? sender, EventArgs e)
    {
        if (sender is ComboBox { DataContext: CemuhookUdpSettingsViewModel settings })
            settings.RefreshListenAddresses();
    }
}
