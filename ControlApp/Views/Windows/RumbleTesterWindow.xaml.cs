using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class RumbleTesterWindow
{
    private readonly RumbleTesterViewModel _viewModel;

    public RumbleTesterWindow(RumbleTesterViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Block the close, including owner and application shutdown, until the
        // driver acknowledges rumble off. An abrupt process crash cannot run this path.
        try
        {
            _viewModel.ShutdownAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to turn rumble off while closing the rumble tester");
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
