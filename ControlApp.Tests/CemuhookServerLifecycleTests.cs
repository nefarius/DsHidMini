using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using Nefarius.DsHidMini.ControlApp.Models.Motion;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookServerLifecycleTests
{
    [Fact]
    public async Task SendWorkerFailureStopsReceiveWorkerAndReleasesSocket()
    {
        using var server = new CemuhookServer(0, "127.0.0.1", () => throw new InvalidOperationException("send worker"));
        var endpoint = server.LocalEndPoint;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => server.RunAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("send worker", error.Message);
        using var rebound = new UdpClient(endpoint);
    }

    [Fact]
    public async Task ClosingPendingReceiveStopsBothWorkersAndReleasesSocket()
    {
        using var server = new CemuhookServer(0, "127.0.0.1", () => new(false, 0, 0, 0, 0, 0, 0));
        var endpoint = server.LocalEndPoint;
        Task run = server.RunAsync(CancellationToken.None);
        // Close the pending receive without cancelling the caller's token.
        server.Dispose();
        Exception? error = await Record.ExceptionAsync(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        if (error != null)
        {
            // Windows can report disposal of a pending receive as an aborted socket operation.
            var socketError = Assert.IsType<SocketException>(error);
            Assert.Equal(SocketError.OperationAborted, socketError.SocketErrorCode);
        }
        using var rebound = new UdpClient(endpoint);
    }

    [Fact]
    public async Task CancellationStopsBothWorkersAndReleasesSocket()
    {
        using var cts = new CancellationTokenSource();
        using var server = new CemuhookServer(0, "127.0.0.1", () => new(false, 0, 0, 0, 0, 0, 0));
        var endpoint = server.LocalEndPoint;
        Task run = server.RunAsync(cts.Token);
        cts.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        using var rebound = new UdpClient(endpoint);
    }

    [Fact]
    public async Task RetransmissionPreservesAcquisitionTimestamp()
    {
        ulong qpc = (ulong)Stopwatch.Frequency * 123;
        var sample = new Ds3MotionSnapshot(true, 0.25, 0.5, -1, -180, 90, 45, qpc);
        using var cts = new CancellationTokenSource();
        using var server = new CemuhookServer(0, "127.0.0.1", () => sample);
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Task run = server.RunAsync(cts.Token);
        try
        {
            await client.SendAsync(ClientPacket(DsuPacket.MessageControllerData, new byte[8]), server.LocalEndPoint);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var first = await client.ReceiveAsync(timeout.Token);
            // No fresh input means no timer-driven duplicate packets.
            using (var quiet = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReceiveAsync(quiet.Token).AsTask());
            server.NotifySnapshotAvailable();
            var second = await client.ReceiveAsync(timeout.Token);
            Assert.Equal(123_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(first.Buffer.AsSpan(68, 8)));
            Assert.Equal(123_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(second.Buffer.AsSpan(68, 8)));
            Assert.Equal(0.25f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(76, 4)));
            Assert.Equal(-1f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(80, 4)));
            Assert.Equal(-0.5f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(84, 4)));
            Assert.Equal(90f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(88, 4)));
            Assert.Equal(-180f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(92, 4)));
            Assert.Equal(-45f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(96, 4)));
            Assert.NotEqual(
                BinaryPrimitives.ReadUInt32LittleEndian(first.Buffer.AsSpan(32, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(second.Buffer.AsSpan(32, 4)));
        }
        finally
        {
            cts.Cancel();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task PendingNotificationsAreCoalescedToTheLatestSample()
    {
        int reads = 0;
        var latest = new Ds3MotionSnapshot(false, 0, 0, 0, 0, 0, 0);
        Ds3MotionSnapshot? consumed = null;
        using var cts = new CancellationTokenSource();
        using var server = new CemuhookServer(0, "127.0.0.1", () =>
        {
            Interlocked.Increment(ref reads);
            consumed = latest;
            return latest;
        });
        for (int i = 0; i < 1000; i++)
        {
            latest = latest with { AccelXG = i };
            server.NotifySnapshotAvailable();
        }
        Task run = server.RunAsync(cts.Token);
        try
        {
            Assert.Equal(1, Volatile.Read(ref reads));
            Assert.Same(latest, consumed);
        }
        finally
        {
            cts.Cancel();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task ClientsReceiveTheSameSerializedSample()
    {
        var latest = new Ds3MotionSnapshot(true, 0, 0, -1, 0, 0, 0, (ulong)Stopwatch.Frequency);
        using var cts = new CancellationTokenSource();
        using var server = new CemuhookServer(0, "127.0.0.1", () => Volatile.Read(ref latest));
        using var firstClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        using var secondClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Task run = server.RunAsync(cts.Token);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            byte[] subscription = ClientPacket(DsuPacket.MessageControllerData, new byte[8]);
            await firstClient.SendAsync(subscription, server.LocalEndPoint);
            await firstClient.ReceiveAsync(timeout.Token);
            await secondClient.SendAsync(subscription, server.LocalEndPoint);
            await secondClient.ReceiveAsync(timeout.Token);
            await firstClient.ReceiveAsync(timeout.Token);

            Volatile.Write(ref latest, latest with { AccelXG = 0.75, TimestampQpc = (ulong)Stopwatch.Frequency * 2 });
            server.NotifySnapshotAvailable();
            var first = await firstClient.ReceiveAsync(timeout.Token);
            var second = await secondClient.ReceiveAsync(timeout.Token);
            Assert.Equal(2_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(first.Buffer.AsSpan(68, 8)));
            Assert.Equal(0.75f, BinaryPrimitives.ReadSingleLittleEndian(first.Buffer.AsSpan(76, 4)));
            Assert.Equal(first.Buffer, second.Buffer);
        }
        finally
        {
            cts.Cancel();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static byte[] ClientPacket(uint type, byte[] payload)
    {
        byte[] packet = DsuPacket.BuildServerPacket(type, payload, 1);
        packet[3] = (byte)'C';
        packet.AsSpan(8, 4).Clear();
        uint crc = 0xFFFFFFFF;
        foreach (byte value in packet)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8, 4), ~crc);
        return packet;
    }

}
