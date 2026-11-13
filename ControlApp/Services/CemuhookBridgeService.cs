using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Reads DS3 motion data via DsHidMini IPC, applies gyro calibration,
///     Kalman filtering, and orientation estimation, then broadcasts through
///     the Cemuhook DSU protocol.
/// </summary>
internal sealed class CemuhookBridgeService : IDisposable
{
    private const int MissedReadTimeoutMs = 2000;

    private readonly int _deviceSlot;
    private readonly CemuhookUdpSettings _udpSettings;
    private readonly MotionControlsSettings _motionSettings;
    private readonly CancellationTokenSource _cts;
    private readonly Task _bridgeTask;
    private readonly Task? _dsuTask;
    private readonly CemuhookServer _dsuServer;
    private readonly object _latestLock = new();
    private Ds3MotionSnapshot _latestSnapshot = new(false, 0, 0, 0, 0, 0, 0);

    public Ds3MotionSnapshot LatestSnapshot
    {
        get
        {
            lock (_latestLock)
                return _latestSnapshot;
        }
    }

    public CemuhookBridgeService(
        int deviceSlot,
        CemuhookUdpSettings udpSettings,
        MotionControlsSettings motionSettings)
    {
        _deviceSlot = deviceSlot;
        _udpSettings = udpSettings;
        _motionSettings = motionSettings;
        _cts = new CancellationTokenSource();
        _dsuServer = new CemuhookServer(
            udpSettings.Port,
            udpSettings.IpAddress,
            () => LatestSnapshot);

        _bridgeTask = Task.Run(() => RunBridge(_cts.Token));
        _dsuTask = Task.Run(async () => await _dsuServer.RunAsync(_cts.Token));

        // Observe faults so a crash in one worker stops the other.
        _ = Task.Run(async () =>
        {
            var completed = await Task.WhenAny(_bridgeTask, _dsuTask!);
            if (completed.IsFaulted)
            {
                Log.Logger.Error(completed.Exception, "Cemuhook bridge worker faulted.");
            }

            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _dsuServer.Dispose();
        _cts.Dispose();
    }

    private void PublishSnapshot(Ds3MotionSnapshot snapshot)
    {
        lock (_latestLock)
        {
            _latestSnapshot = snapshot;
        }
    }

    private void RunBridge(CancellationToken cancellationToken)
    {
        if (!DsHidMiniInterop.IsAvailable)
        {
            Log.Logger.Warning("DsHidMini IPC is unavailable. Cemuhook bridge cannot start.");
            return;
        }

        using var interop = new DsHidMiniInterop();
        interop.SendPing();

        var gyroCal = new GyroCal();
        gyroCal.Initial(130, 522);

        var orientationEstimator = new MotionOrientationEstimator(
            Stopwatch.Frequency,
            smoothing: 0.18);

        var accelFilterX = new KalmanFilter1D(q: 0.01, r: 0.05);
        var accelFilterY = new KalmanFilter1D(q: 0.01, r: 0.05);
        var accelFilterZ = new KalmanFilter1D(q: 0.01, r: 0.05);
        var gyroYawFilter = new KalmanFilter1D(q: 0.05, r: 0.2);
        var pitchRateFilter = new KalmanFilter1D(q: 0.05, r: 0.2);
        var rollRateFilter = new KalmanFilter1D(q: 0.05, r: 0.2);

        bool hasPreviousTilt = false;
        Quaternion previousTilt = Quaternion.Identity;
        long previousOrientationTimestamp = 0;

        var report = default(DS3_RAW_INPUT_REPORT);
        long lastReadTimestamp = Stopwatch.GetTimestamp();

        while (!cancellationToken.IsCancellationRequested)
        {
            bool received = interop.GetRawInputReport(
                _deviceSlot,
                ref report,
                TimeSpan.FromMilliseconds(100));

            if (!received)
            {
                // Publish inactive snapshot after a bounded miss interval so stale
                // motion values are not broadcast as fresh data.
                double missedMs = Stopwatch.GetElapsedTime(lastReadTimestamp).TotalMilliseconds;
                if (missedMs >= MissedReadTimeoutMs)
                {
                    PublishSnapshot(new Ds3MotionSnapshot(false, 0, 0, 0, 0, 0, 0));
                    return;
                }

                continue;
            }

            lastReadTimestamp = Stopwatch.GetTimestamp();

            ushort rawAccelX = BinaryPrimitives.ReverseEndianness(report.AccelerometerX);
            ushort rawAccelY = BinaryPrimitives.ReverseEndianness(report.AccelerometerY);
            ushort rawAccelZ = BinaryPrimitives.ReverseEndianness(report.AccelerometerZ);
            ushort rawGyro = BinaryPrimitives.ReverseEndianness(report.Gyroscope);

            // Check whether the driver provides per-device calibration data.
            // When Fallback is set, EEPROM was not read; use nominal values.
            ushort accelZeroX = 512, accelZeroY = 512, accelZeroZ = 512;
            ushort accelOneGX = 399, accelOneGY = 399, accelOneGZ = 399;

            if (interop.GetMotionSnapshot(_deviceSlot, out var motionSnap, TimeSpan.Zero) &&
                motionSnap.IsAvailable && !motionSnap.IsFallback)
            {
                accelZeroX = motionSnap.AccelZeroX;
                accelZeroY = motionSnap.AccelZeroY;
                accelZeroZ = motionSnap.AccelZeroZ;
                accelOneGX = motionSnap.AccelOneGX;
                accelOneGY = motionSnap.AccelOneGY;
                accelOneGZ = motionSnap.AccelOneGZ;
            }

            // For HwCal path the driver already applies the hardware cal byte;
            // for Sixaxis the GyroCal tracker provides the adjustment.
            // For PlainZero the driver uses software-only tracking, so we skip
            // the GyroCal retarget offset and use the raw gyro directly.
            DsIdentificationMotionPath motionPath = DsIdentificationMotionPath.Unknown;
            if (interop.GetMotionSnapshot(_deviceSlot, out motionSnap, TimeSpan.Zero) &&
                motionSnap.IsAvailable)
            {
                motionPath = motionSnap.MotionPath;
            }

            int calibratedGyro;
            if (motionPath == DsIdentificationMotionPath.PlainZero)
            {
                calibratedGyro = rawGyro;
            }
            else
            {
                calibratedGyro = gyroCal.Runtime(
                    rawGyro,
                    out byte currentCalByte,
                    out bool calChanged);
            }

            double gyroOutput = DsMotionMath.ToMilliDps(calibratedGyro) / 1000.0 * _motionSettings.GyroSensitivity;

            // accelX and accelY are mirrored to match SixAxisBridge behavior (InvertTilt = false => mirror: true)
            int accelX = DsMotionMath.CalibrateAccel(rawAccelX, accelZeroX, accelOneGX, mirror: true);
            int accelY = DsMotionMath.CalibrateAccel(rawAccelY, accelZeroY, accelOneGY, mirror: true);
            int accelZ = DsMotionMath.CalibrateAccel(rawAccelZ, accelZeroZ, accelOneGZ, mirror: false);

            double accelXG = DsMotionMath.ToMilliG(accelX) / 1000.0 * _motionSettings.AccelSensitivity;
            double accelYG = DsMotionMath.ToMilliG(accelY) / 1000.0 * _motionSettings.AccelSensitivity;
            double accelZG = DsMotionMath.ToMilliG(accelZ) / 1000.0 * _motionSettings.AccelSensitivity;

            double filterStrength = _motionSettings.KalmanFilterStrength;

            double filteredAccelX = accelFilterX.Update(accelXG, filterStrength);
            double filteredAccelY = accelFilterY.Update(accelYG, filterStrength);
            double filteredAccelZ = accelFilterZ.Update(accelZG, filterStrength);
            double filteredGyroOutput = gyroYawFilter.Update(gyroOutput, filterStrength);

            // Decouple gravity smoothing from Kalman filter strength at the minimum
            // so a 0.001 filter setting does not impose only 0.01 gravity weight.
            double gravityWeight = filterStrength <= 0.001 ? 0.18 : Math.Clamp(filterStrength, 0.01, 1.0);
            orientationEstimator.Smoothing = gravityWeight;

            var motionSnapshot = new Ds3MotionSnapshot(
                true,
                filteredAccelX,
                filteredAccelY,
                filteredAccelZ,
                filteredGyroOutput,
                0,
                0);
            orientationEstimator.Update(in motionSnapshot);

            Quaternion currentTilt = MotionOrientationEstimator.CreateTiltQuaternion(
                orientationEstimator.UpX,
                orientationEstimator.UpY,
                orientationEstimator.UpZ);

            long currentTimestamp = Stopwatch.GetTimestamp();
            double rawPitchRate = 0;
            double rawRollRate = 0;

            if (hasPreviousTilt)
            {
                double dt = (currentTimestamp - previousOrientationTimestamp) / (double)Stopwatch.Frequency;
                (double omegaX, double omegaY) = MotionOrientationEstimator.CalculateAngularVelocity(
                    previousTilt,
                    currentTilt,
                    dt);
                rawPitchRate = omegaX;
                rawRollRate = omegaY;
            }

            double filteredPitchRate = pitchRateFilter.Update(rawPitchRate, filterStrength);
            double filteredRollRate = rollRateFilter.Update(rawRollRate, filterStrength);

            previousTilt = currentTilt;
            previousOrientationTimestamp = currentTimestamp;
            hasPreviousTilt = true;

            PublishSnapshot(new Ds3MotionSnapshot(
                true,
                filteredAccelX,
                filteredAccelY,
                filteredAccelZ,
                filteredGyroOutput,
                filteredPitchRate,
                filteredRollRate));
        }

        PublishSnapshot(new Ds3MotionSnapshot(false, 0, 0, 0, 0, 0, 0));
    }
}
