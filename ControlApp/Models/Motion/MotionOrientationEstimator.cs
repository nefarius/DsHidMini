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

    private readonly double _smoothing;
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

        double rawDps = snapshot.GyroMilliDps / 1000.0;

        if (_hasTiming && snapshot.TimestampQpc > _lastQpc)
        {
            double dt = (snapshot.TimestampQpc - _lastQpc) / QpcFrequency;
            if (dt > 0 && dt < 0.25)
            {
                if (Math.Abs(rawDps) >= _yawRestDeadzoneDps)
                {
                    YawDegrees += (rawDps - RestBiasDps) * (-UpZ) * dt;
                }
            }
        }

        UpdateRestBias(in snapshot, rawDps, mag);

        _hasTiming = true;
        _lastQpc = snapshot.TimestampQpc;
        _lastSampleIndex = snapshot.SampleIndex;
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

    private void UpdateRestBias(in DsMotionSnapshot snapshot, double rawDps, double mag)
    {
        if (HasRestBias)
        {
            return;
        }

        bool accelStill = !_hasPrevAccel
                          || (Math.Abs(snapshot.AccelMilliGX - _prevMilliGX) <= BiasLearnAccelJitterMilliG
                              && Math.Abs(snapshot.AccelMilliGY - _prevMilliGY) <= BiasLearnAccelJitterMilliG
                              && Math.Abs(snapshot.AccelMilliGZ - _prevMilliGZ) <= BiasLearnAccelJitterMilliG);
        _hasPrevAccel = true;
        _prevMilliGX = snapshot.AccelMilliGX;
        _prevMilliGY = snapshot.AccelMilliGY;
        _prevMilliGZ = snapshot.AccelMilliGZ;

        bool still = UpZ <= BiasLearnFlatUpZ
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
        {
            _biasMin = rawDps;
        }

        if (rawDps > _biasMax)
        {
            _biasMax = rawDps;
        }

        if (_stillCount < BiasLearnSamples)
        {
            return;
        }

        if (_biasMax - _biasMin <= BiasLearnMaxRangeDps)
        {
            RestBiasDps = Math.Clamp(_biasSum / _stillCount, -_biasLearnMaxAbsDps, _biasLearnMaxAbsDps);
            HasRestBias = true;
            return;
        }

        ResetBiasWindow();
    }
}
