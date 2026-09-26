using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

using Microsoft.Win32;

using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

/// <summary>
///     Drives the guided Bluetooth connection diagnostic window: exposes
///     <see cref="BluetoothDiagnosticSession" /> state as UI-thread-safe observable properties and
///     wires up Start/Cancel/Export commands.
/// </summary>
public sealed partial class BluetoothDiagnosticViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly BluetoothDiagnosticSession _session;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private BluetoothDiagnosticStage _stage = BluetoothDiagnosticStage.Idle;

    [ObservableProperty]
    private string _statusMessage = "Ready to check your Bluetooth connection.";

    [ObservableProperty]
    private DiagnosticVerdict? _verdict;

    public BluetoothDiagnosticViewModel(BluetoothDiagnosticSession session)
    {
        _session = session;
        _dispatcher = Application.Current.Dispatcher;
        _session.PropertyChanged += OnSessionPropertyChanged;
        _session.TimelineUpdated += OnSessionTimelineUpdated;
        SyncFromSession();
    }

    public string Title => "Bluetooth connection diagnostic";

    public ObservableCollection<PreflightCheckResult> PreflightItems { get; } = new();

    public bool HasVerdict => Verdict is not null;

    public void Dispose()
    {
        _session.PropertyChanged -= OnSessionPropertyChanged;
        _session.TimelineUpdated -= OnSessionTimelineUpdated;
    }

    [RelayCommand]
    private async Task Start()
    {
        IsBusy = true;
        try
        {
            await _session.RunAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Bluetooth diagnostic run failed unexpectedly.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _session.Cancel();
    }

    [RelayCommand]
    private async Task ExportBundle()
    {
        SaveFileDialog dialog = new()
        {
            FileName = $"BluetoothDiagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
            Filter = "ZIP archive (*.zip)|*.zip",
            DefaultExt = ".zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await _session.ExportBundleAsync(dialog.FileName).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to export Bluetooth diagnostic bundle to '{Path}'.", dialog.FileName);
        }
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _dispatcher.Invoke(SyncFromSession);
    }

    private void OnSessionTimelineUpdated(object? sender, EventArgs e)
    {
        // Timeline entries themselves are only surfaced via the exported bundle today; no live
        // per-event UI binding is needed, so nothing to marshal here besides keeping the handler
        // registered for future use.
    }

    private void SyncFromSession()
    {
        Stage = _session.Stage;
        StatusMessage = _session.StatusMessage;
        Verdict = _session.Verdict;
        OnPropertyChanged(nameof(HasVerdict));

        PreflightItems.Clear();
        foreach (PreflightCheckResult item in _session.PreflightResults)
        {
            PreflightItems.Add(item);
        }
    }
}
