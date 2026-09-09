namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     Full DualShock 3 LED pattern: a flags byte plus four independent
///     per-LED effect blocks. Used by <see cref="DsHidMiniInterop.SetLedPattern" />.
/// </summary>
public readonly struct Ds3LedPattern
{
    /// <summary>
    ///     Documented DS3 LED bits: physical LEDs 1-4 and the explicit off marker.
    /// </summary>
    public const byte ValidFlagsMask =
        Ds3PlayerLeds.Led1 | Ds3PlayerLeds.Led2 | Ds3PlayerLeds.Led3 | Ds3PlayerLeds.Led4 | Ds3PlayerLeds.LedOff;

    public Ds3LedPattern(
        byte flags,
        Ds3LedEffect player1,
        Ds3LedEffect player2,
        Ds3LedEffect player3,
        Ds3LedEffect player4)
    {
        Flags = flags;
        Player1 = player1;
        Player2 = player2;
        Player3 = player3;
        Player4 = player4;
    }

    public byte Flags { get; }

    public Ds3LedEffect Player1 { get; }

    public Ds3LedEffect Player2 { get; }

    public Ds3LedEffect Player3 { get; }

    public Ds3LedEffect Player4 { get; }

    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="flags" /> uses only
    ///     documented DS3 LED bits. Zero (all off) is valid.
    /// </summary>
    public static bool AreFlagsValid(byte flags)
    {
        return (flags & ~ValidFlagsMask) == 0;
    }

    /// <summary>
    ///     Static player-index pattern for slots 1-7 (same mapping as
    ///     <see cref="DsHidMiniInterop.SetPlayerIndex" />).
    /// </summary>
    public static bool TryFromPlayerIndex(byte playerIndex, out Ds3LedPattern pattern)
    {
        if (!Ds3PlayerLeds.TryGetFlags(playerIndex, out byte flags))
        {
            pattern = default;
            return false;
        }

        pattern = new Ds3LedPattern(
            flags,
            Ds3LedEffect.Static,
            Ds3LedEffect.Static,
            Ds3LedEffect.Static,
            Ds3LedEffect.Static);
        return true;
    }
}
