using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Services;

/// <summary>
///     Reads DS3 motion data via DsHidMini IPC, applies gyro calibration,
///     Kalman filtering, and orientation estimation, then broadcasts through
///     the Cemuhook DSU protocol.
/// </summary>
internal sealed class CemuhookBridgeService : IDisposable
{
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

        while (!cancellationToken.IsCancellationRequested)
        {
            bool received = interop.GetRawInputReport(
                _deviceSlot,
                ref report,
                TimeSpan.FromMilliseconds(100));
            if (!received)
                continue;

            ushort rawAccelX = BinaryPrimitives.ReverseEndianness(report.AccelerometerX);
            ushort rawAccelY = BinaryPrimitives.ReverseEndianness(report.AccelerometerY);
            ushort rawAccelZ = BinaryPrimitives.ReverseEndianness(report.AccelerometerZ);
            ushort rawGyro = BinaryPrimitives.ReverseEndianness(report.Gyroscope);

            int calibratedGyro = gyroCal.Runtime(
                rawGyro,
                out byte currentCalByte,
                out bool calChanged);

            double gyroOutput = DsMotionMath.ToMilliDps(calibratedGyro) / 1000.0 * _motionSettings.GyroSensitivity;

            // accelX and accelY are mirrored to match SixAxisBridge behavior (InvertTilt = false => mirror: true)
            int accelX = DsMotionMath.CalibrateAccel(rawAccelX, 508, 395, mirror: true);
            int accelY = DsMotionMath.CalibrateAccel(rawAccelY, 517, 404, mirror: true);
            int accelZ = DsMotionMath.CalibrateAccel(rawAccelZ, 511, 395, mirror: false);

            double accelXG = DsMotionMath.ToMilliG(accelX) / 1000.0 * _motionSettings.AccelSensitivity;
            double accelYG = DsMotionMath.ToMilliG(accelY) / 1000.0 * _motionSettings.AccelSensitivity;
            double accelZG = DsMotionMath.ToMilliG(accelZ) / 1000.0 * _motionSettings.AccelSensitivity;

            double filterStrength = _motionSettings.KalmanFilterStrength;

            double filteredAccelX = accelFilterX.Update(accelXG, filterStrength);
            double filteredAccelY = accelFilterY.Update(accelYG, filterStrength);
            double filteredAccelZ = accelFilterZ.Update(accelZG, filterStrength);
            double filteredGyroOutput = gyroYawFilter.Update(gyroOutput, filterStrength);

            orientationEstimator.Smoothing = Math.Clamp(filterStrength, 0.01, 1.0);

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
