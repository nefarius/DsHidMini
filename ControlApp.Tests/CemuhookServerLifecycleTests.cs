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
        var sample = new Ds3MotionSnapshot(true, 0, 0, -1, 0, 0, 0, qpc);
        using var cts = new CancellationTokenSource();
        using var server = new CemuhookServer(0, "127.0.0.1", () => sample);
        using var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Task run = server.RunAsync(cts.Token);
        try
        {
            await client.SendAsync(ClientPacket(DsuPacket.MessageControllerData, new byte[8]), server.LocalEndPoint);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var first = await client.ReceiveAsync(timeout.Token);
            var second = await client.ReceiveAsync(timeout.Token);
            Assert.Equal(123_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(first.Buffer.AsSpan(68, 8)));
            Assert.Equal(123_000_000UL, BinaryPrimitives.ReadUInt64LittleEndian(second.Buffer.AsSpan(68, 8)));
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
