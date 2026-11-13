using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     UDP server for the Cemuhook DSU protocol. Accepts client connections and
///     broadcasts controller motion data at ~125 Hz.
/// </summary>
internal sealed class CemuhookServer : IDisposable
{
    private const byte DsuSlot = 0;
    private static readonly byte[] DsuMac = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x01 };
    private const int SubscriptionTimeoutSeconds = 5;

    private readonly uint _serverId = unchecked((uint)Random.Shared.NextInt64(1, int.MaxValue));
    private readonly UdpClient _udp;
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly Func<Ds3MotionSnapshot> _snapshotProvider;
    private uint _packetNumber;

    private sealed class Subscription
    {
        public required IPEndPoint Endpoint { get; init; }
        public required long LastRequest { get; set; }
        public bool AllControllers { get; init; }
        public byte Slot { get; init; }
    }

    public CemuhookServer(int port, string bindAddress, Func<Ds3MotionSnapshot> snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
        IPAddress bindIp = IPAddress.Parse(bindAddress);
        _udp = new UdpClient(new IPEndPoint(bindIp, port));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Task receiveTask = ReceiveLoopAsync(cancellationToken);
        Task sendTask = SendLoopAsync(cancellationToken);

        try
        {
            await Task.WhenAll(receiveTask, sendTask);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult received;

            try
            {
                received = await _udp.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionReset)
            {
                // ICMP Port Unreachable from a closed client; recover and continue.
                continue;
            }

            if (!DsuPacket.TryParseClientPacket(
                    received.Buffer,
                    out uint messageType,
                    out byte[] payload))
            {
                continue;
            }

            switch (messageType)
            {
                case DsuPacket.MessageVersion:
                    await SendVersionResponseAsync(received.RemoteEndPoint);
                    break;

                case DsuPacket.MessageControllerInfo:
                    await SendControllerInfoAsync(received.RemoteEndPoint, payload);
                    break;

                case DsuPacket.MessageControllerData:
                    HandleSubscription(received.RemoteEndPoint, payload);
                    break;
            }
        }
    }

    private async Task SendVersionResponseAsync(IPEndPoint endpoint)
    {
        byte[] payload = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, DsuPacket.ProtocolVersion);
        await SendPacketAsync(endpoint, DsuPacket.MessageVersion, payload);
    }

    private async Task SendControllerInfoAsync(IPEndPoint endpoint, byte[] request)
    {
        if (request.Length < 4)
            return;

        int count = BinaryPrimitives.ReadInt32LittleEndian(request.AsSpan(0, 4));
        if (count < 0 || count > 4 || request.Length < 4 + count)
            return;

        for (int i = 0; i < count; i++)
        {
            byte slot = request[4 + i];
            if (slot != DsuSlot)
            {
                await SendControllerInfoResponseAsync(endpoint, slot, false);
                continue;
            }

            Ds3MotionSnapshot snapshot = _snapshotProvider();
            await SendControllerInfoResponseAsync(endpoint, DsuSlot, snapshot.Connected);
        }
    }

    private async Task SendControllerInfoResponseAsync(IPEndPoint endpoint, byte slot, bool connected)
    {
        byte[] payload = new byte[12];
        payload[0] = slot;
        payload[1] = connected ? (byte)2 : (byte)0;
        payload[2] = connected ? (byte)1 : (byte)0;
        payload[3] = connected ? (byte)1 : (byte)0;
        Array.Copy(DsuMac, 0, payload, 4, 6);

        await SendPacketAsync(endpoint, DsuPacket.MessageControllerInfo, payload);
    }

    private void HandleSubscription(IPEndPoint endpoint, byte[] request)
    {
        if (request.Length < 8)
            return;

        byte flags = request[0];
        byte requestedSlot = request[1];

        bool allControllers = (flags & 0x03) == 0;

        // Reject unsupported bits.
        if ((flags & ~0x03) != 0)
            return;

        // Bit 0 = slot-based registration.
        if ((flags & 0x01) != 0 && requestedSlot != DsuSlot)
            return;

        // Bit 1 = MAC-based registration.
        if ((flags & 0x02) != 0 && !request.AsSpan(2, 6).SequenceEqual(DsuMac))
            return;

        string key = endpoint.ToString();
        _subscriptions[key] = new Subscription
        {
            Endpoint = endpoint,
            LastRequest = Stopwatch.GetTimestamp(),
            AllControllers = allControllers,
            Slot = DsuSlot
        };
    }

    private async Task SendLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(8));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            Ds3MotionSnapshot snapshot = _snapshotProvider();
            if (!snapshot.Connected)
                continue;

            long now = Stopwatch.GetTimestamp();

            foreach (var entry in _subscriptions)
            {
                Subscription subscription = entry.Value;

                double age = Stopwatch.GetElapsedTime(subscription.LastRequest, now).TotalSeconds;
                if (age > SubscriptionTimeoutSeconds)
                {
                    _subscriptions.TryRemove(entry.Key, out _);
                    continue;
                }

                if (!subscription.AllControllers && subscription.Slot != DsuSlot)
                    continue;

                byte[] payload = BuildControllerData(snapshot);
                await SendPacketAsync(subscription.Endpoint, DsuPacket.MessageControllerData, payload);
            }
        }
    }

    private byte[] BuildControllerData(Ds3MotionSnapshot snapshot)
    {
        byte[] payload = new byte[80];

        payload[0] = DsuSlot;
        payload[1] = 2;
        payload[2] = 1;
        payload[3] = 1;
        Array.Copy(DsuMac, 0, payload, 4, 6);
        payload[11] = 1;

        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12, 4), ++_packetNumber);

        payload[20] = 128;
        payload[21] = 128;
        payload[22] = 128;
        payload[23] = 128;

        ulong timestamp = unchecked((ulong)(Stopwatch.GetTimestamp() * 1_000_000.0 / Stopwatch.Frequency));
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(48, 8), timestamp);

        WriteFloat(payload, 56, (float)snapshot.AccelXG);
        WriteFloat(payload, 60, (float)snapshot.AccelZG);
        WriteFloat(payload, 64, (float)-snapshot.AccelYG);

        const double dsuGyroScale = 312.0 / 360.0;
        WriteFloat(payload, 68, (float)(snapshot.PitchDegreesPerSecond * dsuGyroScale));
        WriteFloat(payload, 72, (float)(snapshot.GyroAxisDegreesPerSecond * dsuGyroScale));
        WriteFloat(payload, 76, (float)(-snapshot.RollDegreesPerSecond * dsuGyroScale));

        return payload;
    }

    private static void WriteFloat(byte[] destination, int offset, float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(destination.AsSpan(offset, 4), value);
    }

    private async Task SendPacketAsync(IPEndPoint endpoint, uint messageType, byte[] payload)
    {
        byte[] packet = DsuPacket.BuildServerPacket(messageType, payload, _serverId);
        await _udp.SendAsync(packet, packet.Length, endpoint);
    }

    public void Dispose()
    {
        _udp.Dispose();
    }
}
