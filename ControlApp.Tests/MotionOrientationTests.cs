using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionOrientationTests
{
    private static DsMotionSnapshot Sample(int mx, int my, int mz, int mdps, uint index, ulong qpc)
    {
        return new DsMotionSnapshot
        {
            Flags = DsMotionSnapshotFlags.Available,
            AccelMilliGX = mx,
            AccelMilliGY = my,
            AccelMilliGZ = mz,
            GyroMilliDps = mdps,
            SampleIndex = index,
            TimestampQpc = qpc
        };
    }

    [Fact]
    public void FlatButtonsUp_IsLevel()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 0, 1, 1_000_000));

        Assert.InRange(estimator.PitchDegrees, -1.0, 1.0);
        Assert.InRange(estimator.RollDegrees, -1.0, 1.0);
    }

    [Fact]
    public void RightGripDown_RollsNegative()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(1000, 0, 0, 0, 1, 1_000_000));

        Assert.True(estimator.RollDegrees < -80);
    }

    [Fact]
    public void UsbPortDown_PitchesNegative()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, -1000, 0, 0, 1, 1_000_000));

        Assert.True(estimator.PitchDegrees < -80);
    }

    [Fact]
    public void UsbPortUp_PitchesPositive()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 1000, 0, 0, 1, 1_000_000));

        Assert.True(estimator.PitchDegrees > 80);
    }

    [Fact]
    public void YawIntegratesCorrectedRate()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 90_000, 1, 0));
        estimator.Update(Sample(0, 0, -1000, 90_000, 2, 100_000));

        Assert.InRange(estimator.YawDegrees, 8.9, 9.1);
    }

    [Fact]
    public void Recenter_ZerosYawOnly()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, -1000, 0, 90_000, 1, 0));
        estimator.Update(Sample(0, -1000, 0, 90_000, 2, 100_000));
        double pitch = estimator.PitchDegrees;
        double roll = estimator.RollDegrees;

        estimator.Recenter();

        Assert.Equal(0, estimator.YawDegrees);
        Assert.Equal(pitch, estimator.PitchDegrees);
        Assert.Equal(roll, estimator.RollDegrees);
    }

    [Fact]
    public void Smoothing_BlendsGravity()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 0.5);
        estimator.Update(Sample(0, 0, -1000, 0, 1, 1));
        estimator.Update(Sample(1000, 0, 0, 0, 2, 2));

        Assert.InRange(estimator.SmoothMilliGX, 499, 501);
        Assert.InRange(estimator.SmoothMilliGZ, -501, -499);
    }

    [Fact]
    public void DuplicateSampleIndex_DoesNotIntegrateAgain()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 90_000, 1, 0));
        estimator.Update(Sample(0, 0, -1000, 90_000, 2, 100_000));
        double yaw = estimator.YawDegrees;
        estimator.Update(Sample(0, 0, -1000, 90_000, 2, 200_000));

        Assert.Equal(yaw, estimator.YawDegrees);
    }

    [Fact]
    public void RestGyroBias_BelowDeadzone_DoesNotIntegrateYaw()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 2_000, 1, 0));
        estimator.Update(Sample(0, 0, -1000, 2_000, 2, 100_000));

        Assert.Equal(0, estimator.YawDegrees);
    }

    [Fact]
    public void TurnRate_AboveDeadzone_IntegratesYaw()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 10_000, 1, 0));
        estimator.Update(Sample(0, 0, -1000, 10_000, 2, 100_000));

        Assert.InRange(estimator.YawDegrees, 0.99, 1.01);
    }

    [Fact]
    public void OnGripNoise_HoldsPitchReadout()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, -1000, 0, 1, 1_000_000));
        double pitch = estimator.PitchDegrees;

        estimator.Update(Sample(1000, 40, -50, 0, 2, 2_000_000));

        Assert.Equal(pitch, estimator.PitchDegrees);
        Assert.True(estimator.RollDegrees < -80);
    }

    [Fact]
    public void OnGrip_DoesNotIntegrateYaw()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(1000, 0, 0, 90_000, 1, 0));
        estimator.Update(Sample(1000, 0, 0, 90_000, 2, 100_000));

        Assert.InRange(estimator.YawDegrees, -0.01, 0.01);
    }

    [Fact]
    public void FaceDown_ReversesYawSign()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(0, 0, 1000, 90_000, 1, 0));
        estimator.Update(Sample(0, 0, 1000, 90_000, 2, 100_000));

        Assert.InRange(estimator.YawDegrees, -9.1, -8.9);
    }

    [Fact]
    public void Tilt45_IntegratesWeightedYaw()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(Sample(707, 0, -707, 90_000, 1, 0));
        estimator.Update(Sample(707, 0, -707, 90_000, 2, 100_000));

        double expected = 9.0 * (707.0 / Math.Sqrt((707.0 * 707.0) + (707.0 * 707.0)));
        Assert.InRange(estimator.YawDegrees, expected - 0.05, expected + 0.05);
    }
}
