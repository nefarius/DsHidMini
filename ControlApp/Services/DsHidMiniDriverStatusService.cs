using Nefarius.DsHidMini.ControlApp.Models.Drivers;
using Nefarius.Utilities.DeviceManagement.PnP;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Tells the UI when an MSI upgrade left the previous DsHidMini driver active until the next reboot.
/// </summary>
public partial class DsHidMiniDriverStatusService : ObservableObject
{
    [ObservableProperty]
    private bool _isRebootPending;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Warning;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    public void Refresh(IReadOnlyList<PnPDevice> devices)
    {
        try
        {
            DriverRebootMarker marker = DsHidMiniSetup.ReadRebootMarker();
            List<DriverDeviceSnapshot> snapshots = devices.Select(Snapshot).ToList();

            DriverRebootPendingState state = DriverRebootPendingPolicy.Evaluate(
                marker,
                DriverRebootPendingPolicy.GetLastBootUtc(),
                DsHidMiniSetup.InstalledDriverVersion,
                snapshots);

            if (state.MarkerIsStale && SecurityUtil.IsElevated)
            {
                DsHidMiniSetup.TryClearRebootMarker();
            }

            if (state.IsPending)
            {
                Log.Logger.Warning("DsHidMini driver reboot pending: {Reasons}", string.Join(" ", state.Reasons));
            }

            IsRebootPending = state.IsPending;
            Severity = InfoBarSeverity.Warning;
            StatusTitle = "Restart required to finish the driver update";
            StatusMessage =
                "The DsHidMini driver was updated, but the previous version may still be active. " +
                "Controllers can misbehave until you restart Windows.";
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to evaluate DsHidMini driver reboot state.");
        }
    }

    private static DriverDeviceSnapshot Snapshot(PnPDevice device)
    {
        return new DriverDeviceSnapshot(
            device.InstanceId,
            TryGet(() => device.GetProperty<bool>(DevicePropertyKey.Device_IsRebootRequired)),
            TryGetNullable(() => device.GetProperty<uint>(DevicePropertyKey.Device_ProblemCode)),
            TryGetNullable(() => device.GetProperty<uint>(DevicePropertyKey.Device_DevNodeStatus)),
            DsHidMiniDriverCompatibility.TryGetInstalledVersion(device));
    }

    private static bool TryGet(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static uint? TryGetNullable(Func<uint> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
