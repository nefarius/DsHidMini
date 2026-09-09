namespace Nefarius.DsHidMini.IPC.Models.Public;

/// <summary>
///     One DualShock 3 LED effect block (duration and flash multipliers).
///     Values match the driver's <c>DS_LED</c> / named effect macros.
/// </summary>
public readonly struct Ds3LedEffect
{
    /// <summary>PS3-correct static effect: lasts forever, no flashing.</summary>
    public static Ds3LedEffect Static { get; } = new(0xFF, 0x0001, 0x00, 0x01);

    /// <summary>Slow flash, used for Low/Dying battery levels.</summary>
    public static Ds3LedEffect SlowFlash { get; } = new(0xFF, 0x000F, 0x7F, 0x7F);

    /// <summary>Fast flash, used by the DS4Windows high-latency warning.</summary>
    public static Ds3LedEffect FastFlash { get; } = new(0xFF, 0x0003, 0x7F, 0x7F);

    /// <summary>All-zero effect block for an unlit LED.</summary>
    public static Ds3LedEffect None { get; } = new(0x00, 0x0000, 0x00, 0x00);

    public Ds3LedEffect(
        byte totalDuration,
        ushort basePortionDuration,
        byte offPortionMultiplier,
        byte onPortionMultiplier)
    {
        TotalDuration = totalDuration;
        BasePortionDuration = basePortionDuration;
        OffPortionMultiplier = offPortionMultiplier;
        OnPortionMultiplier = onPortionMultiplier;
    }

    public byte TotalDuration { get; }

    public ushort BasePortionDuration { get; }

    public byte OffPortionMultiplier { get; }

    public byte OnPortionMultiplier { get; }
}
