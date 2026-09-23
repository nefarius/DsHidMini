using System.ComponentModel;

using Nefarius.DsHidMini.ControlApp.Models.Rumble;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

namespace Nefarius.DsHidMini.ControlApp.Views.Windows;

public partial class RumbleTesterWindow
{
    private readonly Action<string>? _reportShutdownFailure;
    private readonly RumbleTesterViewModel _viewModel;

    public RumbleTesterWindow(RumbleTesterViewModel viewModel, Action<string>? reportShutdownFailure = null)
    {
        _viewModel = viewModel;
        _reportShutdownFailure = reportShutdownFailure;
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Block the close, including owner and application shutdown, until the
        // rumble-off attempt finishes. An abrupt process crash cannot run this path.
        try
        {
            RumbleCommandResult result = _viewModel.ShutdownAsync().GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                ReportShutdownFailure(result);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to turn rumble off while closing the rumble tester");
            ReportShutdownFailure(RumbleCommandResult.FromException(ex));
        }

        base.OnClosing(e);
    }

    private void ReportShutdownFailure(RumbleCommandResult result)
    {
        string detail = result.Error
            ?? (result.Sent
                ? RumbleTesterStatus.Rejected(result.Status)
                : "The rumble-off request did not reach the driver.");
        Log.Logger.Warning("Rumble tester closed without turning rumble off: {Detail}", detail);
        try
        {
            _reportShutdownFailure?.Invoke(detail);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to report rumble-off failure");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
