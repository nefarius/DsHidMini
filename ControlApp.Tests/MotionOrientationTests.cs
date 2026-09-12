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

    [Fact]
    public void FlatStill_LearnsRestBiasAfterWindow()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out _);

        Assert.True(estimator.HasRestBias);
        Assert.InRange(estimator.RestBiasDps, -1.02, -0.98);
        Assert.Equal(0, estimator.YawDegrees);
    }

    [Fact]
    public void LearnedRestBias_IsSubtractedFromTurns()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out uint index, out ulong qpc);

        estimator.Update(Sample(0, 0, -1000, 5_000, index + 1, qpc + 100_000));

        Assert.InRange(estimator.YawDegrees, 0.59, 0.61);
    }

    [Fact]
    public void LearnedRestBias_RestStillDoesNotIntegrateYaw()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out uint index, out ulong qpc);

        estimator.Update(Sample(0, 0, -1000, -1_000, index + 1, qpc + 100_000));

        Assert.Equal(0, estimator.YawDegrees);
    }

    [Fact]
    public void LearnedRestBias_DoesNotUnmaskRateBelowRawDeadzone()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out uint index, out ulong qpc);

        estimator.Update(Sample(0, 0, -1000, 3_500, index + 1, qpc + 100_000));

        Assert.Equal(0, estimator.YawDegrees);
    }

    [Fact]
    public void LearnedRestBias_FrozenAfterLearn()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out uint index, out ulong qpc);
        FeedStill(estimator, -1_500, MotionOrientationEstimator.BiasLearnSamples, ref index, ref qpc);

        Assert.InRange(estimator.RestBiasDps, -1.02, -0.98);
    }

    [Fact]
    public void RestBiasWindow_RejectsWideRateRange()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        uint index = 0;
        ulong qpc = 0;
        for (int i = 0; i < MotionOrientationEstimator.BiasLearnSamples; i++)
        {
            index++;
            qpc += 10_000;
            int milliDps = (i % 2) == 0 ? -1_000 : 1_000;
            estimator.Update(Sample(0, 0, -1000, milliDps, index, qpc));
        }

        Assert.False(estimator.HasRestBias);
    }

    [Fact]
    public void MotionDuringLearn_ResetsRestBiasWindow()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        uint index = 0;
        ulong qpc = 0;
        FeedStill(estimator, -1_000, MotionOrientationEstimator.BiasLearnSamples / 2, ref index, ref qpc);
        index++;
        qpc += 10_000;
        estimator.Update(Sample(0, 0, -1000, 20_000, index, qpc));
        FeedStill(estimator, -1_000, MotionOrientationEstimator.BiasLearnSamples / 2, ref index, ref qpc);

        Assert.False(estimator.HasRestBias);
    }

    [Fact]
    public void OnGripStill_DoesNotLearnRestBias()
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        uint index = 0;
        ulong qpc = 0;
        for (int i = 0; i < MotionOrientationEstimator.BiasLearnSamples; i++)
        {
            index++;
            qpc += 10_000;
            estimator.Update(Sample(1000, 0, 0, -1_000, index, qpc));
        }

        Assert.False(estimator.HasRestBias);
    }

    [Fact]
    public void Recenter_KeepsRestBias()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out _, out _);
        estimator.Recenter();

        Assert.True(estimator.HasRestBias);
        Assert.InRange(estimator.RestBiasDps, -1.02, -0.98);
        Assert.Equal(0, estimator.YawDegrees);
    }

    [Fact]
    public void Reset_ClearsRestBias()
    {
        MotionOrientationEstimator estimator = LearnRestBias(-1_000, out _, out _);

        estimator.Reset();

        Assert.False(estimator.HasRestBias);
        Assert.Equal(0, estimator.RestBiasDps);
    }

    private static MotionOrientationEstimator LearnRestBias(int milliDps, out uint index)
    {
        return LearnRestBias(milliDps, out index, out _);
    }

    private static MotionOrientationEstimator LearnRestBias(int milliDps, out uint index, out ulong qpc)
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        index = 0;
        qpc = 0;
        FeedStill(estimator, milliDps, MotionOrientationEstimator.BiasLearnSamples, ref index, ref qpc);
        return estimator;
    }

    private static void FeedStill(
        MotionOrientationEstimator estimator,
        int milliDps,
        int count,
        ref uint index,
        ref ulong qpc)
    {
        for (int i = 0; i < count; i++)
        {
            index++;
            qpc += 10_000;
            estimator.Update(Sample(0, 0, -1000, milliDps, index, qpc));
        }
    }
}
