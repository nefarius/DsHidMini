namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Sony-compatible motion formulas from <c>docs/MOTION.md</c> / <c>driver/DsMotion.c</c>.
/// </summary>
internal static class DsMotionMath
{
    public const int AccelGain = 113;
    public const int NominalZero = 512;
    public const int NominalOneG = NominalZero - AccelGain;

    public static int Q10(int value)
    {
        return (value + ((value >> 31) & 0x3FF)) >> 10;
    }

    public static int Clamp10(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value > 1023 ? 1023 : value;
    }

    public static int CalibrateAccel(int raw, int zero, int oneG, bool mirror)
    {
        int cal;
        if (zero != oneG)
        {
            int t = ((raw - zero) * 1024 / (zero - oneG)) * AccelGain;
            cal = Q10(t) + NominalZero;
        }
        else
        {
            cal = raw;
        }

        if (mirror)
        {
            cal = 0x3FF - cal;
        }

        return cal;
    }

    public static int PlainZeroGyro(int raw, int eepromZero)
    {
        return Clamp10(NominalZero + eepromZero - raw);
    }

    public static int HwCalReportedGyro(int raw)
    {
        return Clamp10(0x3FF - raw);
    }

    public static int TrackedGyro(int raw, int zeroRef)
    {
        return Clamp10(NominalZero + zeroRef - raw);
    }

    public static int ToMilliG(int calibrated)
    {
        return ((calibrated - NominalZero) * 1000) / AccelGain;
    }

    public static int ToMilliDps(int calibrated)
    {
        return ((calibrated - NominalZero) * 5000) / 7;
    }
}
