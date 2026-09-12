namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     One Helix axis-angle applied to the face-on diagnostic pad
///     (X right, Y up, Z toward the camera).
/// </summary>
internal readonly record struct MotionViewerAxisAngle(
    double AxisX,
    double AxisY,
    double AxisZ,
    double AngleDegrees);

/// <summary>
///     Helix/WPF quaternion (X, Y, Z, W) for the diagnostic pad pose.
/// </summary>
internal readonly record struct MotionViewerQuaternion(
    double X,
    double Y,
    double Z,
    double W)
{
    public static MotionViewerQuaternion Identity { get; } = new(0, 0, 0, 1);
}

/// <summary>
///     Maps the Sony gravity-up vector and integrated yaw onto the face-on
///     Helix pad without an Euler singularity at roll ±90°.
/// </summary>
internal static class MotionViewerRotation
{
    public static MotionViewerAxisAngle Yaw(double yawDegrees)
    {
        // +Z faces the camera; right-hand positive is CCW on screen, so
        // clockwise-from-above (positive estimator yaw) is a negative angle.
        return new MotionViewerAxisAngle(0, 0, 1, -yawDegrees);
    }

    /// <summary>
    ///     Shortest-arc tilt that takes the mapped pad-up vector onto Helix
    ///     world-up <c>(0, 0, 1)</c>. Pad frame to Helix is
    ///     <c>M = diag(1, -1, -1)</c>, matching the hardware-verified Euler
    ///     map (flat = identity, USB-up = <c>R_X(-90)</c>, grip-down =
    ///     <c>R_Y(-90)</c>). Face-down uses axis X, 180°.
    /// </summary>
    public static MotionViewerQuaternion TiltFromUp(double upX, double upY, double upZ)
    {
        double vx = upX;
        double vy = -upY;
        double vz = -upZ;
        double mag = Math.Sqrt((vx * vx) + (vy * vy) + (vz * vz));
        if (mag < 1e-9)
        {
            return MotionViewerQuaternion.Identity;
        }

        vx /= mag;
        vy /= mag;
        vz /= mag;

        // Rotate v onto U0 = (0, 0, 1). axis = v × U0 = (vy, -vx, 0).
        double dot = vz;
        if (dot > 0.999999)
        {
            return MotionViewerQuaternion.Identity;
        }

        if (dot < -0.999999)
        {
            return new MotionViewerQuaternion(1, 0, 0, 0);
        }

        return Normalize(vy, -vx, 0, 1.0 + dot);
    }

    /// <summary>
    ///     Gravity tilt first, then world heading about Z. WPF applies the
    ///     right factor first, so this is <c>q_yaw * q_tilt</c>.
    /// </summary>
    public static MotionViewerQuaternion ComposeYawThenTilt(
        double yawDegrees,
        double upX,
        double upY,
        double upZ)
    {
        MotionViewerQuaternion yaw = FromAxisAngle(0, 0, 1, -yawDegrees);
        MotionViewerQuaternion tilt = TiltFromUp(upX, upY, upZ);
        return Multiply(yaw, tilt);
    }

    internal static MotionViewerQuaternion FromAxisAngle(
        double axisX,
        double axisY,
        double axisZ,
        double angleDegrees)
    {
        double mag = Math.Sqrt((axisX * axisX) + (axisY * axisY) + (axisZ * axisZ));
        if (mag < 1e-9)
        {
            return MotionViewerQuaternion.Identity;
        }

        double half = angleDegrees * Math.PI / 360.0;
        double s = Math.Sin(half) / mag;
        return new MotionViewerQuaternion(axisX * s, axisY * s, axisZ * s, Math.Cos(half));
    }

    internal static MotionViewerQuaternion Multiply(MotionViewerQuaternion a, MotionViewerQuaternion b)
    {
        return new MotionViewerQuaternion(
            (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
            (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
            (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
            (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));
    }

    internal static bool AlmostEqual(
        MotionViewerQuaternion a,
        MotionViewerQuaternion b,
        double epsilon = 1e-6)
    {
        // q and -q are the same rotation.
        double d =
            (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);
        return Math.Abs(Math.Abs(d) - 1.0) <= epsilon;
    }

    private static MotionViewerQuaternion Normalize(double x, double y, double z, double w)
    {
        double mag = Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));
        if (mag < 1e-9)
        {
            return MotionViewerQuaternion.Identity;
        }

        return new MotionViewerQuaternion(x / mag, y / mag, z / mag, w / mag);
    }
}
