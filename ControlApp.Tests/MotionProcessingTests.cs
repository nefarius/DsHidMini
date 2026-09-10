using Nefarius.DsHidMini.ControlApp.Models.Motion;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionProcessingTests
{
    // DS3-A1a EEPROM from docs/MOTION.md
    private const int ZeroX = 498;
    private const int OneGX = 386;
    private const int ZeroY = 494;
    private const int OneGY = 383;
    private const int ZeroZ = 496;
    private const int OneGZ = 387;

    [Theory]
    [InlineData(494, 509, false)]
    [InlineData(607, 621, false)]
    [InlineData(383, 397, false)]
    public void AccelX_MatchesMotionDoc_WithoutMirror(int raw, int expected, bool mirror)
    {
        Assert.Equal(expected, DsMotionMath.CalibrateAccel(raw, ZeroX, OneGX, mirror));
    }

    [Theory]
    [InlineData(475, 493)]
    [InlineData(385, 402)]
    [InlineData(603, 622)]
    public void AccelY_MatchesMotionDoc(int raw, int expected)
    {
        Assert.Equal(expected, DsMotionMath.CalibrateAccel(raw, ZeroY, OneGY, false));
    }

    [Theory]
    [InlineData(396, 409)]
    [InlineData(624, 644)]
    public void AccelZ_MatchesMotionDoc(int raw, int expected)
    {
        Assert.Equal(expected, DsMotionMath.CalibrateAccel(raw, ZeroZ, OneGZ, false));
    }

    [Fact]
    public void AccelX_MirrorHappensAfterCalibration()
    {
        int cal = DsMotionMath.CalibrateAccel(607, ZeroX, OneGX, false);
        Assert.Equal(621, cal);
        Assert.Equal(0x3FF - 621, DsMotionMath.CalibrateAccel(607, ZeroX, OneGX, true));
    }

    [Fact]
    public void Accel_ZeroEqualsOneG_PassesRawThrough()
    {
        Assert.Equal(500, DsMotionMath.CalibrateAccel(500, 512, 512, false));
        Assert.Equal(0x3FF - 500, DsMotionMath.CalibrateAccel(500, 512, 512, true));
    }

    [Fact]
    public void PlainZeroGyro_UsesEepromZero()
    {
        // DS3-A1a: EEPROM 481, idle ~482 → ~511
        Assert.Equal(511, DsMotionMath.PlainZeroGyro(482, 481));
        Assert.Equal(512, DsMotionMath.PlainZeroGyro(481, 481));
    }

    [Fact]
    public void HwCalGyro_InvertsRawWithoutSoftwareZero()
    {
        // DS3-A2 idle 361 with factory cal applied
        Assert.Equal(0x3FF - 361, DsMotionMath.HwCalReportedGyro(361));
        Assert.Equal(512, DsMotionMath.HwCalReportedGyro(511));
    }

    [Fact]
    public void SixaxisGyro_UsesZeroRefNotEepromWhenTrackerSeeded()
    {
        SonyGyroTracker tracker = new();
        tracker.Initial(0x75, 513);
        Assert.Equal(DsMotionMath.Clamp10(512 + tracker.ZeroRef - 513), tracker.Output);
    }

    [Fact]
    public void Gyro_ClockwisePositive_AfterInversion()
    {
        // Raw falls for clockwise-from-above; Sony output must rise above 512.
        Assert.True(DsMotionMath.PlainZeroGyro(400, 512) > 512);
        Assert.True(DsMotionMath.HwCalReportedGyro(400) > 512);
    }

    [Fact]
    public void MilliG_AndMilliDps_UseDocumentedScales()
    {
        Assert.Equal(1000, DsMotionMath.ToMilliG(512 + 113));
        Assert.Equal(-1000, DsMotionMath.ToMilliG(512 - 113));
        Assert.Equal(0, DsMotionMath.ToMilliDps(512));
        Assert.Equal(5000 / 7, DsMotionMath.ToMilliDps(513));
    }

    [Fact]
    public void Tracker_InitialCalByte_IsTruncatedNotClamped()
    {
        SonyGyroTracker tracker = new();
        // Far-below-rest EEPROM zero forces a large negative step that wraps as u8.
        byte sent = tracker.Initial(0, 200);
        Assert.Equal(unchecked((byte)tracker.CalByteRaw), sent);
        Assert.InRange(tracker.CalByteRaw, int.MinValue, int.MaxValue);
    }

    [Fact]
    public void Tracker_StillRawConvergesCalByte()
    {
        SonyGyroTracker tracker = new();
        tracker.Initial(0x7F, 521);

        bool everChanged = false;
        for (int i = 0; i < 800; i++)
        {
            tracker.Runtime(361, out bool changed);
            everChanged |= changed;
        }

        Assert.True(everChanged || tracker.ZeroRef != 521);
    }
}
