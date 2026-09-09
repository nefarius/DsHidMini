namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     DualShock 3 player-index to LED flags mapping used by
///     <see cref="DsHidMiniInterop.SetPlayerIndex" /> (issue #379).
/// </summary>
public static class Ds3PlayerLeds
{
    /// <summary>Physical LED 1 bit.</summary>
    public const byte Led1 = 0x02;

    /// <summary>Physical LED 2 bit.</summary>
    public const byte Led2 = 0x04;

    /// <summary>Physical LED 3 bit.</summary>
    public const byte Led3 = 0x08;

    /// <summary>Physical LED 4 bit.</summary>
    public const byte Led4 = 0x10;

    /// <summary>Explicit all-off marker used by the hardware and custom patterns.</summary>
    public const byte LedOff = 0x20;

    /// <summary>
    ///     Maps a player index (1-7) to the DS3 LED flags byte. Indices 5-7 use the
    ///     extra combinations the hardware uses once four physical LEDs are exhausted.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> when <paramref name="playerIndex" /> is 1-7;
    ///     <paramref name="flags" /> is then the matching LED mask.
    /// </returns>
    public static bool TryGetFlags(byte playerIndex, out byte flags)
    {
        switch (playerIndex)
        {
            case 1:
                flags = Led1;
                return true;
            case 2:
                flags = Led2;
                return true;
            case 3:
                flags = Led3;
                return true;
            case 4:
                flags = Led4;
                return true;
            case 5:
                flags = (byte)(Led1 | Led4);
                return true;
            case 6:
                flags = (byte)(Led2 | Led4);
                return true;
            case 7:
                flags = (byte)(Led3 | Led4);
                return true;
            default:
                flags = 0;
                return false;
        }
    }
}
