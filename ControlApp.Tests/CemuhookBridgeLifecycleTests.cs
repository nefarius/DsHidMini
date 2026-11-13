using System.Collections.Concurrent;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC.Models.Public;
using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookBridgeLifecycleTests
{
    private static readonly CemuhookIpcBinding Binding = new("A", "INSTANCE-A", 1);
    private static CemuhookMotionReader Reader(CemuhookTestSource source, MotionControlsSettings? settings = null) =>
        new("A", settings ?? new(), () => Binding, _ => source, _ => { });

    [Fact]
    public async Task BlockingReaderRunsOnADedicatedBackgroundThread()
    {
        using var source = new CemuhookTestSource();
        using var reader = Reader(source);
        Thread thread = await source.ReaderThread.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(thread.IsThreadPoolThread);
        Assert.True(thread.IsBackground);
    }

    [Fact]
    public async Task StartupFailureCompletesAndClearsTheSnapshot()
    {
        using var reader = new CemuhookMotionReader("A", new(), () => Binding,
            _ => throw new InvalidOperationException("source startup failed"), _ => { });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("source startup failed", error.Message);
        Assert.False(reader.LatestSnapshot.Connected);
    }

    [Fact]
    public async Task ProducerFailureClearsTheSnapshotAndDisposesTheSource()
    {
        using var source = new CemuhookTestSource();
        using var reader = Reader(source);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(1, 1));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        source.Fail(new InvalidOperationException("producer failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(source.IsDisposed);
        Assert.False(reader.LatestSnapshot.Connected);
    }

    [Fact]
    public async Task MissingTelemetryDoesNotChangeCalibrationOrAdvanceTheSample()
    {
        using var source = new CemuhookTestSource();
        using var reader = Reader(source, new() { KalmanFilterStrength = 0 });
        var sample = CemuhookMotionProcessorTests.Sample(1, 1);
        sample.AccelMilliGX = 321; sample.GyroMilliDps = 12345;
        source.Enqueue(sample);
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        var published = reader.LatestSnapshot;
        int reads = source.Reads;
        await CemuhookTestSource.Eventually(() => source.Reads >= reads + 3);
        Assert.Same(published, reader.LatestSnapshot);
        Assert.Equal(0.321, published.AccelXG);
        Assert.Equal(12.345, published.GyroAxisDegreesPerSecond);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(2, 1.005));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.TimestampQpc != sample.TimestampQpc);
    }

    [Fact]
    public async Task InputOutageExpiresAndRecoversWithoutRestarting()
    {
        using var source = new CemuhookTestSource();
        using var reader = Reader(source);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(5, 1));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        await CemuhookTestSource.Eventually(() => !reader.LatestSnapshot.Connected);
        Assert.False(reader.Completion.IsCompleted);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(1, 4));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        Assert.Equal(0, reader.LatestSnapshot.PitchDegreesPerSecond);
        Assert.Equal(0, reader.LatestSnapshot.RollDegreesPerSecond);
    }

    [Fact]
    public async Task DisposeWaitsForTheReaderAndIsSafeToRepeat()
    {
        using var source = new CemuhookTestSource();
        var reader = Reader(source);
        await source.ReaderThread.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Run(reader.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(reader.Completion.IsCompletedSuccessfully);
        Assert.True(source.IsDisposed);
        Assert.False(reader.LatestSnapshot.Connected);
        reader.Dispose();
    }

    [Fact]
    public async Task ImmediateReadFailuresAreThrottledAndCancellable()
    {
        using var source = new CemuhookTestSource(immediateMisses: true);
        using var reader = Reader(source);
        await source.ReaderThread.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(150);
        Assert.InRange(source.Reads, 1, 100);
        await Task.Run(reader.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(source.IsDisposed);
    }
}

internal sealed class CemuhookTestSource(bool immediateMisses = false) : ICemuhookMotionSource
{
    private readonly BlockingCollection<Func<DsMotionSnapshot?>> _samples = new();
    private int _reads, _disposed;
    public int Reads => Volatile.Read(ref _reads);
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
    public TaskCompletionSource<Thread> ReaderThread { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Enqueue(DsMotionSnapshot sample) => _samples.Add(() => sample);
    public void OnRead(Func<DsMotionSnapshot?> read) => _samples.Add(read);
    public void Fail(Exception error) => _samples.Add(() => throw error);
    public bool TryRead(out DsMotionSnapshot snapshot, TimeSpan timeout)
    {
        Interlocked.Increment(ref _reads); ReaderThread.TrySetResult(Thread.CurrentThread);
        if (_samples.TryTake(out var read, immediateMisses ? TimeSpan.Zero : timeout) && read() is { } value)
        { snapshot = value; return true; }
        snapshot = default; return false;
    }
    public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0) _samples.Dispose(); }
    public static async Task Eventually(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) await Task.Delay(10, timeout.Token);
    }
}
