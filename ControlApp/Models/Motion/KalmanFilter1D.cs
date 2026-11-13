namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Adaptive 1D Kalman filter that reduces sensor jitter while tracking
/// deliberate motion with minimal latency.
/// </summary>
internal sealed class KalmanFilter1D
{
    private double _x;
    private double _p;
    private readonly double _qBase;
    private readonly double _rBase;
    private bool _initialized;

    public KalmanFilter1D(double q = 0.01, double r = 0.1)
    {
        _qBase = q;
        _rBase = r;
        _p = q;
        _initialized = false;
    }

    public double Value => _x;

    public bool IsInitialized => _initialized;

    internal void Reset()
    {
        _initialized = false;
        _x = 0;
        _p = _qBase;
    }

    internal void Reset(double initialValue)
    {
        _x = initialValue;
        _p = _qBase;
        _initialized = true;
    }

    /// <summary>
    ///     Updates the filter with a new measurement and returns the filtered estimate.
    ///     <paramref name="filterStrength"/> ranges from 0.001 (raw pass-through) to 1.0 (maximum filtering).
    /// </summary>
    internal double Update(double measurement, double filterStrength = 0.2)
    {
        if (!_initialized)
        {
            _x = measurement;
            _p = _qBase;
            _initialized = true;
            return _x;
        }

        filterStrength = Math.Clamp(filterStrength, 0.001, 1.0);

        if (filterStrength <= 0.001)
        {
            _x = measurement;
            return _x;
        }

        double residual = Math.Abs(measurement - _x);

        // Scale R with filter strength; scale Q adaptively so deliberate motion is tracked without latency.
        double r = _rBase * (filterStrength * 10.0);
        double q = _qBase * (1.0 + residual * residual * 20.0) / (filterStrength * 2.0 + 0.1);

        _p += q;
        double k = _p / (_p + r);
        _x += k * (measurement - _x);
        _p *= (1.0 - k);

        return _x;
    }
}
