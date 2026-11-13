namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Processed DS3 motion snapshot for Cemuhook UDP broadcast.
///     Acceleration is in g-force; gyro and rates are in degrees per second.
/// </summary>
internal sealed record Ds3MotionSnapshot(
    bool Connected,
    double AccelXG,
    double AccelYG,
    double AccelZG,
    double GyroAxisDegreesPerSecond,
    double PitchDegreesPerSecond,
    double RollDegreesPerSecond);
