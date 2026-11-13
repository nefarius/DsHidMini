using System.Security.Cryptography;
using System.Text;
using Nefarius.DsHidMini.ControlApp.Models.Util;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

internal sealed record CemuhookControllerIdentity(string Key, ulong Mac)
{
    public static CemuhookControllerIdentity Create(string? address, string instanceId)
    {
        string normalized = MacAddressFormatter.Normalize(address);
        if (normalized.Length == 12 && normalized != "000000000000")
            return new(normalized, Convert.ToUInt64(normalized, 16));

        string key = instanceId.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("A stable controller identity is required.");
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        hash[0] = (byte)((hash[0] | 2) & 0xFE);
        ulong mac = 0;
        foreach (byte value in hash.AsSpan(0, 6)) mac = (mac << 8) | value;
        return new(key, mac);
    }
}

internal sealed record CemuhookPadState(byte Slot, ulong Mac, byte ConnectionType, Ds3MotionSnapshot Motion)
{
    public static readonly Ds3MotionSnapshot Inactive = new(false, 0, 0, 0, 0, 0, 0);
    public static CemuhookPadState Empty(byte slot) => new(slot, 0, 0, Inactive);
}

internal sealed class CemuhookControllerRegistry
{
    internal sealed record Lease(string Identity, byte Slot, long Generation);
    private sealed record Entry(CemuhookControllerIdentity Identity, Lease Lease, bool Attached, long Order, CemuhookPadState State);
    private readonly object _gate = new();
    private readonly Entry?[] _slots = new Entry?[4];
    private long _generation;

    public Lease Attach(CemuhookControllerIdentity identity, byte connectionType, int? preferredSlot = null)
    {
        lock (_gate)
        {
            int slot = Array.FindIndex(_slots, entry => entry?.Identity.Key == identity.Key);
            if (slot < 0 && preferredSlot is >= 0 and < 4 && _slots[preferredSlot.Value]?.Attached != true)
                slot = preferredSlot.Value;
            if (slot < 0) slot = Array.FindIndex(_slots, entry => entry == null);
            if (slot < 0)
                slot = Enumerable.Range(0, 4).Where(index => !_slots[index]!.Attached)
                    .OrderBy(index => _slots[index]!.Order).DefaultIfEmpty(-1).First();
            if (slot < 0) throw new InvalidOperationException("All four Cemuhook controller slots are in use. Disable another controller first.");
            var lease = new Lease(identity.Key, (byte)slot, ++_generation);
            _slots[slot] = new(identity, lease, true, _generation,
                new((byte)slot, identity.Mac, connectionType, CemuhookPadState.Inactive));
            return lease;
        }
    }

    public bool Publish(Lease lease, Ds3MotionSnapshot snapshot)
    {
        lock (_gate)
        {
            Entry? entry = _slots[lease.Slot];
            if (entry?.Lease != lease || !entry.Attached) return false;
            _slots[lease.Slot] = entry with { State = entry.State with { Motion = snapshot } };
            return true;
        }
    }

    public void Detach(Lease lease)
    {
        lock (_gate)
        {
            Entry? entry = _slots[lease.Slot];
            if (entry?.Lease != lease) return;
            _slots[lease.Slot] = entry with { Attached = false, State = entry.State with { Motion = CemuhookPadState.Inactive } };
        }
    }

    public void SetConnection(Lease lease, byte connectionType)
    {
        lock (_gate)
        {
            Entry? entry = _slots[lease.Slot];
            if (entry?.Lease == lease)
                _slots[lease.Slot] = entry with { State = entry.State with { ConnectionType = connectionType } };
        }
    }

    public CemuhookPadState Get(byte slot)
    {
        lock (_gate) return slot < 4 ? _slots[slot]?.State ?? CemuhookPadState.Empty(slot) : CemuhookPadState.Empty(slot);
    }
}
