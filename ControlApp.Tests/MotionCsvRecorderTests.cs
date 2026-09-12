using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionCsvRecorderTests
{
    [Fact]
    public void Write_EmitsHeaderAndInvariantRow()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"motion-test-{Guid.NewGuid():N}.csv");
        try
        {
            MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
            DsMotionSnapshot snapshot = Available(
                rawX: 500,
                milliX: 0,
                milliY: 0,
                milliZ: -1000,
                milliDps: 1500,
                index: 7,
                qpc: 2_000_000);

            using (MotionCsvRecorder recorder = new(path))
            {
                estimator.Update(snapshot);
                Assert.True(recorder.TryWrite(snapshot, estimator));
                Assert.Equal(1, recorder.RowCount);
            }

            string[] lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Equal(MotionCsvRecorder.Header, lines[0]);
            Assert.StartsWith("7,2000000,0.000,", lines[1], StringComparison.Ordinal);
            Assert.Contains(",1500,", lines[1], StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Write_SkipsDuplicateSampleIndex()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"motion-test-{Guid.NewGuid():N}.csv");
        try
        {
            MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
            DsMotionSnapshot first = Available(0, 0, 0, -1000, 0, 3, 1_000_000);
            DsMotionSnapshot dup = Available(0, 0, 0, -1000, 0, 3, 2_000_000);
            DsMotionSnapshot next = Available(0, 0, 0, -1000, 0, 4, 3_000_000);

            using (MotionCsvRecorder recorder = new(path))
            {
                estimator.Update(first);
                Assert.True(recorder.TryWrite(first, estimator));
                estimator.Update(dup);
                Assert.False(recorder.TryWrite(dup, estimator));
                estimator.Update(next);
                Assert.True(recorder.TryWrite(next, estimator));
                Assert.Equal(2, recorder.RowCount);
            }

            Assert.Equal(3, File.ReadAllLines(path).Length);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Write_UsesInvariantDecimalPoint()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"motion-test-{Guid.NewGuid():N}.csv");
        try
        {
            MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
            DsMotionSnapshot first = Available(0, 0, 0, -1000, 90_000, 1, 0);
            DsMotionSnapshot second = Available(0, 0, 0, -1000, 90_000, 2, 100_000);

            using (MotionCsvRecorder recorder = new(path))
            {
                estimator.Update(first);
                recorder.TryWrite(first, estimator);
                estimator.Update(second);
                recorder.TryWrite(second, estimator);
            }

            string row = File.ReadAllLines(path)[2];
            string[] cells = row.Split(',');
            Assert.Equal("100.000", cells[2]);
            Assert.Equal("9.000", cells[^1]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static DsMotionSnapshot Available(
        ushort rawX,
        int milliX,
        int milliY,
        int milliZ,
        int milliDps,
        uint index,
        ulong qpc)
    {
        return new DsMotionSnapshot
        {
            Flags = DsMotionSnapshotFlags.Available,
            RawAccelX = rawX,
            AccelMilliGX = milliX,
            AccelMilliGY = milliY,
            AccelMilliGZ = milliZ,
            GyroMilliDps = milliDps,
            SampleIndex = index,
            TimestampQpc = qpc
        };
    }
}
