using System.Diagnostics;
using System.Windows;

using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

public sealed partial class MotionViewerViewModel : ObservableObject, IDisposable
{
    private readonly int _deviceIndex;
    private readonly MotionOrientationEstimator _estimator;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _interopLock = new();
    private readonly object _shutdownLock = new();
    private DsHidMiniInterop? _interop;
    private Task? _pumpTask;
    private Task? _shutdownTask;
    private int _uiGeneration;
    private bool _disposed;
    private const int SnapshotMissBudget = 10;

    public MotionViewerViewModel(int deviceIndex, string deviceTitle)
    {
        _deviceIndex = deviceIndex;
        Title = $"Motion viewer — {deviceTitle}";
        _estimator = new MotionOrientationEstimator(Stopwatch.Frequency);
        StatusText = MotionStatusFormatter.StatusText(true, false, false, false);
    }

    public string Title { get; }

    internal MotionOrientationEstimator Estimator => _estimator;

    public event EventHandler? PoseChanged;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isUnavailable;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private string _rawAccelText = "—";

    [ObservableProperty]
    private string _calAccelText = "—";

    [ObservableProperty]
    private string _accelGText = "—";

    [ObservableProperty]
    private string _rawGyroText = "—";

    [ObservableProperty]
    private string _calGyroText = "—";

    [ObservableProperty]
    private string _gyroDpsText = "—";

    [ObservableProperty]
    private string _eepromText = "—";

    [ObservableProperty]
    private string _trackerText = "—";

    [ObservableProperty]
    private string _pathText = "—";

    [ObservableProperty]
    private string _sampleText = "—";

    [ObservableProperty]
    private string _yawNote =
        "Yaw is integrated from the single SIXAXIS gyro and will drift. Use Recenter after holding the pad still.";

    public CancellationToken SessionToken => _cts.Token;

    public bool IsSessionCancelled { get; private set; }

    public void Start()
    {
        _pumpTask = Task.Run(PumpAsync);
    }

    [RelayCommand]
    private void Recenter()
    {
        _estimator.Recenter();
        PoseChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    private async Task PumpAsync()
    {
        try
        {
            if (!DsHidMiniInterop.IsAvailable)
            {
                PublishUnavailable(MotionStatusFormatter.StatusText(false, false, false, false));
                return;
            }

            lock (_interopLock)
            {
                _interop = new DsHidMiniInterop();
            }

            bool mapped = _interop.HasMotionTelemetry;
            if (!mapped)
            {
                PublishUnavailable(MotionStatusFormatter.StatusText(true, false, false, false));
                return;
            }

            int misses = 0;
            while (!_cts.IsCancellationRequested)
            {
                DsMotionSnapshot snapshot = default;
                bool got;
                try
                {
                    got = _interop.GetMotionSnapshot(_deviceIndex, out snapshot, TimeSpan.FromMilliseconds(40));
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

                if (got && snapshot.SlotIndex == 0)
                {
                    PublishUnavailable("The controller disconnected or its IPC slot is empty.");
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

                if (!IsPaused)
                {
                    _estimator.Update(snapshot);
                }

                int generation = Interlocked.Increment(ref _uiGeneration);
                DsMotionSnapshot copy = snapshot;
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

                    ApplySnapshot(copy, mapped);
                    PoseChanged?.Invoke(this, EventArgs.Empty);
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

    private void ApplySnapshot(in DsMotionSnapshot snapshot, bool mapped)
    {
        IsUnavailable = false;
        StatusSeverity = snapshot.IsFallback ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;
        StatusText = MotionStatusFormatter.StatusText(true, mapped, snapshot.IsAvailable, snapshot.IsFallback);
        PathText = MotionStatusFormatter.PathLabel(snapshot.MotionPath);
        RawAccelText = $"{snapshot.RawAccelX}  {snapshot.RawAccelY}  {snapshot.RawAccelZ}";
        CalAccelText = $"{snapshot.CalAccelX}  {snapshot.CalAccelY}  {snapshot.CalAccelZ}";
        AccelGText =
            $"{snapshot.AccelMilliGX / 1000.0:0.00}  {snapshot.AccelMilliGY / 1000.0:0.00}  {snapshot.AccelMilliGZ / 1000.0:0.00} g";
        RawGyroText = snapshot.RawGyro.ToString();
        CalGyroText = snapshot.CalGyro.ToString();
        GyroDpsText = $"{snapshot.GyroMilliDps / 1000.0:0.00} deg/s";
        EepromText =
            $"X {snapshot.AccelZeroX}/{snapshot.AccelOneGX}  Y {snapshot.AccelZeroY}/{snapshot.AccelOneGY}  Z {snapshot.AccelZeroZ}/{snapshot.AccelOneGZ}  G {snapshot.GyroZero}/{snapshot.GyroEepromCal}";
        TrackerText = $"zero {snapshot.ZeroRef}  cal 0x{snapshot.CalByte:X2}  tracker {(snapshot.HasTracker ? "on" : "off")}";
        SampleText = $"#{snapshot.SampleIndex}  QPC {snapshot.TimestampQpc}";
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
