using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

using Microsoft.Win32;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;
using Nefarius.DsHidMini.ControlApp.Models.Onboarding;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

/// <summary>
///     Drives the mandatory first-run setup window. Reuses the same
///     <see cref="BluetoothDiagnosticSession" /> pair/unplug/connect sequence as the guided
///     diagnostic, framed as a one-time setup instead of troubleshooting, with no way to skip
///     ahead: the only exits are finishing successfully or closing the application.
/// </summary>
public sealed partial class OnboardingViewModel : ObservableObject, IDisposable
{
    private readonly OnboardingCoordinator _coordinator;
    private readonly Dispatcher _dispatcher;
    private readonly BluetoothDiagnosticSession _session;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private BluetoothDiagnosticStage _stage = BluetoothDiagnosticStage.Idle;

    [ObservableProperty]
    private string _statusMessage = "Connect your controller to this PC with a USB cable to begin.";

    [ObservableProperty]
    private DiagnosticVerdict? _verdict;

    public OnboardingViewModel(BluetoothDiagnosticSession session, OnboardingCoordinator coordinator)
    {
        _session = session;
        _coordinator = coordinator;
        _dispatcher = Application.Current.Dispatcher;
        _session.PropertyChanged += OnSessionPropertyChanged;
        SyncFromSession();
    }

    public string Title => "DsHidMini setup — connect your controller";

    public ObservableCollection<PreflightCheckResult> PreflightItems { get; } = new();

    public bool HasVerdict => Verdict is not null;

    public bool IsSuccessful => Verdict?.Code == DiagnosticVerdictCode.Success;

    public bool CanApplyFix => PreflightItems.Any(i => !i.Passed && i.CanAutoRepair);

    public bool NeedsElevation => !SecurityUtil.IsElevated;

    /// <summary>
    ///     Raised once <see cref="FinishCommand" /> has recorded completion. The window should close
    ///     itself and let normal startup continue.
    /// </summary>
    public event EventHandler? SetupCompleted;

    public void Dispose()
    {
        _session.PropertyChanged -= OnSessionPropertyChanged;
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
            Log.Logger.Error(ex, "First-run setup check failed unexpectedly.");
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
    private async Task ApplyFix()
    {
        _session.TryAutoRepairBlockedCheck();
        await Start().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task ExportBundle()
    {
        SaveFileDialog dialog = new()
        {
            FileName = $"DsHidMiniSetup-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
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
            Log.Logger.Error(ex, "Failed to export first-run setup diagnostic bundle to '{Path}'.", dialog.FileName);
        }
    }

    [RelayCommand]
    private void RestartAsAdmin()
    {
        Main.RestartAsAdmin();
    }

    [RelayCommand]
    private void Finish()
    {
        if (!IsSuccessful)
        {
            return;
        }

        _coordinator.MarkCompleted();
        SetupCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _dispatcher.Invoke(SyncFromSession);
    }

    private void SyncFromSession()
    {
        Stage = _session.Stage;
        StatusMessage = _session.StatusMessage;
        Verdict = _session.Verdict;
        OnPropertyChanged(nameof(HasVerdict));
        OnPropertyChanged(nameof(IsSuccessful));

        PreflightItems.Clear();
        foreach (PreflightCheckResult item in _session.PreflightResults)
        {
            PreflightItems.Add(item);
        }

        OnPropertyChanged(nameof(CanApplyFix));
    }
}
