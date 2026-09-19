using System.Windows;

using Nefarius.DsHidMini.ControlApp.Models.Input;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

public sealed partial class InputTesterViewModel : ObservableObject, IDisposable
{
    private readonly int _deviceIndex;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _interopLock = new();
    private readonly object _shutdownLock = new();
    private DsHidMiniInterop? _interop;
    private Task? _pumpTask;
    private Task? _shutdownTask;
    private int _uiGeneration;
    private bool _disposed;
    private const int SnapshotMissBudget = 10;

    public InputTesterViewModel(int deviceIndex, string deviceTitle)
    {
        _deviceIndex = deviceIndex;
        Title = $"Input tester — {deviceTitle}";
        StatusText = InputTesterStatusFormatter.StatusText(true, false);
    }

    public string Title { get; }

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isUnavailable;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private ControllerInputState _state = ControllerInputState.Idle;

    public CancellationToken SessionToken => _cts.Token;

    public bool IsSessionCancelled { get; private set; }

    public void Start()
    {
        _pumpTask = Task.Run(PumpAsync);
    }

    private async Task PumpAsync()
    {
        try
        {
            if (!DsHidMiniInterop.IsAvailable)
            {
                PublishUnavailable(InputTesterStatusFormatter.StatusText(false, false));
                return;
            }

            lock (_interopLock)
            {
                _interop = new DsHidMiniInterop();
            }

            int misses = 0;
            while (!_cts.IsCancellationRequested)
            {
                DS3_RAW_INPUT_REPORT report = default;
                bool got;
                try
                {
                    got = _interop.GetRawInputReport(_deviceIndex, ref report, TimeSpan.FromMilliseconds(40));
                }
                catch (Exception ex)
                {
                    PublishUnavailable(ex.Message);
                    return;
                }

                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                if (!got)
                {
                    misses++;
                    if (misses >= SnapshotMissBudget)
                    {
                        PublishUnavailable("The controller disconnected or its IPC slot is empty.");
                        return;
                    }

                    continue;
                }

                misses = 0;
                ControllerInputState mapped = ControllerInputState.FromRawReport(in report);
                int generation = Interlocked.Increment(ref _uiGeneration);
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null)
                {
                    return;
                }

                await dispatcher.InvokeAsync(() =>
                {
                    if (_disposed || generation != Volatile.Read(ref _uiGeneration))
                    {
                        return;
                    }

                    ApplySnapshot(mapped);
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            PublishUnavailable(ex.Message);
        }
    }

    private void ApplySnapshot(ControllerInputState state)
    {
        IsUnavailable = false;
        StatusSeverity = InfoBarSeverity.Informational;
        StatusText = InputTesterStatusFormatter.StatusText(true, true);
        State = state;
    }

    private void PublishUnavailable(string message)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
            {
                return;
            }

            IsUnavailable = true;
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = message;
            State = ControllerInputState.Idle;
        });
    }

    public void Dispose()
    {
        _ = ShutdownAsync();
    }

    internal Task ShutdownAsync()
    {
        lock (_shutdownLock)
        {
            if (_shutdownTask is not null)
            {
                return _shutdownTask;
            }

            _disposed = true;
            IsSessionCancelled = true;
            _cts.Cancel();
            _shutdownTask = CompleteShutdownAsync();
            return _shutdownTask;
        }
    }

    private async Task CompleteShutdownAsync()
    {
        Task? pump = _pumpTask;
        if (pump is not null)
        {
            try
            {
                await pump.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lock (_interopLock)
        {
            _interop?.Dispose();
            _interop = null;
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
