using System.Diagnostics;
using System.IO;
using System.Text;

using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

/// <summary>
///     Live telemetry source for the guided capture; abstracts the IPC so the runner is testable.
/// </summary>
public interface IControllerTelemetrySource : IDisposable
{
    bool HasMotionTelemetry { get; }

    bool HasInputReportMetrics { get; }

    bool TryGetMotion(out DsMotionSnapshot snapshot, TimeSpan timeout);

    bool TryGetMetrics(out DsInputReportMetrics metrics);
}

/// <summary>
///     <see cref="IControllerTelemetrySource" /> over the driver IPC for one slot.
/// </summary>
public sealed class DsHidMiniTelemetrySource : IControllerTelemetrySource
{
    private readonly int _deviceIndex;
    private readonly DsHidMiniInterop _interop = new();

    public DsHidMiniTelemetrySource(int deviceIndex)
    {
        _deviceIndex = deviceIndex;
    }

    public bool HasMotionTelemetry => _interop.HasMotionTelemetry;

    public bool HasInputReportMetrics => _interop.HasInputReportMetrics;

    public bool TryGetMotion(out DsMotionSnapshot snapshot, TimeSpan timeout)
    {
        return _interop.GetMotionSnapshot(_deviceIndex, out snapshot, timeout);
    }

    public bool TryGetMetrics(out DsInputReportMetrics metrics)
    {
        return _interop.GetInputReportMetrics(_deviceIndex, out metrics, null);
    }

    public void Dispose()
    {
        _interop.Dispose();
    }
}

/// <summary>
///     Progress notification for the guided capture UI.
/// </summary>
public sealed record GuidedCaptureProgress(
    int PhaseIndex,
    ControllerCapturePhase Phase,
    TimeSpan Elapsed,
    TimeSpan Total,
    int SampleCount);

/// <summary>
///     Runs the timed phases of the guided capture and condenses them into a
///     <see cref="ControllerTelemetryCapture" />.
/// </summary>
public static class GuidedCaptureRunner
{
    /// <summary>
    ///     Default 15-second script: still, tilt, shake.
    /// </summary>
    public static IReadOnlyList<ControllerCapturePhase> DefaultPhases { get; } = new[]
    {
        new ControllerCapturePhase("Still", "Put the controller flat on a table and do not touch it.",
            TimeSpan.FromSeconds(5)),
        new ControllerCapturePhase("Tilt", "Slowly tilt the controller left, right, forward and back.",
            TimeSpan.FromSeconds(5)),
        new ControllerCapturePhase("Shake", "Shake the controller gently and rotate it around.",
            TimeSpan.FromSeconds(5))
    };

    private const int MaxConsecutiveMisses = 75; // ~3 s at the 40 ms poll below

    public static async Task<ControllerTelemetryCapture> RunAsync(
        IControllerTelemetrySource source,
        IReadOnlyList<ControllerCapturePhase> phases,
        IProgress<GuidedCaptureProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(phases);

        return await Task.Run(() => Run(source, phases, progress, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    internal static ControllerTelemetryCapture Run(
        IControllerTelemetrySource source,
        IReadOnlyList<ControllerCapturePhase> phases,
        IProgress<GuidedCaptureProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!source.HasMotionTelemetry)
        {
            return new ControllerTelemetryCapture
            {
                MotionAvailable = false,
                Note = "The installed driver does not expose motion telemetry; no live capture was recorded."
            };
        }

        TimeSpan total = TimeSpan.Zero;
        foreach (ControllerCapturePhase phase in phases)
        {
            total += phase.Duration;
        }

        using MemoryStream csvStream = new();
        MotionOrientationEstimator estimator = new(Stopwatch.Frequency);
        List<ControllerPhaseResult> results = new();
        List<ControllerReportRateSample> rates = new();
        string? note = null;
        int totalSamples = 0;
        int lastMetricsSequence = int.MinValue;
        double lastMetricsPollMs = -1000;
        bool disconnected = false;

        Stopwatch clock = Stopwatch.StartNew();
        using (MotionCsvRecorder recorder = new("memory", new StreamWriter(
                   csvStream, new UTF8Encoding(false), 4096, leaveOpen: true)))
        {
            for (int p = 0; p < phases.Count && !disconnected; p++)
            {
                ControllerCapturePhase phase = phases[p];
                double startMs = clock.Elapsed.TotalMilliseconds;
                double endTarget = startMs + phase.Duration.TotalMilliseconds;
                HashSet<ushort> ax = new(), ay = new(), az = new(), gyro = new();
                int samples = 0;
                int misses = 0;
                uint lastIndex = 0;
                bool haveIndex = false;

                while (clock.Elapsed.TotalMilliseconds < endTarget)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    DsMotionSnapshot snapshot;
                    bool got;
                    try
                    {
                        got = source.TryGetMotion(out snapshot, TimeSpan.FromMilliseconds(40));
                    }
                    catch (Exception ex)
                    {
                        note = $"Live capture stopped: {ex.Message}";
                        disconnected = true;
                        break;
                    }

                    if (got && snapshot.SlotIndex == 0)
                    {
                        note = "The controller disconnected during the capture.";
                        disconnected = true;
                        break;
                    }

                    if (!got)
                    {
                        if (++misses >= MaxConsecutiveMisses)
                        {
                            note = "No motion samples arrived for several seconds; the capture is incomplete.";
                            disconnected = true;
                            break;
                        }
                    }
                    else
                    {
                        misses = 0;
                        if (snapshot.IsAvailable && (!haveIndex || snapshot.SampleIndex != lastIndex))
                        {
                            haveIndex = true;
                            lastIndex = snapshot.SampleIndex;
                            estimator.Update(snapshot);
                            recorder.TryWrite(snapshot, estimator);
                            samples++;
                            ax.Add(snapshot.RawAccelX);
                            ay.Add(snapshot.RawAccelY);
                            az.Add(snapshot.RawAccelZ);
                            gyro.Add(snapshot.RawGyro);
                        }
                    }

                    double nowMs = clock.Elapsed.TotalMilliseconds;
                    if (source.HasInputReportMetrics && nowMs - lastMetricsPollMs >= 1000)
                    {
                        lastMetricsPollMs = nowMs;
                        try
                        {
                            if (source.TryGetMetrics(out DsInputReportMetrics metrics)
                                && metrics.SequenceNumber != lastMetricsSequence)
                            {
                                lastMetricsSequence = metrics.SequenceNumber;
                                rates.Add(new ControllerReportRateSample(nowMs, metrics.ReportRateHz, metrics.AverageIntervalUs));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }

                    progress?.Report(new GuidedCaptureProgress(
                        p, phase, TimeSpan.FromMilliseconds(nowMs), total, totalSamples + samples));
                }

                double endMs = clock.Elapsed.TotalMilliseconds;
                double seconds = Math.Max(0.001, (endMs - startMs) / 1000.0);
                totalSamples += samples;
                results.Add(new ControllerPhaseResult
                {
                    Name = phase.Name,
                    StartMs = startMs,
                    EndMs = endMs,
                    SampleCount = samples,
                    SamplesPerSecond = samples / seconds,
                    DistinctRawAccelX = ax.Count,
                    DistinctRawAccelY = ay.Count,
                    DistinctRawAccelZ = az.Count,
                    DistinctRawGyro = gyro.Count
                });
            }
        }

        if (totalSamples == 0 && note is null)
        {
            note = "The driver exposed motion telemetry but no samples were received.";
        }

        return new ControllerTelemetryCapture
        {
            MotionAvailable = totalSamples > 0,
            Note = note,
            MotionCsv = totalSamples > 0 ? csvStream.ToArray() : Array.Empty<byte>(),
            Phases = results,
            ReportRates = rates
        };
    }
}
