using Nefarius.DsHidMini.ControlApp.Models.Rumble;
using Nefarius.DsHidMini.IPC.Models.Public;

using Wpf.Ui.Controls;

namespace Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

public sealed partial class RumbleTesterViewModel : ObservableObject, IDisposable
{
    internal static readonly TimeSpan DefaultPulseDuration = TimeSpan.FromMilliseconds(800);

    private readonly Func<IRumbleOutput> _outputFactory;
    private readonly Func<bool>? _readOutputStalled;
    private readonly TimeSpan _pulseDuration;
    private readonly object _gate = new();
    private readonly object _shutdownLock = new();
    private IRumbleOutput? _output;
    private System.Threading.Timer? _outputStallPoll;
    private CancellationTokenSource? _pulseCts;
    private Task<RumbleCommandResult>? _shutdownTask;
    private int _acceptCommands = 1;
    private int _pulseGeneration;

    public RumbleTesterViewModel(
        int deviceIndex,
        string deviceTitle,
        Func<bool>? readOutputStalled = null,
        string? outputStallNote = null)
        : this(deviceIndex, deviceTitle, null, null, readOutputStalled, outputStallNote, null)
    {
    }

    internal RumbleTesterViewModel(
        int deviceIndex,
        string deviceTitle,
        Func<IRumbleOutput>? outputFactory,
        TimeSpan? pulseDuration,
        Func<bool>? readOutputStalled = null,
        string? outputStallNote = null,
        TimeSpan? outputStallPollInterval = null)
    {
        _outputFactory = outputFactory ?? (() => new IpcRumbleOutput(deviceIndex));
        _readOutputStalled = readOutputStalled;
        _pulseDuration = pulseDuration ?? DefaultPulseDuration;
        OutputStallNote = outputStallNote ?? string.Empty;
        Title = $"Rumble tester — {deviceTitle}";
        StatusText = RumbleTesterStatus.Ready;
        if (readOutputStalled is not null)
        {
            TimeSpan interval = outputStallPollInterval ?? TimeSpan.FromMilliseconds(500);
            _outputStallPoll = new Timer(
                _ => PublishOutputStall(QueryOutputStalled()),
                null,
                TimeSpan.Zero,
                interval);
        }
    }

    public string OutputStallNote { get; }

    public string Title { get; }

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private double _largeMotor = 128;

    [ObservableProperty]
    private double _smallMotor = 128;

    [ObservableProperty]
    private bool _isPulsing;

    [ObservableProperty]
    private bool _isOutputStalled;

    [ObservableProperty]
    private bool _isShutdown;

    [RelayCommand]
    private Task TestLarge()
    {
        return PulseAsync(RumbleStrength.FromSlider(LargeMotor), 0);
    }

    [RelayCommand]
    private Task TestSmall()
    {
        return PulseAsync(0, RumbleStrength.FromSlider(SmallMotor));
    }

    [RelayCommand]
    private Task TestBoth()
    {
        return PulseAsync(
            RumbleStrength.FromSlider(LargeMotor),
            RumbleStrength.FromSlider(SmallMotor));
    }

    [RelayCommand]
    private Task Stop()
    {
        return TurnOffAsync();
    }

    internal Task<RumbleCommandResult> ShutdownAsync()
    {
        lock (_shutdownLock)
        {
            if (_shutdownTask is not null)
            {
                return _shutdownTask;
            }

            IsShutdown = true;
            StopOutputStallPoll();
            _shutdownTask = Task.Run(ShutdownCore);
            return _shutdownTask;
        }
    }

    public void Dispose()
    {
        ShutdownAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async Task PulseAsync(byte large, byte small)
    {
        if (Volatile.Read(ref _acceptCommands) == 0)
        {
            return;
        }

        int generation = Interlocked.Increment(ref _pulseGeneration);
        var pulse = new CancellationTokenSource();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _pulseCts, pulse);
        CancelPulse(previous);
        previous?.Dispose();
        Publish(() =>
        {
            if (!IsCurrentPulse(generation))
            {
                return;
            }

            IsPulsing = true;
        });

        try
        {
            RumbleCommandResult on = await Task.Run(() => Send(large, small, generation)).ConfigureAwait(false);
            if (!on.Sent || generation != Volatile.Read(ref _pulseGeneration))
            {
                return;
            }

            bool stalled = QueryOutputStalled();
            Publish(() =>
            {
                if (!IsCurrentPulse(generation))
                {
                    return;
                }

                IsOutputStalled = stalled;
                IsPulsing = on.Succeeded;
                ApplyResult(on, RumbleTesterStatus.Pulsing(large, small));
            });

            try
            {
                await Task.Delay(_pulseDuration, pulse.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (generation != Volatile.Read(ref _pulseGeneration) || Volatile.Read(ref _acceptCommands) == 0)
            {
                return;
            }

            RumbleCommandResult off = await Task.Run(() => Send(0, 0, generation)).ConfigureAwait(false);
            if (!off.Sent || generation != Volatile.Read(ref _pulseGeneration))
            {
                return;
            }

            bool stalledAfterOff = QueryOutputStalled();
            Publish(() =>
            {
                if (!IsCurrentPulse(generation))
                {
                    return;
                }

                IsOutputStalled = stalledAfterOff;
                IsPulsing = false;
                if (!off.Succeeded)
                {
                    ApplyResult(off, RumbleTesterStatus.Off);
                    return;
                }

                if (!on.Succeeded)
                {
                    return;
                }

                ApplyResult(off, RumbleTesterStatus.Off);
            });
        }
        finally
        {
            if (generation == Volatile.Read(ref _pulseGeneration) && Volatile.Read(ref _acceptCommands) != 0)
            {
                Publish(() =>
                {
                    if (!IsCurrentPulse(generation))
                    {
                        return;
                    }

                    IsPulsing = false;
                });
            }
        }
    }

    private async Task TurnOffAsync()
    {
        if (Volatile.Read(ref _acceptCommands) == 0)
        {
            return;
        }

        int generation = Interlocked.Increment(ref _pulseGeneration);
        CancelPulse(_pulseCts);
        RumbleCommandResult off = await Task.Run(() => Send(0, 0, generation)).ConfigureAwait(false);
        bool stalled = QueryOutputStalled();
        Publish(() =>
        {
            if (!IsCurrentPulse(generation))
            {
                return;
            }

            IsOutputStalled = stalled;
            IsPulsing = false;
            ApplyResult(off, RumbleTesterStatus.Off);
        });
    }

    private RumbleCommandResult Send(byte large, byte small, int generation)
    {
        lock (_gate)
        {
            if (Volatile.Read(ref _acceptCommands) == 0 || Volatile.Read(ref _pulseGeneration) != generation)
            {
                return RumbleCommandResult.NotSent;
            }

            try
            {
                uint status = EnsureOutput().SetRumble(large, small);
                return RumbleCommandResult.FromStatus(status);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Rumble request failed");
                return RumbleCommandResult.FromException(ex);
            }
        }
    }

    private RumbleCommandResult ShutdownCore()
    {
        RumbleCommandResult result = RumbleCommandResult.NotSent;
        try
        {
            Interlocked.Exchange(ref _acceptCommands, 0);
            Interlocked.Increment(ref _pulseGeneration);
            CancellationTokenSource? pulse = Interlocked.Exchange(ref _pulseCts, null);
            CancelPulse(pulse);

            lock (_gate)
            {
                try
                {
                    uint status = EnsureOutput().SetRumble(0, 0);
                    result = RumbleCommandResult.FromStatus(status);
                    if (!result.Succeeded)
                    {
                        Log.Logger.Warning(
                            "Turning rumble off returned NTSTATUS 0x{Status:X8}",
                            status);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "Failed to turn rumble off");
                    result = RumbleCommandResult.FromException(ex);
                }
                finally
                {
                    _output?.Dispose();
                    _output = null;
                }
            }

            pulse?.Dispose();
            return result.Sent
                ? result
                : RumbleCommandResult.FromException(
                    new InvalidOperationException("The rumble-off request did not reach the driver."));
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Rumble tester shutdown failed");
            return result.Sent ? result : RumbleCommandResult.FromException(ex);
        }
    }

    private bool IsCurrentPulse(int generation)
    {
        return !IsShutdown && generation == Volatile.Read(ref _pulseGeneration);
    }

    private IRumbleOutput EnsureOutput()
    {
        return _output ??= _outputFactory();
    }

    private void ApplyResult(RumbleCommandResult result, string successText)
    {
        if (!result.Sent)
        {
            return;
        }

        if (result.Error is not null)
        {
            StatusSeverity = InfoBarSeverity.Error;
            StatusText = result.Error;
            return;
        }

        if (!PowerOffUsbResult.IsNtSuccess(result.Status))
        {
            if (IsOutputStalled && !string.IsNullOrEmpty(OutputStallNote))
            {
                StatusSeverity = InfoBarSeverity.Warning;
                StatusText = OutputStallNote;
                return;
            }

            StatusSeverity = InfoBarSeverity.Error;
            StatusText = RumbleTesterStatus.Rejected(result.Status);
            return;
        }

        StatusSeverity = InfoBarSeverity.Informational;
        StatusText = successText;
    }

    private bool QueryOutputStalled()
    {
        if (_readOutputStalled is null)
        {
            return false;
        }

        try
        {
            return _readOutputStalled();
        }
        catch (Exception ex)
        {
            Log.Logger.Debug(ex, "Failed to read output stall status");
            return false;
        }
    }

    private void PublishOutputStall(bool stalled)
    {
        Publish(() =>
        {
            if (IsShutdown)
            {
                return;
            }

            IsOutputStalled = stalled;
        });
    }

    private void StopOutputStallPoll()
    {
        Timer? poll = Interlocked.Exchange(ref _outputStallPoll, null);
        poll?.Dispose();
    }

    private void Publish(Action apply)
    {
        System.Windows.Threading.Dispatcher? dispatcher = TryGetDispatcher();
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            apply();
            return;
        }

        _ = dispatcher.BeginInvoke(apply);
    }

    private static System.Windows.Threading.Dispatcher? TryGetDispatcher()
    {
        try
        {
            return Application.Current?.Dispatcher;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void CancelPulse(CancellationTokenSource? pulse)
    {
        if (pulse is null)
        {
            return;
        }

        try
        {
            pulse.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
