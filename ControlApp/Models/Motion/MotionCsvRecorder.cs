using System.Globalization;
using System.IO;
using System.Text;

using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     One CSV row per new <see cref="DsMotionSnapshot.SampleIndex" />.
///     Duplicate indices are skipped. Uses <see cref="CultureInfo.InvariantCulture" />.
/// </summary>
internal sealed class MotionCsvRecorder : IDisposable
{
    public const string Header =
        "SampleIndex,TimestampQpc,DtMs,RawAccelX,RawAccelY,RawAccelZ,CalAccelX,CalAccelY,CalAccelZ,AccelMilliGX,AccelMilliGY,AccelMilliGZ,RawGyro,CalGyro,GyroMilliDps,ZeroRef,CalByte,Flags,MotionPath,Pitch,Roll,Yaw";

    private readonly StreamWriter _writer;
    private bool _disposed;
    private bool _hasSample;
    private ulong _lastQpc;
    private uint _lastSampleIndex;

    public MotionCsvRecorder(string path)
        : this(path, new StreamWriter(File.Create(path), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
    {
    }

    internal MotionCsvRecorder(string path, StreamWriter writer)
    {
        Path = path;
        _writer = writer;
        _writer.WriteLine(Header);
    }

    public string Path { get; }

    public int RowCount { get; private set; }

    public static string CreateDefaultPath(int slotIndex)
    {
        return CreateUniquePath(slotIndex, DateTime.Now, ResolveLogDirectory());
    }

    public static MotionCsvRecorder Start(int slotIndex)
    {
        return Start(slotIndex, DateTime.Now, ResolveLogDirectory());
    }

    internal static string CreateUniquePath(int slotIndex, DateTime stamp, string directory)
    {
        Directory.CreateDirectory(directory);
        for (int n = 0; n < 1000; n++)
        {
            string path = FormatPath(directory, slotIndex, stamp, n);
            if (!File.Exists(path))
            {
                return path;
            }
        }

        throw new IOException($"Could not allocate a unique motion recording path for slot {slotIndex}.");
    }

    internal static MotionCsvRecorder Start(int slotIndex, DateTime stamp, string directory)
    {
        Directory.CreateDirectory(directory);
        IOException? last = null;
        for (int n = 0; n < 1000; n++)
        {
            string path = FormatPath(directory, slotIndex, stamp, n);
            try
            {
                FileStream stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return new MotionCsvRecorder(path, writer);
            }
            catch (IOException ex)
            {
                last = ex;
            }
        }

        throw new IOException("Could not create a unique motion recording file.", last);
    }

    private static string ResolveLogDirectory()
    {
        try
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DsHidMini",
                "Log",
                "Motion");
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch (Exception)
        {
            return System.IO.Path.GetTempPath();
        }
    }

    private static string FormatPath(string directory, int slotIndex, DateTime stamp, int suffix)
    {
        string stampText = stamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string fileName = suffix == 0
            ? $"motion-slot{slotIndex}-{stampText}.csv"
            : $"motion-slot{slotIndex}-{stampText}-{suffix}.csv";
        return System.IO.Path.Combine(directory, fileName);
    }

    public bool TryWrite(in DsMotionSnapshot snapshot, MotionOrientationEstimator estimator)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!snapshot.IsAvailable)
        {
            return false;
        }

        if (_hasSample && snapshot.SampleIndex == _lastSampleIndex)
        {
            return false;
        }

        double dtMs = 0;
        if (_hasSample && snapshot.TimestampQpc > _lastQpc && estimator.QpcFrequency > 0)
        {
            dtMs = (snapshot.TimestampQpc - _lastQpc) / estimator.QpcFrequency * 1000.0;
        }

        CultureInfo inv = CultureInfo.InvariantCulture;
        _writer.WriteLine(string.Join(
            ',',
            snapshot.SampleIndex.ToString(inv),
            snapshot.TimestampQpc.ToString(inv),
            dtMs.ToString("0.000", inv),
            snapshot.RawAccelX.ToString(inv),
            snapshot.RawAccelY.ToString(inv),
            snapshot.RawAccelZ.ToString(inv),
            snapshot.CalAccelX.ToString(inv),
            snapshot.CalAccelY.ToString(inv),
            snapshot.CalAccelZ.ToString(inv),
            snapshot.AccelMilliGX.ToString(inv),
            snapshot.AccelMilliGY.ToString(inv),
            snapshot.AccelMilliGZ.ToString(inv),
            snapshot.RawGyro.ToString(inv),
            snapshot.CalGyro.ToString(inv),
            snapshot.GyroMilliDps.ToString(inv),
            snapshot.ZeroRef.ToString(inv),
            snapshot.CalByte.ToString(inv),
            ((ushort)snapshot.Flags).ToString(inv),
            ((byte)snapshot.MotionPath).ToString(inv),
            estimator.PitchDegrees.ToString("0.000", inv),
            estimator.RollDegrees.ToString("0.000", inv),
            estimator.YawDegrees.ToString("0.000", inv)));

        _hasSample = true;
        _lastSampleIndex = snapshot.SampleIndex;
        _lastQpc = snapshot.TimestampQpc;
        RowCount++;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer.Flush();
        _writer.Dispose();
    }
}
