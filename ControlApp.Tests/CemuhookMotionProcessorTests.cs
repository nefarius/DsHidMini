using System.Diagnostics;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class CemuhookMotionProcessorTests
{
    [Theory]
    [InlineData(DsIdentificationMotionPath.HwCal, false)]
    [InlineData(DsIdentificationMotionPath.Sixaxis, false)]
    [InlineData(DsIdentificationMotionPath.PlainZero, false)]
    [InlineData(DsIdentificationMotionPath.HwCal, true)]
    [InlineData(DsIdentificationMotionPath.Sixaxis, true)]
    [InlineData(DsIdentificationMotionPath.PlainZero, true)]
    public void UsesCalibratedValuesFromOneSnapshotForEveryPath(DsIdentificationMotionPath path, bool fallback)
    {
        var processor = new CemuhookMotionProcessor(new MotionControlsSettings
        {
            KalmanFilterStrength = 0,
            AccelSensitivity = 1.5,
            GyroSensitivity = 0.5
        });
        var sample = Sample(1, 1);
        sample.MotionPath = path;
        sample.Flags |= fallback ? DsMotionSnapshotFlags.Fallback : DsMotionSnapshotFlags.None;
        // Contradictory raw/calibration fields must never override the driver's processed values.
        sample.RawAccelX = sample.RawAccelY = sample.RawAccelZ = 600;
        sample.RawGyro = 496;
        sample.CalGyro = 100;
        sample.AccelMilliGX = 125;
        sample.AccelMilliGY = 250;
        sample.GyroMilliDps = 12345;

        Assert.True(processor.TryProcess(in sample, out var result));
        Assert.Equal(0.1875, result.AccelXG);
        Assert.Equal(-0.375, result.AccelYG);
        Assert.Equal(-1.5, result.AccelZG);
        Assert.Equal(6.1725, result.GyroAxisDegreesPerSecond);
        Assert.Equal(sample.TimestampQpc, result.TimestampQpc);
    }

    [Theory]
    [InlineData(DsIdentificationMotionPath.HwCal)]
    [InlineData(DsIdentificationMotionPath.Sixaxis)]
    public void StartingOnAnAlreadyCalibratedStationaryPadDoesNotIntroduceGyroBias(DsIdentificationMotionPath path)
    {
        var processor = new CemuhookMotionProcessor(new MotionControlsSettings());
        for (uint index = 1; index <= 150; index++)
        {
            var sample = Sample(index, 1 + index * 0.005);
            sample.MotionPath = path;
            sample.RawGyro = 496;
            sample.ZeroRef = 496;
            sample.GyroMilliDps = 0;
            Assert.True(processor.TryProcess(in sample, out var result));
            Assert.Equal(0, result.GyroAxisDegreesPerSecond);
        }
    }

    [Fact]
    public void AngularVelocityUsesAcquisitionTimeRatherThanProcessingTime()
    {
        var processor = new CemuhookMotionProcessor(new MotionControlsSettings { KalmanFilterStrength = 1 });
        var first = Sample(1, 1);
        var second = Sample(2, 1.1);
        second.AccelMilliGY = 500;
        second.AccelMilliGZ = -866;
        Assert.True(processor.TryProcess(in first, out _));
        // Both calls run back-to-back. Filtering reduces the tilt step to about 2.04 degrees;
        // its rate must use the sensor's 100 ms interval, not the tiny processing interval.
        Assert.True(processor.TryProcess(in second, out var result));
        Assert.InRange(result.PitchDegreesPerSecond, 20, 21);
        Assert.Equal(0, result.RollDegreesPerSecond);
    }

    [Fact]
    public void RejectsDuplicateAndOutOfOrderSamplesWithoutChangingFilterState()
    {
        var settings = new MotionControlsSettings();
        var processor = new CemuhookMotionProcessor(settings);
        var reference = new CemuhookMotionProcessor(settings);
        var first = Sample(1, 1);
        Assert.True(processor.TryProcess(in first, out _));
        Assert.True(reference.TryProcess(in first, out _));
        var duplicate = first;
        duplicate.GyroMilliDps = 200000;
        Assert.False(processor.TryProcess(in duplicate, out _));
        duplicate.SampleIndex = 2;
        duplicate.TimestampQpc--;
        Assert.False(processor.TryProcess(in duplicate, out _));
        var next = Sample(2, 1.01);
        next.GyroMilliDps = 10000;
        Assert.True(processor.TryProcess(in next, out var actual));
        Assert.True(reference.TryProcess(in next, out var expected));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnavailableOrIncompatibleSnapshotsAreNotPublishedAsConnected()
    {
        var processor = new CemuhookMotionProcessor(new MotionControlsSettings());
        var sample = Sample(1, 1);
        sample.Flags = DsMotionSnapshotFlags.None;
        Assert.False(processor.TryProcess(in sample, out _));
        sample.Flags = DsMotionSnapshotFlags.Available;
        sample.Version++;
        Assert.False(processor.TryProcess(in sample, out _));
        sample.Version = DsMotionSnapshot.CurrentVersion;
        sample.TimestampQpc = 0;
        Assert.False(processor.TryProcess(in sample, out _));
    }

    [Fact]
    public void ResumingAfterAGapResetsFiltersAndDoesNotSynthesizeATurnAcrossTheGap()
    {
        var processor = new CemuhookMotionProcessor(new MotionControlsSettings());
        var first = Sample(1, 1);
        first.GyroMilliDps = 90000;
        Assert.True(processor.TryProcess(in first, out _));
        var resumed = Sample(2, 2);
        resumed.AccelMilliGY = 1000;
        resumed.AccelMilliGZ = 0;
        Assert.True(processor.TryProcess(in resumed, out var result));
        Assert.Equal(-1, result.AccelYG);
        Assert.Equal(0, result.AccelZG);
        Assert.Equal(0, result.GyroAxisDegreesPerSecond);
        Assert.Equal(0, result.PitchDegreesPerSecond);
        Assert.Equal(0, result.RollDegreesPerSecond);
        processor.Reset();
        // A reconnected device may restart its sample counter.
        Assert.True(processor.TryProcess(in first, out _));
    }

    internal static DsMotionSnapshot Sample(uint index, double seconds) => new()
    {
        SlotIndex = 1,
        Version = DsMotionSnapshot.CurrentVersion,
        Flags = DsMotionSnapshotFlags.Available,
        SampleIndex = index,
        TimestampQpc = (ulong)(seconds * Stopwatch.Frequency),
        AccelMilliGZ = -1000,
        CalGyro = 512
    };
}
