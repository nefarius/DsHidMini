using System.Diagnostics;
using System.Numerics;

using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Diagnostic pad pose: smoothed gravity for pitch (USB/trigger edge) and
///     roll (left/right grips), integrated yaw from
///     the single SIXAXIS gyro. A rest-rate estimate learned while the pad
///     is flat and still is frozen and subtracted from samples that already
///     pass the <see cref="DefaultYawRestDeadzoneDps" /> deadzone. Yaw is weighted
///     by <c>-u_z</c> so a standing-on-grip gyro (axis horizontal) does not
///     accumulate heading. Call <see cref="Recenter" /> to zero accumulated yaw.
/// </summary>
internal sealed class MotionOrientationEstimator
{
    public const double DefaultYawRestDeadzoneDps = 4.0;

    public const double PitchHoldHorizG = 0.15;

    /// <summary>
    ///     Consecutive flat-still samples (~1 s at 100 Hz) before the rest
    ///     bias is frozen.
    /// </summary>
    public const int BiasLearnSamples = 100;

    public const double BiasLearnFlatUpZ = -0.85;

    public const double BiasLearnGTolerance = 0.15;

    public const double BiasLearnAccelJitterMilliG = 50;

    private const double BiasLearnMaxAbsDps = 2.0;

    private const double BiasLearnMaxRangeDps = 1.5;

    private double _smoothing;
    private readonly double _yawRestDeadzoneDps;
    private readonly double _biasLearnMaxAbsDps;
    private bool _hasGravity;
    private bool _hasTiming;
    private ulong _lastQpc;
    private double _smoothX;
    private double _smoothY;
    private double _smoothZ;
    private uint _lastSampleIndex;
    private bool _hasPrevAccel;
    private int _prevMilliGX;
    private int _prevMilliGY;
    private int _prevMilliGZ;
    private int _stillCount;
    private double _biasSum;
    private double _biasMin = double.PositiveInfinity;
    private double _biasMax = double.NegativeInfinity;

    public MotionOrientationEstimator(
        double qpcFrequency,
        double smoothing = 0.18,
        double yawRestDeadzoneDps = DefaultYawRestDeadzoneDps)
    {
        QpcFrequency = qpcFrequency > 0 ? qpcFrequency : 10_000_000;
        _smoothing = Math.Clamp(smoothing, 0.01, 1.0);
        _yawRestDeadzoneDps = Math.Max(0, yawRestDeadzoneDps);
        _biasLearnMaxAbsDps = Math.Min(BiasLearnMaxAbsDps, _yawRestDeadzoneDps);
    }

    public double QpcFrequency { get; }

    public double Smoothing
    {
        get => _smoothing;
        set => _smoothing = Math.Clamp(value, 0.001, 1.0);
    }

    public double PitchDegrees { get; private set; }

    public double RollDegrees { get; private set; }

    public double YawDegrees { get; private set; }

    public double SmoothMilliGX => _smoothX;

    public double SmoothMilliGY => _smoothY;

    public double SmoothMilliGZ => _smoothZ;

    /// <summary>
    ///     Smoothed unit gravity in the Sony pad frame (+1 g = axis up).
    ///     Flat buttons-up is <c>(0, 0, -1)</c>.
    /// </summary>
    public double UpX { get; private set; }

    public double UpY { get; private set; }

    public double UpZ { get; private set; } = -1;

    /// <summary>
    ///     Learned rest rate in deg/s, frozen after the first clean rest
    ///     and subtracted from turns that already pass the deadzone.
    ///     Zero until <see cref="HasRestBias" />.
    /// </summary>
    public double RestBiasDps { get; private set; }

    public bool HasRestBias { get; private set; }

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
        UpX = 0;
        UpY = 0;
        UpZ = -1;
        PitchDegrees = 0;
        RollDegrees = 0;
        YawDegrees = 0;
        ClearRestBias();
        _hasPrevAccel = false;
        _prevMilliGX = 0;
        _prevMilliGY = 0;
        _prevMilliGZ = 0;
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

        UpdateCore(
            snapshot.AccelMilliGX,
            snapshot.AccelMilliGY,
            snapshot.AccelMilliGZ,
            snapshot.GyroMilliDps / 1000.0,
            snapshot.TimestampQpc,
            snapshot.AccelMilliGX,
            snapshot.AccelMilliGY,
            snapshot.AccelMilliGZ);

        _lastSampleIndex = snapshot.SampleIndex;
    }

    /// <summary>
    ///     Updates orientation from a Cemuhook bridge snapshot (g-force values instead of milli-g).
    /// </summary>
    internal void Update(in Ds3MotionSnapshot snapshot)
    {
        if (!snapshot.Connected)
            return;

        ulong timestampQpc = snapshot.TimestampQpc != 0
            ? snapshot.TimestampQpc
            : (ulong)Stopwatch.GetTimestamp();
        double gx = snapshot.AccelXG * 1000.0;
        double gy = snapshot.AccelYG * 1000.0;
        double gz = snapshot.AccelZG * 1000.0;

        UpdateCore(
            gx,
            gy,
            gz,
            snapshot.GyroAxisDegreesPerSecond,
            timestampQpc,
            (int)Math.Round(gx),
            (int)Math.Round(gy),
            (int)Math.Round(gz));
    }

    private void UpdateCore(
        double accelMilliGX,
        double accelMilliGY,
        double accelMilliGZ,
        double rawDps,
        ulong timestampQpc,
        int accelMilliGXInt,
        int accelMilliGYInt,
        int accelMilliGZInt)
    {
        // Smooth gravity vector
        if (!_hasGravity)
        {
            _smoothX = accelMilliGX;
            _smoothY = accelMilliGY;
            _smoothZ = accelMilliGZ;
            _hasGravity = true;
        }
        else
        {
            _smoothX += (accelMilliGX - _smoothX) * _smoothing;
            _smoothY += (accelMilliGY - _smoothY) * _smoothing;
            _smoothZ += (accelMilliGZ - _smoothZ) * _smoothing;
        }

        double ax = _smoothX / 1000.0;
        double ay = _smoothY / 1000.0;
        double az = _smoothZ / 1000.0;

        double mag = Math.Sqrt((ax * ax) + (ay * ay) + (az * az));

        if (mag > 1e-6)
        {
            UpX = ax / mag;
            UpY = ay / mag;
            UpZ = az / mag;
        }

        double horiz = Math.Sqrt((ay * ay) + (az * az));

        // Aviation/gamepad frame: USB/trigger edge is the nose (pitch from Y),
        // grips are the wings (roll from X). Sony +1 g is axis-up.
        // On a grip, ay/az are noise and Atan2 flips; hold the last pitch.
        if (horiz >= PitchHoldHorizG)
        {
            PitchDegrees = Math.Atan2(ay, -az) * (180.0 / Math.PI);
        }

        RollDegrees = Math.Atan2(-ax, horiz) * (180.0 / Math.PI);

        // Integrate yaw from gyro
        if (_hasTiming && timestampQpc > _lastQpc)
        {
            double dt = (timestampQpc - _lastQpc) / QpcFrequency;
            if (dt > 0 && dt < 0.25)
            {
                if (Math.Abs(rawDps) >= _yawRestDeadzoneDps)
                {
                    YawDegrees += (rawDps - RestBiasDps) * (-UpZ) * dt;
                }
            }
        }

        UpdateRestBias(accelMilliGXInt, accelMilliGYInt, accelMilliGZInt, rawDps, mag);

        _hasTiming = true;
        _lastQpc = timestampQpc;
    }

    private void ClearRestBias()
    {
        HasRestBias = false;
        RestBiasDps = 0;
        ResetBiasWindow();
    }

    private void ResetBiasWindow()
    {
        _stillCount = 0;
        _biasSum = 0;
        _biasMin = double.PositiveInfinity;
        _biasMax = double.NegativeInfinity;
    }

    private void UpdateRestBias(
        int accelMilliGX,
        int accelMilliGY,
        int accelMilliGZ,
        double rawDps,
        double mag)
    {
        if (HasRestBias)
            return;

        bool accelStill = !_hasPrevAccel
            || (Math.Abs(accelMilliGX - _prevMilliGX) <= BiasLearnAccelJitterMilliG
            && Math.Abs(accelMilliGY - _prevMilliGY) <= BiasLearnAccelJitterMilliG
            && Math.Abs(accelMilliGZ - _prevMilliGZ) <= BiasLearnAccelJitterMilliG);

        _hasPrevAccel = true;
        _prevMilliGX = accelMilliGX;
        _prevMilliGY = accelMilliGY;
        _prevMilliGZ = accelMilliGZ;

        bool still =
            UpZ <= BiasLearnFlatUpZ
            && Math.Abs(mag - 1.0) <= BiasLearnGTolerance
            && Math.Abs(rawDps) <= _biasLearnMaxAbsDps
            && accelStill;

        if (!still)
        {
            ResetBiasWindow();
            return;
        }

        _stillCount++;
        _biasSum += rawDps;

        if (rawDps < _biasMin)
            _biasMin = rawDps;

        if (rawDps > _biasMax)
            _biasMax = rawDps;

        if (_stillCount < BiasLearnSamples)
            return;

        if (_biasMax - _biasMin <= BiasLearnMaxRangeDps)
        {
            RestBiasDps = Math.Clamp(
                _biasSum / _stillCount,
                -_biasLearnMaxAbsDps,
                _biasLearnMaxAbsDps);

            HasRestBias = true;
            return;
        }

        ResetBiasWindow();
    }

    /// <summary>
    /// Computes the shortest-arc tilt quaternion that takes the mapped pad-up vector
    /// onto world-up (0, 0, 1), matching DsHidMini's MotionViewerRotation.TiltFromUp.
    /// </summary>
    public static Quaternion CreateTiltQuaternion(double upX, double upY, double upZ)
    {
        double vx = upX;
        double vy = -upY;
        double vz = -upZ;

        double magnitude = Math.Sqrt(vx * vx + vy * vy + vz * vz);
        if (magnitude < 1e-9)
            return Quaternion.Identity;

        vx /= magnitude;
        vy /= magnitude;
        vz /= magnitude;

        double dot = vz;
        if (dot > 0.999999)
            return Quaternion.Identity;

        if (dot < -0.999999)
            return new Quaternion(1, 0, 0, 0);

        return Quaternion.Normalize(
            new Quaternion((float)vy, (float)-vx, 0, (float)(1.0 + dot)));
    }

    /// <summary>
    /// Computes angular velocity in degrees per second from consecutive tilt quaternions.
    /// </summary>
    public static (double OmegaX, double OmegaY) CalculateAngularVelocity(
        Quaternion previousTilt,
        Quaternion currentTilt,
        double dt)
    {
        if (dt <= 0 || dt >= 0.25)
            return (0, 0);

        // Relative rotation in controller-local space.
        Quaternion delta = Quaternion.Normalize(
            Quaternion.Multiply(
                Quaternion.Conjugate(previousTilt),
                currentTilt));

        // Use the shortest quaternion representation.
        if (delta.W < 0)
        {
            delta = new Quaternion(-delta.X, -delta.Y, -delta.Z, -delta.W);
        }

        double sinHalf = Math.Sqrt(
            delta.X * delta.X +
            delta.Y * delta.Y +
            delta.Z * delta.Z);

        if (sinHalf > 1e-7)
        {
            double angle = 2.0 * Math.Atan2(
                sinHalf,
                Math.Clamp(delta.W, -1.0, 1.0));

            double degreesPerSecond =
                angle / sinHalf *
                (180.0 / Math.PI) / dt;

            double omegaX = delta.X * degreesPerSecond;
            double omegaY = delta.Y * degreesPerSecond;

            return (omegaX, omegaY);
        }

        return (0, 0);
    }
}
