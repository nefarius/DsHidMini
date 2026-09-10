using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Diagnostic pad pose: smoothed gravity for pitch (USB/trigger edge) and
///     roll (left/right grips), integrated yaw from
///     the single SIXAXIS gyro. Rest bias below
///     <see cref="DefaultYawRestDeadzoneDps" /> is ignored. Call
///     <see cref="Recenter" /> to zero accumulated yaw.
/// </summary>
internal sealed class MotionOrientationEstimator
{
    public const double DefaultYawRestDeadzoneDps = 4.0;

    private readonly double _smoothing;
    private readonly double _yawRestDeadzoneDps;
    private bool _hasGravity;
    private bool _hasTiming;
    private ulong _lastQpc;
    private double _smoothX;
    private double _smoothY;
    private double _smoothZ;
    private uint _lastSampleIndex;

    public MotionOrientationEstimator(
        double qpcFrequency,
        double smoothing = 0.18,
        double yawRestDeadzoneDps = DefaultYawRestDeadzoneDps)
    {
        QpcFrequency = qpcFrequency > 0 ? qpcFrequency : 10_000_000;
        _smoothing = Math.Clamp(smoothing, 0.01, 1.0);
        _yawRestDeadzoneDps = Math.Max(0, yawRestDeadzoneDps);
    }

    public double QpcFrequency { get; }

    public double PitchDegrees { get; private set; }

    public double RollDegrees { get; private set; }

    public double YawDegrees { get; private set; }

    public double SmoothMilliGX => _smoothX;

    public double SmoothMilliGY => _smoothY;

    public double SmoothMilliGZ => _smoothZ;

    public void Recenter()
    {
        YawDegrees = 0;
    }

    public void Reset()
    {
        _hasGravity = false;
        _hasTiming = false;
        _lastQpc = 0;
        _lastSampleIndex = 0;
        _smoothX = 0;
        _smoothY = 0;
        _smoothZ = -1000;
        PitchDegrees = 0;
        RollDegrees = 0;
        YawDegrees = 0;
    }

    public void Update(in DsMotionSnapshot snapshot)
    {
        if (!snapshot.IsAvailable)
        {
            return;
        }

        if (snapshot.SampleIndex == _lastSampleIndex && _hasGravity)
        {
            return;
        }

        double gx = snapshot.AccelMilliGX;
        double gy = snapshot.AccelMilliGY;
        double gz = snapshot.AccelMilliGZ;

        if (!_hasGravity)
        {
            _smoothX = gx;
            _smoothY = gy;
            _smoothZ = gz;
            _hasGravity = true;
        }
        else
        {
            _smoothX += (gx - _smoothX) * _smoothing;
            _smoothY += (gy - _smoothY) * _smoothing;
            _smoothZ += (gz - _smoothZ) * _smoothing;
        }

        double ax = _smoothX / 1000.0;
        double ay = _smoothY / 1000.0;
        double az = _smoothZ / 1000.0;
        double horiz = Math.Sqrt((ay * ay) + (az * az));

        // Aviation/gamepad frame: USB/trigger edge is the nose (pitch from Y),
        // grips are the wings (roll from X). Sony +1 g is axis-up.
        PitchDegrees = Math.Atan2(ay, -az) * (180.0 / Math.PI);
        RollDegrees = Math.Atan2(-ax, horiz) * (180.0 / Math.PI);

        if (_hasTiming && snapshot.TimestampQpc > _lastQpc)
        {
            double dt = (snapshot.TimestampQpc - _lastQpc) / QpcFrequency;
            if (dt > 0 && dt < 0.25)
            {
                double dps = snapshot.GyroMilliDps / 1000.0;
                if (Math.Abs(dps) >= _yawRestDeadzoneDps)
                {
                    YawDegrees += dps * dt;
                }
            }
        }

        _hasTiming = true;
        _lastQpc = snapshot.TimestampQpc;
        _lastSampleIndex = snapshot.SampleIndex;
    }
}
