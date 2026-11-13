using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>One asynchronous DSU listener serving controller slots 0 through 3.</summary>
internal sealed class CemuhookServer : IDisposable
{
    private readonly uint _serverId = unchecked((uint)Random.Shared.NextInt64(1, int.MaxValue));
    private readonly UdpClient _udp;
    private readonly Func<byte, CemuhookPadState> _provider;
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly Channel<byte> _updates = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
    { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest, AllowSynchronousContinuations = false });
    private readonly uint[] _packetNumbers = new uint[4];
    private int _pendingSlots;

    private sealed record Subscription(IPEndPoint Endpoint, long All, long[] Slots, Dictionary<ulong, long> Macs)
    {
        public bool Matches(CemuhookPadState pad, long now) => Fresh(All, now) || Fresh(Slots[pad.Slot], now)
            || (Macs.TryGetValue(pad.Mac, out long requested) && Fresh(requested, now));
        public bool Expired(long now) => !Fresh(All, now) && !Slots.Any(time => Fresh(time, now)) && !Macs.Values.Any(time => Fresh(time, now));
        private static bool Fresh(long time, long now) => time != 0 && Stopwatch.GetElapsedTime(time, now).TotalSeconds <= 5;
    }

    internal IPEndPoint LocalEndPoint => (IPEndPoint)_udp.Client.LocalEndPoint!;

    public CemuhookServer(int port, string bindAddress, Func<byte, CemuhookPadState> provider)
    {
        _provider = provider;
        _udp = new UdpClient(new IPEndPoint(IPAddress.Parse(bindAddress), port));
        NotifySnapshotAvailable();
    }

    public void NotifySnapshotAvailable(byte? slot = null)
    {
        Interlocked.Or(ref _pendingSlots, slot is < 4 ? 1 << slot.Value : 15);
        _updates.Writer.TryWrite(0);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var workers = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task receive = ReceiveLoopAsync(workers.Token);
        Task send = SendLoopAsync(workers.Token);
        try
        {
            await Task.WhenAny(receive, send).ConfigureAwait(false);
            workers.Cancel();
            await Task.WhenAll(receive, send).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (workers.IsCancellationRequested) { }
        finally { Dispose(); }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            UdpReceiveResult received;
            try { received = await _udp.ReceiveAsync(token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset) { continue; }
            if (!DsuPacket.TryParseClientPacket(received.Buffer, out uint type, out byte[] payload)) continue;
            switch (type)
            {
                case DsuPacket.MessageVersion:
                    var version = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(version, DsuPacket.ProtocolVersion);
                    await SendPacketAsync(received.RemoteEndPoint, type, version, token).ConfigureAwait(false);
                    break;
                case DsuPacket.MessageControllerInfo:
                    if (payload.Length < 4) break;
                    int count = BinaryPrimitives.ReadInt32LittleEndian(payload);
                    if (count is < 0 or > 4 || payload.Length < 4 + count) break;
                    for (int i = 0; i < count; i++)
                    {
                        byte slot = payload[4 + i];
                        if (slot >= 4) continue;
                        var info = new byte[12];
                        WriteIdentity(info, _provider(slot));
                        await SendPacketAsync(received.RemoteEndPoint, type, info, token).ConfigureAwait(false);
                    }
                    break;
                case DsuPacket.MessageControllerData:
                    Subscribe(received.RemoteEndPoint, payload);
                    break;
            }
        }
    }

    private void Subscribe(IPEndPoint endpoint, byte[] request)
    {
        if (request.Length < 8 || (request[0] & ~3) != 0) return;
        byte flags = request[0], slot = request[1];
        if ((flags & 1) != 0 && slot >= 4) return;
        ulong mac = ReadMac(request.AsSpan(2, 6));
        long now = Stopwatch.GetTimestamp();
        string key = endpoint.ToString();
        Subscription Add(Subscription? previous)
        {
            var slots = previous == null ? new long[4] : (long[])previous.Slots.Clone();
            var macs = previous == null ? new Dictionary<ulong, long>() : new(previous.Macs);
            foreach (ulong old in macs.Where(pair => Stopwatch.GetElapsedTime(pair.Value, now).TotalSeconds > 5).Select(pair => pair.Key).ToArray()) macs.Remove(old);
            if ((flags & 1) != 0) slots[slot] = now;
            if ((flags & 2) != 0) macs[mac] = now;
            return new(endpoint, flags == 0 ? now : previous?.All ?? 0, slots, macs);
        }
        _subscriptions.AddOrUpdate(key, _ => Add(null), (_, previous) => Add(previous));
        NotifySnapshotAvailable();
    }

    private async Task SendLoopAsync(CancellationToken token)
    {
        await foreach (byte _ in _updates.Reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            int mask = Interlocked.Exchange(ref _pendingSlots, 0);
            long now = Stopwatch.GetTimestamp();
            foreach (var entry in _subscriptions)
                if (entry.Value.Expired(now)) _subscriptions.TryRemove(entry);
            for (byte slot = 0; slot < 4; slot++)
            {
                if ((mask & (1 << slot)) == 0) continue;
                CemuhookPadState pad = _provider(slot);
                if (!pad.Motion.Connected) continue;
                byte[]? packet = null;
                foreach (Subscription subscription in _subscriptions.Values)
                {
                    if (!subscription.Matches(pad, now)) continue;
                    packet ??= DsuPacket.BuildServerPacket(DsuPacket.MessageControllerData, BuildControllerData(pad), _serverId);
                    await _udp.SendAsync(packet, subscription.Endpoint, token).ConfigureAwait(false);
                }
            }
        }
    }

    private static void WriteIdentity(byte[] payload, CemuhookPadState pad)
    {
        bool connected = pad.Motion.Connected;
        payload[0] = pad.Slot;
        payload[1] = connected ? (byte)2 : (byte)0;
        payload[2] = connected ? (byte)1 : (byte)0;
        payload[3] = connected ? pad.ConnectionType : (byte)0;
        for (int i = 0; i < 6; i++) payload[4 + i] = (byte)(pad.Mac >> ((5 - i) * 8));
    }

    private byte[] BuildControllerData(CemuhookPadState pad)
    {
        var payload = new byte[80];
        WriteIdentity(payload, pad);
        payload[11] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12, 4), ++_packetNumbers[pad.Slot]);
        payload[20] = payload[21] = payload[22] = payload[23] = 128;
        Ds3MotionSnapshot motion = pad.Motion;
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(48, 8), (ulong)(motion.TimestampQpc * 1_000_000.0 / Stopwatch.Frequency));
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(56, 4), (float)motion.AccelXG);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(60, 4), (float)motion.AccelZG);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(64, 4), (float)-motion.AccelYG);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(68, 4), (float)motion.PitchDegreesPerSecond);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(72, 4), (float)motion.GyroAxisDegreesPerSecond);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(76, 4), (float)-motion.RollDegreesPerSecond);
        return payload;
    }

    private static ulong ReadMac(ReadOnlySpan<byte> bytes)
    {
        ulong mac = 0;
        foreach (byte value in bytes) mac = (mac << 8) | value;
        return mac;
    }

    private async Task SendPacketAsync(IPEndPoint endpoint, uint type, byte[] payload, CancellationToken token) =>
        await _udp.SendAsync(DsuPacket.BuildServerPacket(type, payload, _serverId), endpoint, token).ConfigureAwait(false);

    public void Dispose()
    {
        _updates.Writer.TryComplete();
        _udp.Dispose();
    }
}
