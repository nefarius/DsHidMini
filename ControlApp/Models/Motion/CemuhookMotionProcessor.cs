using System.Diagnostics;
using System.Numerics;

using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager;
using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>Filters one coherent, driver-calibrated sample at its acquisition time.</summary>
internal sealed class CemuhookMotionProcessor(MotionControlsSettings settings)
{
    private readonly MotionOrientationEstimator _orientation = new(Stopwatch.Frequency, smoothing: 0.18);
    private readonly KalmanFilter1D _accelX = new(0.01, 0.05);
    private readonly KalmanFilter1D _accelY = new(0.01, 0.05);
    private readonly KalmanFilter1D _accelZ = new(0.01, 0.05);
    private readonly KalmanFilter1D _yaw = new(0.05, 0.2);
    private readonly KalmanFilter1D _pitch = new(0.05, 0.2);
    private readonly KalmanFilter1D _roll = new(0.05, 0.2);
    private bool _hasPrevious;
    private uint _previousSample;
    private ulong _previousTimestamp;
    private Quaternion _previousTilt;

    public bool TryProcess(in DsMotionSnapshot sample, out Ds3MotionSnapshot snapshot)
    {
        snapshot = null!;
        if (!sample.IsAvailable || sample.Version != DsMotionSnapshot.CurrentVersion || sample.TimestampQpc == 0)
            return false;

        if (_hasPrevious && (sample.SampleIndex == _previousSample || sample.TimestampQpc <= _previousTimestamp))
            return false;

        double dt = _hasPrevious
            ? (sample.TimestampQpc - _previousTimestamp) / (double)Stopwatch.Frequency
            : 0;
        if (dt >= 0.25)
            Reset();

        double strength = settings.KalmanFilterStrength;
        double x = _accelX.Update(sample.AccelMilliGX / 1000.0 * settings.AccelSensitivity, strength);
        // Preserve the bridge's Y-axis convention; the driver already mirrors X.
        double y = _accelY.Update(-sample.AccelMilliGY / 1000.0 * settings.AccelSensitivity, strength);
        double z = _accelZ.Update(sample.AccelMilliGZ / 1000.0 * settings.AccelSensitivity, strength);
        double yaw = _yaw.Update(sample.GyroMilliDps / 1000.0 * settings.GyroSensitivity, strength);

        _orientation.Smoothing = strength <= 0.001 ? 0.18 : Math.Clamp(strength, 0.01, 1.0);
        var filtered = new Ds3MotionSnapshot(true, x, y, z, yaw, 0, 0, sample.TimestampQpc);
        _orientation.Update(in filtered);
        Quaternion tilt = MotionOrientationEstimator.CreateTiltQuaternion(_orientation.UpX, _orientation.UpY, _orientation.UpZ);
        var rates = _hasPrevious
            ? MotionOrientationEstimator.CalculateAngularVelocity(_previousTilt, tilt, dt)
            : (OmegaX: 0.0, OmegaY: 0.0);

        snapshot = filtered with
        {
            PitchDegreesPerSecond = _pitch.Update(rates.OmegaX, strength),
            RollDegreesPerSecond = _roll.Update(rates.OmegaY, strength)
        };
        _hasPrevious = true;
        _previousSample = sample.SampleIndex;
        _previousTimestamp = sample.TimestampQpc;
        _previousTilt = tilt;
        return true;
    }

    public void Reset()
    {
        _hasPrevious = false;
        _orientation.Reset();
        _accelX.Reset();
        _accelY.Reset();
        _accelZ.Reset();
        _yaw.Reset();
        _pitch.Reset();
        _roll.Reset();
    }
}
