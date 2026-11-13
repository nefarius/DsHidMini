using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookBridgeLifecycleTests
{
    [Fact]
    public async Task StartupFailureCompletesAndReleasesTheListener()
    {
        var udp = Settings();
        using var service = new CemuhookBridgeService(udp, new(),
            () => throw new InvalidOperationException("source startup failed"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("source startup failed", error.Message);
        Assert.False(service.LatestSnapshot.Connected);
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, udp.Port));
    }

    [Fact]
    public async Task ProducerFailureClearsTheSnapshotDisposesTheSourceAndReleasesTheListener()
    {
        using var source = new ScriptedSource();
        var udp = Settings();
        using var service = new CemuhookBridgeService(udp, new(), () => source);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(1, 1));
        await Eventually(() => service.LatestSnapshot.Connected);
        source.Fail(new InvalidOperationException("producer failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(source.IsDisposed);
        Assert.False(service.LatestSnapshot.Connected);
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, udp.Port));
    }

    [Fact]
    public async Task MissingTelemetryDoesNotPublishNominalCalibrationOrAdvanceTheSample()
    {
        using var source = new ScriptedSource();
        using var service = new CemuhookBridgeService(Settings(), new() { KalmanFilterStrength = 0 }, () => source);
        var sample = CemuhookMotionProcessorTests.Sample(1, 1);
        sample.AccelMilliGX = 321;
        sample.GyroMilliDps = 12345;
        source.Enqueue(sample);
        await Eventually(() => service.LatestSnapshot.Connected);
        var published = service.LatestSnapshot;
        int reads = source.Reads;
        // Let the producer encounter several failed reads after the successful sample.
        await Eventually(() => source.Reads >= reads + 3);
        Assert.Same(published, service.LatestSnapshot);
        Assert.Equal(0.321, published.AccelXG);
        Assert.Equal(12.345, published.GyroAxisDegreesPerSecond);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(2, 1.005));
        await Eventually(() => service.LatestSnapshot.TimestampQpc != sample.TimestampQpc);
        Assert.True(service.LatestSnapshot.Connected);
    }

    [Fact]
    public async Task InputOutageExpiresTheSampleAndCanRecoverWithoutRestarting()
    {
        using var source = new ScriptedSource();
        using var service = new CemuhookBridgeService(Settings(), new(), () => source);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(5, 1));
        await Eventually(() => service.LatestSnapshot.Connected);
        await Eventually(() => !service.LatestSnapshot.Connected);
        Assert.False(service.Completion.IsCompleted);
        source.Enqueue(CemuhookMotionProcessorTests.Sample(1, 4));
        await Eventually(() => service.LatestSnapshot.Connected);
        Assert.Equal(0, service.LatestSnapshot.PitchDegreesPerSecond);
        Assert.Equal(0, service.LatestSnapshot.RollDegreesPerSecond);
    }

    [Fact]
    public async Task DisposeWaitsForWorkersAndIsSafeToRepeat()
    {
        using var source = new ScriptedSource();
        var udp = Settings();
        var service = new CemuhookBridgeService(udp, new(), () => source);
        await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Run(service.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(service.Completion.IsCompletedSuccessfully);
        Assert.True(source.IsDisposed);
        Assert.False(service.LatestSnapshot.Connected);
        service.Dispose();
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, udp.Port));
    }

    [Fact]
    public async Task ImmediateReadFailuresAreThrottledAndRemainCancellable()
    {
        using var source = new ScriptedSource(immediateMisses: true);
        using var service = new CemuhookBridgeService(Settings(), new(), () => source);
        await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(150);
        Assert.InRange(source.Reads, 1, 100);
        await Task.Run(service.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(source.IsDisposed);
    }

    [Fact]
    public async Task HostRecreatesAFailedBridgeAtTheUnchangedEndpoint()
    {
        var services = new List<CemuhookBridgeService>();
        var sources = new List<ScriptedSource>();
        using var host = new CemuhookBridgeHost((udp, motion) =>
        {
            var source = new ScriptedSource();
            sources.Add(source);
            var service = new CemuhookBridgeService(udp, motion, () => source);
            services.Add(service);
            return service;
        });
        var settings = Settings();
        host.Update(settings, new());
        sources[0].Fail(new InvalidOperationException("device read failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => services[0].Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        host.Update(settings, new());
        Assert.Equal(2, services.Count);
        Assert.False(services[1].Completion.IsCompleted);
        Assert.True(sources[0].IsDisposed);
        host.Update(settings, new());
        Assert.Equal(2, services.Count);
        host.Dispose();
        Assert.True(services[1].Completion.IsCompletedSuccessfully);
        host.Update(settings, new());
        Assert.Equal(2, services.Count);
        foreach (var source in sources)
            source.Dispose();
    }

    [Fact]
    public async Task HostStopsThePreviousBridgeBeforeRebindingAndDisabling()
    {
        var services = new List<CemuhookBridgeService>();
        var sources = new List<ScriptedSource>();
        using var host = new CemuhookBridgeHost((udp, motion) =>
        {
            var source = new ScriptedSource();
            sources.Add(source);
            var service = new CemuhookBridgeService(udp, motion, () => source);
            services.Add(service);
            return service;
        });
        var settings = Settings();
        host.Update(settings, new());
        await sources[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        int firstPort = settings.Port;
        // Changing the address on the same settings instance must restart the listener.
        settings.IpAddress = "0.0.0.0";
        host.Update(settings, new());
        Assert.Equal(2, services.Count);
        Assert.True(services[0].Completion.IsCompletedSuccessfully);
        await sources[1].Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        settings.Enabled = false;
        host.Update(settings, new());
        Assert.True(services[1].Completion.IsCompletedSuccessfully);
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, firstPort));
        foreach (var source in sources)
            source.Dispose();
    }

    private static CemuhookUdpSettings Settings()
    {
        using var reservation = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return new() { Enabled = true, IpAddress = "127.0.0.1", Port = ((IPEndPoint)reservation.Client.LocalEndPoint!).Port };
    }

    private static async Task Eventually(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class ScriptedSource(bool immediateMisses = false) : ICemuhookMotionSource
    {
        private readonly BlockingCollection<Func<DsMotionSnapshot>> _samples = new();
        private int _reads;
        private int _disposed;
        public int Reads => Volatile.Read(ref _reads);
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enqueue(DsMotionSnapshot sample) => _samples.Add(() => sample);
        public void Fail(Exception error) => _samples.Add(() => throw error);

        public bool TryRead(out DsMotionSnapshot snapshot, TimeSpan timeout)
        {
            Interlocked.Increment(ref _reads);
            Entered.TrySetResult();
            if (_samples.TryTake(out var read, immediateMisses ? TimeSpan.Zero : timeout))
            {
                snapshot = read();
                return true;
            }
            snapshot = default;
            return false;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _samples.Dispose();
        }
    }
}
