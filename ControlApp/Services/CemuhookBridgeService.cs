using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>Consumes coherent driver motion telemetry and broadcasts filtered DSU samples.</summary>
internal sealed class CemuhookBridgeService : IDisposable
{
    private static readonly Ds3MotionSnapshot InactiveSnapshot = new(false, 0, 0, 0, 0, 0, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly CemuhookServer _server;
    private readonly object _lifetimeLock = new();
    private bool _disposed;
    private Ds3MotionSnapshot _latestSnapshot = InactiveSnapshot;

    public Ds3MotionSnapshot LatestSnapshot => Volatile.Read(ref _latestSnapshot);

    /// <summary>Completes only after both workers have stopped and the socket has been released.</summary>
    public Task Completion { get; }

    public CemuhookBridgeService(int deviceSlot, CemuhookUdpSettings udpSettings, MotionControlsSettings motionSettings)
        : this(udpSettings, motionSettings, () => new CemuhookMotionSource(deviceSlot))
    {
    }

    internal CemuhookBridgeService(
        CemuhookUdpSettings udpSettings,
        MotionControlsSettings motionSettings,
        Func<ICemuhookMotionSource> sourceFactory)
    {
        try
        {
            _server = new CemuhookServer(udpSettings.Port, udpSettings.IpAddress, () => LatestSnapshot);
        }
        catch
        {
            _cts.Dispose();
            throw;
        }

        CancellationToken token = _cts.Token;
        // The IPC API blocks synchronously for the device's lifetime. Give it a
        // dedicated background thread instead of occupying a thread-pool worker.
        Task producer = Task.Factory.StartNew(
            () => RunBridge(sourceFactory, motionSettings, token),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        Task server = Task.Run(() => _server.RunAsync(token));
        Completion = SuperviseAsync(producer, server);
        // Observe background failures even before the owner next checks Completion.
        _ = Completion.ContinueWith(
            task => Log.Logger.Error(task.Exception, "Cemuhook bridge worker faulted."),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task SuperviseAsync(Task producer, Task server)
    {
        try
        {
            await Task.WhenAny(producer, server).ConfigureAwait(false);
            RequestStop();
            await Task.WhenAll(producer, server).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
        finally
        {
            Volatile.Write(ref _latestSnapshot, InactiveSnapshot);
            _server.Dispose();
            lock (_lifetimeLock)
            {
                _disposed = true;
                _cts.Dispose();
            }
        }
    }

    private void RequestStop()
    {
        lock (_lifetimeLock)
        {
            if (!_disposed)
                _cts.Cancel();
        }
    }

    public void Dispose()
    {
        RequestStop();
        try
        {
            Completion.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Completion exposes the failure; the observer above logs it.
        }
    }

    private void RunBridge(
        Func<ICemuhookMotionSource> sourceFactory,
        MotionControlsSettings motionSettings,
        CancellationToken cancellationToken)
    {
        using ICemuhookMotionSource source = sourceFactory();
        var processor = new CemuhookMotionProcessor(motionSettings);
        long lastRead = Stopwatch.GetTimestamp();
        while (!cancellationToken.IsCancellationRequested)
        {
            long readStart = Stopwatch.GetTimestamp();
            if (source.TryRead(out var sample, TimeSpan.FromMilliseconds(100)) &&
                processor.TryProcess(in sample, out var snapshot))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                lastRead = Stopwatch.GetTimestamp();
                Volatile.Write(ref _latestSnapshot, snapshot);
                _server.NotifySnapshotAvailable();
                continue;
            }

            if (Stopwatch.GetElapsedTime(lastRead).TotalSeconds >= 2 && LatestSnapshot.Connected)
            {
                Volatile.Write(ref _latestSnapshot, InactiveSnapshot);
                _server.NotifySnapshotAvailable();
                processor.Reset();
            }

            // Empty slots and missing events can return immediately rather than wait.
            TimeSpan remaining = TimeSpan.FromMilliseconds(10) - Stopwatch.GetElapsedTime(readStart);
            if (remaining > TimeSpan.Zero)
                cancellationToken.WaitHandle.WaitOne(remaining);
        }
    }
}
