using Nefarius.DsHidMini.ControlApp.Models.Drivers;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Reports and updates the experimental driver IPCEnabled registry flag.
/// </summary>
public partial class DshmIpcStatusService : ObservableObject
{
    private readonly Func<bool> _isElevatedProbe;
    private readonly IDshmDriverParametersStore _store;

    public DshmIpcStatusService()
        : this(new DshmDriverParametersStore(), static () => SecurityUtil.IsElevated)
    {
    }

    internal DshmIpcStatusService(IDshmDriverParametersStore store, Func<bool> isElevated)
    {
        _store = store;
        _isElevatedProbe = isElevated;
    }

    [ObservableProperty]
    private bool _canDisable;

    [ObservableProperty]
    private bool _canEnable;

    [ObservableProperty]
    private bool _isElevated;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isStateKnown;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private string _stateDisplay = "Unknown";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    /// <summary>
    ///     Re-reads elevation and the stored IPCEnabled value.
    /// </summary>
    public void Refresh()
    {
        IsElevated = _isElevatedProbe();

        if (!_store.TryReadIpcEnabled(out int? value))
        {
            IsStateKnown = false;
            IsEnabled = false;
        }
        else
        {
            IsStateKnown = true;
            IsEnabled = value is > 0;
        }

        CanEnable = IsElevated && IsStateKnown && !IsEnabled;
        CanDisable = IsElevated && IsStateKnown && IsEnabled;
        StateDisplay = !IsStateKnown
            ? "Unknown"
            : IsEnabled
                ? "Enabled"
                : "Disabled";

        ApplyStatus();
    }

    /// <summary>
    ///     Writes IPCEnabled as a DWORD and refreshes the displayed state.
    /// </summary>
    public bool TrySetEnabled(bool enabled)
    {
        if (!_isElevatedProbe())
        {
            Refresh();
            return false;
        }

        try
        {
            _store.WriteIpcEnabled(enabled ? 1 : 0);
            Refresh();
            return IsStateKnown && IsEnabled == enabled;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to update DsHidMini IPCEnabled.");
            Refresh();
            return false;
        }
    }

    private void ApplyStatus()
    {
        if (!IsStateKnown)
        {
            SetStatus(
                InfoBarSeverity.Warning,
                "Driver IPC state could not be read",
                "Could not read IPCEnabled from the DsHidMini driver parameters.");
            return;
        }

        if (IsEnabled)
        {
            SetStatus(
                InfoBarSeverity.Warning,
                "Experimental driver IPC is enabled",
                IsElevated
                    ? "IPC is still experimental. A reboot or driver reload is required after changing this setting."
                    : "IPC is still experimental. Restart as Administrator to change this setting. A reboot or driver reload is required after a change.");
            return;
        }

        SetStatus(
            InfoBarSeverity.Informational,
            "Experimental driver IPC is disabled",
            IsElevated
                ? "The driver keeps IPC off unless IPCEnabled is set to 1. A reboot or driver reload is required after changing this setting."
                : "The driver keeps IPC off unless IPCEnabled is set to 1. Restart as Administrator to change this setting.");
    }

    private void SetStatus(InfoBarSeverity severity, string title, string message)
    {
        Severity = severity;
        StatusTitle = title;
        StatusMessage = message;
    }
}
