using System.Diagnostics;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

internal sealed record CemuhookIpcBinding(string ControllerKey, string InstanceId, int Slot);

/// <summary>One blocking IPC reader, independent of the shared UDP listener.</summary>
internal sealed class CemuhookMotionReader : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private bool _disposed;
    private Ds3MotionSnapshot _latest = CemuhookPadState.Inactive;
    public Ds3MotionSnapshot LatestSnapshot => Volatile.Read(ref _latest);
    public Task Completion { get; }

    public CemuhookMotionReader(string identity, MotionControlsSettings settings,
        Func<CemuhookIpcBinding?> resolve, Func<CemuhookIpcBinding, ICemuhookMotionSource> open,
        Action<Ds3MotionSnapshot> publish)
    {
        CancellationToken token = _cts.Token;
        Completion = Task.Factory.StartNew(() =>
        {
            try { Run(identity, settings, resolve, open, publish, token); }
            finally
            {
                Volatile.Write(ref _latest, CemuhookPadState.Inactive);
                try { publish(CemuhookPadState.Inactive); }
                finally { lock (_gate) { _disposed = true; _cts.Dispose(); } }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        _ = Completion.ContinueWith(task => Log.Logger.Error(task.Exception, "Cemuhook motion reader failed."),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void Run(string identity, MotionControlsSettings settings, Func<CemuhookIpcBinding?> resolve,
        Func<CemuhookIpcBinding, ICemuhookMotionSource> open, Action<Ds3MotionSnapshot> publish, CancellationToken token)
    {
        ICemuhookMotionSource? source = null;
        CemuhookIpcBinding? active = null;
        var processor = new CemuhookMotionProcessor(settings);
        long lastRead = Stopwatch.GetTimestamp();
        void Invalidate()
        {
            source?.Dispose(); source = null; active = null; processor.Reset();
            Volatile.Write(ref _latest, CemuhookPadState.Inactive);
            publish(CemuhookPadState.Inactive);
        }
        try
        {
            while (!token.IsCancellationRequested)
            {
                long readStart = Stopwatch.GetTimestamp();
                CemuhookIpcBinding? before = resolve();
                if (before == null || before.ControllerKey != identity || before.Slot is < 1 or > 255)
                {
                    if (active != null) Invalidate();
                }
                else
                {
                    if (active != before)
                    {
                        Invalidate();
                        source = open(before);
                        active = before;
                    }
                    bool received = source!.TryRead(out var sample, TimeSpan.FromMilliseconds(100));
                    // The slot may have moved or been reassigned while the read was blocked.
                    if (resolve() != before)
                    {
                        Invalidate();
                        continue;
                    }
                    if (received && sample.SlotIndex == before.Slot && processor.TryProcess(in sample, out var snapshot))
                    {
                        if (token.IsCancellationRequested) break;
                        Volatile.Write(ref _latest, snapshot);
                        publish(snapshot);
                        lastRead = Stopwatch.GetTimestamp();
                        continue;
                    }
                }
                if (LatestSnapshot.Connected && Stopwatch.GetElapsedTime(lastRead).TotalSeconds >= 2)
                {
                    Volatile.Write(ref _latest, CemuhookPadState.Inactive);
                    publish(CemuhookPadState.Inactive); processor.Reset();
                }
                TimeSpan remaining = TimeSpan.FromMilliseconds(10) - Stopwatch.GetElapsedTime(readStart);
                if (remaining > TimeSpan.Zero) token.WaitHandle.WaitOne(remaining);
            }
        }
        finally { source?.Dispose(); }
    }

    public void Dispose()
    {
        lock (_gate) if (!_disposed) _cts.Cancel();
        try { Completion.GetAwaiter().GetResult(); }
        catch (Exception) { /* Completion and the logging observer retain the failure. */ }
    }
}
