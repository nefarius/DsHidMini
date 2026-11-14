using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.Services;
using Nefarius.DsHidMini.IPC.Models.Public;
using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookSharedControllerTests
{
    private static CemuhookControllerIdentity Identity(int index) => CemuhookControllerIdentity.Create($"0011223344{index:X2}", $"PAD-{index}");
    private static Ds3MotionSnapshot Motion(double value) => new(true, value, 0, -1, value * 10, 0, 0, (ulong)Stopwatch.GetTimestamp());
    private static DsMotionSnapshot Input(uint slot, double value, uint index = 1, double seconds = 1)
    {
        var sample = CemuhookMotionProcessorTests.Sample(index, seconds);
        sample.SlotIndex = slot; sample.AccelMilliGX = (int)(value * 1000);
        return sample;
    }

    [Fact]
    public void RegistryAllocatesFourDistinctStableIdentitiesAndRejectsAFifthActiveController()
    {
        var registry = new CemuhookControllerRegistry();
        for (int i = 0; i < 4; i++)
        {
            var identity = Identity(i);
            var lease = registry.Attach(identity, 1);
            Assert.Equal(i, lease.Slot);
            registry.Publish(lease, Motion(i));
            Assert.Equal(identity.Mac, registry.Get((byte)i).Mac);
        }
        Assert.Throws<InvalidOperationException>(() => registry.Attach(Identity(4), 1));
        for (byte i = 0; i < 4; i++) Assert.Equal((double)i, registry.Get(i).Motion.AccelXG);
    }

    [Fact]
    public void ReconnectKeepsTheDsuSlotAndRejectsOldPublicationsAndOldDetachCalls()
    {
        var registry = new CemuhookControllerRegistry();
        var old = registry.Attach(Identity(1), 1, 3);
        registry.Publish(old, Motion(1)); registry.Detach(old);
        var current = registry.Attach(Identity(1), 2);
        Assert.Equal(old.Slot, current.Slot);
        Assert.False(registry.Get(current.Slot).Motion.Connected);
        Assert.False(registry.Publish(old, Motion(99)));
        registry.Publish(current, Motion(2)); registry.Detach(old);
        Assert.True(registry.Get(current.Slot).Motion.Connected);
        Assert.Equal((byte)2, registry.Get(current.Slot).ConnectionType);
        Assert.Equal(Identity(1).Mac, registry.Get(current.Slot).Mac);
    }

    [Fact]
    public void ReusedDsuSlotCannotReceiveAFormerOwnersData()
    {
        var registry = new CemuhookControllerRegistry();
        var a = registry.Attach(Identity(1), 1, 0);
        registry.Detach(a);
        var b = registry.Attach(Identity(2), 1, 0);
        Assert.Equal(a.Slot, b.Slot);
        registry.Publish(b, Motion(2));
        Assert.False(registry.Publish(a, Motion(99)));
        registry.Detach(a);
        Assert.Equal(Identity(2).Mac, registry.Get(0).Mac);
        Assert.Equal(2, registry.Get(0).Motion.AccelXG);
    }

    [Fact]
    public void IdentityNormalizationAndFallbackAreStableAcrossRuns()
    {
        Assert.Equal(CemuhookControllerIdentity.Create("00:11:22:33:44:55", "USB"),
            CemuhookControllerIdentity.Create("001122334455", "BLUETOOTH"));
        var fallback = CemuhookControllerIdentity.Create(null, "instance-a");
        Assert.Equal(fallback, CemuhookControllerIdentity.Create(null, "INSTANCE-A"));
        Assert.NotEqual(fallback.Mac, CemuhookControllerIdentity.Create(null, "INSTANCE-B").Mac);
        Assert.Equal(2UL, (fallback.Mac >> 40) & 3);
    }

    [Fact]
    public async Task ControllerDiscoveryReportsAllFourSlotsWithDistinctMacsAndConnectionTypes()
    {
        var registry = new CemuhookControllerRegistry();
        for (int i = 0; i < 3; i++) registry.Publish(registry.Attach(Identity(i), i == 1 ? (byte)2 : (byte)1), Motion(i));
        using var server = new CemuhookServer(0, "127.0.0.1", registry.Get);
        using var stop = new CancellationTokenSource();
        using var client = Client();
        Task run = server.RunAsync(stop.Token);
        try
        {
            await client.SendAsync(Packet(DsuPacket.MessageControllerInfo, [4, 0, 0, 0, 0, 1, 2, 3]), server.LocalEndPoint);
            var replies = new Dictionary<byte, byte[]>();
            for (int i = 0; i < 4; i++) { byte[] reply = await Receive(client); replies[reply[20]] = reply; }
            for (byte slot = 0; slot < 3; slot++)
            {
                Assert.Equal(32, replies[slot].Length);
                Assert.Equal((byte)2, replies[slot][21]);
                Assert.Equal(Identity(slot).Mac, Mac(replies[slot]));
                Assert.Equal(slot == 1 ? (byte)2 : (byte)1, replies[slot][23]);
            }
            Assert.Equal((byte)0, replies[3][21]);
            Assert.Equal(0UL, Mac(replies[3]));
        }
        finally { stop.Cancel(); await run; }
    }

    [Fact]
    public async Task SlotRegistrationsAccumulateAndCombinedFlagsSelectTheUnion()
    {
        var registry = new CemuhookControllerRegistry();
        for (int i = 0; i < 4; i++) registry.Publish(registry.Attach(Identity(i), 1), Motion(i));
        using var server = new CemuhookServer(0, "127.0.0.1", registry.Get);
        using var stop = new CancellationTokenSource(); using var client = Client();
        Task run = server.RunAsync(stop.Token);
        try
        {
            await client.SendAsync(Subscribe(1, 1), server.LocalEndPoint);
            Assert.Equal((byte)1, (await Receive(client))[20]);
            await client.SendAsync(Subscribe(3, 3, Identity(0).Mac), server.LocalEndPoint);
            var slots = new HashSet<byte>();
            for (int i = 0; i < 3; i++) slots.Add((await Receive(client))[20]);
            Assert.True(slots.SetEquals([0, 1, 3]));
            server.NotifySnapshotAvailable(2);
            await Quiet(client);
            server.NotifySnapshotAvailable(3);
            Assert.Equal((byte)3, (await Receive(client))[20]);
        }
        finally { stop.Cancel(); await run; }
    }

    [Fact]
    public async Task MacSubscriptionDoesNotFollowSlotReuseButFollowsTheSameControllerToAnotherDsuSlot()
    {
        var registry = new CemuhookControllerRegistry();
        var a = registry.Attach(Identity(1), 1, 0); registry.Publish(a, Motion(1));
        using var server = new CemuhookServer(0, "127.0.0.1", registry.Get);
        using var stop = new CancellationTokenSource(); using var client = Client();
        Task run = server.RunAsync(stop.Token);
        try
        {
            await client.SendAsync(Subscribe(2, 0, Identity(1).Mac), server.LocalEndPoint);
            Assert.Equal(Identity(1).Mac, Mac(await Receive(client)));
            registry.Detach(a);
            var b = registry.Attach(Identity(2), 1, 0); registry.Publish(b, Motion(2));
            server.NotifySnapshotAvailable(0); await Quiet(client);
            var returned = registry.Attach(Identity(1), 2, 2); registry.Publish(returned, Motion(3));
            server.NotifySnapshotAvailable(returned.Slot);
            byte[] response = await Receive(client);
            Assert.Equal((byte)2, response[20]); Assert.Equal(Identity(1).Mac, Mac(response));
        }
        finally { stop.Cancel(); await run; }
    }

    [Fact]
    public async Task FourControllersUseOneListenerAndDisconnectingOneDoesNotRestartTheOthers()
    {
        var endpoint = Endpoint(); var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        var preferences = new Dictionary<string, int>();
        using var shared = Shared(endpoint, sources, preferences);
        object[] owners = Enumerable.Range(0, 4).Select(_ => new object()).ToArray();
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            var binding = new CemuhookIpcBinding(Identity(i).Key, $"INSTANCE-{i}", i + 5);
            sources[binding] = new();
            shared.UpdateController(owners[i], Identity(i), true, 1, new() { KalmanFilterStrength = 0 }, () => binding);
            sources[binding].Enqueue(Input((uint)binding.Slot, index + 1));
        }
        await CemuhookTestSource.Eventually(() => Enumerable.Range(0, 4).All(i => shared.GetPad((byte)i).Motion.Connected));
        for (int i = 0; i < 4; i++) Assert.Equal(i + 1, shared.GetPad((byte)i).Motion.AccelXG);
        using var client = Client();
        await client.SendAsync(Subscribe(1, 1), new IPEndPoint(IPAddress.Loopback, endpoint.Port));
        byte[] before = await Receive(client);
        shared.RemoveController(owners[0]);
        Assert.False(shared.GetPad(0).Motion.Connected);
        Assert.True(shared.GetPad(1).Motion.Connected);
        var b = new CemuhookIpcBinding(Identity(1).Key, "INSTANCE-1", 6);
        sources[b].Enqueue(Input(6, 8, 2, 1.005));
        byte[] after = await Receive(client);
        Assert.Equal(BinaryPrimitives.ReadUInt32LittleEndian(before.AsSpan(12, 4)), BinaryPrimitives.ReadUInt32LittleEndian(after.AsSpan(12, 4)));
        Assert.Equal(8f, BinaryPrimitives.ReadSingleLittleEndian(after.AsSpan(76, 4)));
        Assert.Equal((byte)1, after[20]);
        Assert.Null(shared.GetError(owners[1]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetainedControllerReaderTracksIpcSlotChangesAndDropsAnInFlightOldSample(bool sameDeviceInstance)
    {
        var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        var first = new CemuhookIpcBinding("A", "USB-A", 1);
        var next = new CemuhookIpcBinding("A", sameDeviceInstance ? first.InstanceId : "BT-A", 7);
        CemuhookIpcBinding? binding = first;
        sources[first] = new(); sources[next] = new();
        var published = new ConcurrentQueue<Ds3MotionSnapshot>();
        using var reader = new CemuhookMotionReader("A", new() { KalmanFilterStrength = 0 },
            () => Volatile.Read(ref binding), value => sources[value], published.Enqueue);
        sources[first].Enqueue(Input(1, 1));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        sources[first].OnRead(() => { Volatile.Write(ref binding, next); return Input(1, 99, 2, 1.005); });
        sources[next].Enqueue(Input(7, 2));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected && reader.LatestSnapshot.AccelXG == 2);
        Assert.True(sources[first].IsDisposed);
        Assert.DoesNotContain(published, snapshot => snapshot.AccelXG == 99);
        Assert.Equal(0, reader.LatestSnapshot.PitchDegreesPerSecond);
    }

    [Fact]
    public async Task IpcSlotReusedByAnotherControllerIsNeverPublishedUnderTheOldIdentity()
    {
        var a = new CemuhookIpcBinding("A", "INSTANCE-A", 1);
        CemuhookIpcBinding? binding = a;
        using var source = new CemuhookTestSource();
        var published = new ConcurrentQueue<Ds3MotionSnapshot>(); int opened = 0;
        using var reader = new CemuhookMotionReader("A", new() { KalmanFilterStrength = 0 }, () => Volatile.Read(ref binding),
            _ => { Interlocked.Increment(ref opened); return source; }, published.Enqueue);
        source.Enqueue(Input(1, 1));
        await CemuhookTestSource.Eventually(() => reader.LatestSnapshot.Connected);
        source.OnRead(() => { Volatile.Write(ref binding, new("B", "INSTANCE-B", 1)); return Input(1, 99, 2, 1.005); });
        await CemuhookTestSource.Eventually(() => source.IsDisposed && !reader.LatestSnapshot.Connected);
        Assert.False(reader.LatestSnapshot.Connected);
        Assert.DoesNotContain(published, snapshot => snapshot.AccelXG == 99);
        Assert.Equal(1, Volatile.Read(ref opened));
    }

    [Fact]
    public async Task DisconnectAndReconnectKeepPreferredSlotAndStableMacWithFreshFilterState()
    {
        var endpoint = Endpoint(); var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        var preferences = new Dictionary<string, int> { [Identity(1).Key] = 3 };
        using var shared = Shared(endpoint, sources, preferences);
        var first = new CemuhookIpcBinding(Identity(1).Key, "USB-A", 1);
        CemuhookIpcBinding? binding = first;
        sources[first] = new(); object owner = new();
        shared.UpdateController(owner, Identity(1), true, 1, new(), () => Volatile.Read(ref binding));
        sources[first].Enqueue(Input(1, 1));
        await CemuhookTestSource.Eventually(() => shared.GetPad(3).Motion.Connected);
        Volatile.Write(ref binding, null);
        await CemuhookTestSource.Eventually(() => !shared.GetPad(3).Motion.Connected);
        await CemuhookTestSource.Eventually(() => sources[first].IsDisposed);
        shared.RemoveController(owner);
        var next = new CemuhookIpcBinding(Identity(1).Key, "BT-A", 9);
        sources[next] = new(); Volatile.Write(ref binding, next);
        object newOwner = new();
        shared.UpdateController(newOwner, Identity(1), true, 2, new(), () => Volatile.Read(ref binding));
        sources[next].Enqueue(Input(9, 2));
        await CemuhookTestSource.Eventually(() => shared.GetPad(3).Motion.Connected);
        Assert.Equal(3, shared.GetSlot(newOwner)); Assert.Equal(Identity(1).Mac, shared.GetPad(3).Mac);
        Assert.Equal((byte)2, shared.GetPad(3).ConnectionType); Assert.Equal(2, shared.GetPad(3).Motion.AccelXG);
        Assert.Equal(0, shared.GetPad(3).Motion.PitchDegreesPerSecond);
        shared.RemoveController(owner);
        Assert.True(shared.GetPad(3).Motion.Connected);
    }

    [Fact]
    public async Task ChangingSharedEndpointKeepsReadersAndSlotsAndReleasesTheOldPort()
    {
        var endpoint = Endpoint(); int oldPort = endpoint.Port;
        var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        using var shared = Shared(endpoint, sources, new());
        var binding = new CemuhookIpcBinding(Identity(1).Key, "A", 8); sources[binding] = new();
        object owner = new(); shared.UpdateController(owner, Identity(1), true, 1, new(), () => binding);
        sources[binding].Enqueue(Input(8, 1));
        await CemuhookTestSource.Eventually(() => shared.GetPad(0).Motion.Connected);
        endpoint.Port = Endpoint().Port; shared.UpdateEndpoint();
        Assert.Equal(0, shared.GetSlot(owner)); Assert.False(sources[binding].IsDisposed);
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, oldPort));
        using var client = Client(); await client.SendAsync(Subscribe(0, 0), new IPEndPoint(IPAddress.Loopback, endpoint.Port));
        Assert.Equal(Identity(1).Mac, Mac(await Receive(client)));
    }

    [Fact]
    public void SharedEndpointMigrationAndPreferredSlotsSurviveSerializationAndRollback()
    {
        string root = Path.Combine(Path.GetTempPath(), "dshm-shared-" + Guid.NewGuid().ToString("N"));
        var data = new DshmConfigManagerUserData { SchemaVersion = DshmConfigManagerUserData.CurrentSchemaVersion };
        data.Devices.Add(new("B") { Settings = new() { CemuhookUdp = new() { Enabled = true, Port = 26762 } } });
        data.Devices.Add(new("A") { Settings = new() { CemuhookUdp = new() { Enabled = true, IpAddress = "0.0.0.0", Port = 26761 } } });
        using var config = new DshmConfigManager(data, new(root, root));
        var endpoint = config.SharedCemuhookUdp;
        Assert.Equal(26761, endpoint.Port); Assert.Equal("0.0.0.0", endpoint.IpAddress);
        Assert.All(data.Devices, device => Assert.True(device.Settings.CemuhookUdp.Enabled));
        data.CemuhookSlots[Identity(1).Key] = 3;
        string snapshot = JsonConvert.SerializeObject(data);
        endpoint.Port = 1234; data.CemuhookSlots.Clear();
        data.RestoreFromSnapshot(snapshot);
        Assert.Same(endpoint, config.SharedCemuhookUdp); Assert.Equal(26761, endpoint.Port);
        Assert.Equal(3, config.GetCemuhookSlot(Identity(1).Key));
    }

    [Fact]
    public async Task FailedReaderCanRestartInItsExistingDsuSlotWithoutAffectingTheOtherController()
    {
        var endpoint = Endpoint();
        var sources = new ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource>();
        using var shared = Shared(endpoint, sources, new());
        var a = new CemuhookIpcBinding(Identity(1).Key, "A", 1);
        var b = new CemuhookIpcBinding(Identity(2).Key, "B", 2);
        sources[a] = new(); sources[b] = new();
        object ownerA = new(), ownerB = new();
        shared.UpdateController(ownerA, Identity(1), true, 1, new(), () => a);
        shared.UpdateController(ownerB, Identity(2), true, 1, new(), () => b);
        sources[a].Enqueue(Input(1, 1)); sources[b].Enqueue(Input(2, 2));
        await CemuhookTestSource.Eventually(() => shared.GetPad(0).Motion.Connected && shared.GetPad(1).Motion.Connected);
        sources[a].Fail(new InvalidOperationException("reader lost IPC"));
        await CemuhookTestSource.Eventually(() => shared.GetError(ownerA)?.Contains("reader lost IPC") == true);
        Assert.False(shared.GetPad(0).Motion.Connected);
        Assert.True(shared.GetPad(1).Motion.Connected);
        Assert.False(sources[b].IsDisposed);
        sources[a] = new();
        shared.UpdateController(ownerA, Identity(1), true, 1, new(), () => a);
        sources[a].Enqueue(Input(1, 3));
        await CemuhookTestSource.Eventually(() => shared.GetPad(0).Motion.Connected);
        Assert.Equal(0, shared.GetSlot(ownerA)); Assert.Equal(1, shared.GetSlot(ownerB));
        Assert.Equal(3, shared.GetPad(0).Motion.AccelXG);
        Assert.Null(shared.GetError(ownerA));
    }

    [Fact]
    public async Task FailedSharedListenerCanRetryTheSameEndpoint()
    {
        var endpoint = Endpoint(); int starts = 0;
        var failure = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new CemuhookBridgeHost(settings =>
        {
            bool fail = ++starts == 1;
            return new(settings.Port, settings.IpAddress,
                slot => fail ? throw new InvalidOperationException("UDP worker failed") : CemuhookPadState.Empty(slot));
        });
        host.StatusChanged += (_, _) =>
        {
            if (host.ErrorMessage?.Contains("UDP worker failed") == true) failure.TrySetResult();
        };
        host.Update(endpoint, true);
        await failure.Task.WaitAsync(TimeSpan.FromSeconds(5));
        host.Update(endpoint, true);
        Assert.Null(host.ErrorMessage); Assert.Equal(2, starts);
        using var client = Client();
        await client.SendAsync(Packet(DsuPacket.MessageVersion, []), new IPEndPoint(IPAddress.Loopback, endpoint.Port));
        Assert.Equal(DsuPacket.ProtocolVersion, BinaryPrimitives.ReadUInt16LittleEndian((await Receive(client)).AsSpan(20, 2)));
        host.Dispose();
        using var rebound = new UdpClient(new IPEndPoint(IPAddress.Loopback, endpoint.Port));
    }
    private static CemuhookBridgeService Shared(CemuhookUdpSettings endpoint,
        ConcurrentDictionary<CemuhookIpcBinding, CemuhookTestSource> sources, Dictionary<string, int> preferences) =>
        new(() => endpoint, key => preferences.TryGetValue(key, out int slot) ? slot : null,
            (key, slot) => preferences[key] = slot, binding => sources[binding]);
    private static CemuhookUdpSettings Endpoint()
    {
        using var socket = Client();
        return new() { Enabled = true, Port = ((IPEndPoint)socket.Client.LocalEndPoint!).Port };
    }
    private static UdpClient Client() => new(new IPEndPoint(IPAddress.Loopback, 0));
    private static byte[] Packet(uint type, byte[] payload) => CemuhookServerLifecycleTests.ClientPacket(type, payload);
    private static byte[] Subscribe(byte flags, byte slot, ulong mac = 0)
    {
        var payload = new byte[8]; payload[0] = flags; payload[1] = slot;
        for (int i = 0; i < 6; i++) payload[2 + i] = (byte)(mac >> ((5 - i) * 8));
        return Packet(DsuPacket.MessageControllerData, payload);
    }
    private static async Task<byte[]> Receive(UdpClient client)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return (await client.ReceiveAsync(timeout.Token)).Buffer;
    }
    private static async Task Quiet(UdpClient client)
    {
        using var timeout = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReceiveAsync(timeout.Token).AsTask());
    }
    private static ulong Mac(byte[] packet)
    {
        ulong result = 0; foreach (byte value in packet.AsSpan(24, 6)) result = (result << 8) | value; return result;
    }
}
