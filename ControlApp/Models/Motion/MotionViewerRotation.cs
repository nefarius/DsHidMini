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
///     Maps estimator Euler angles onto the face-on Helix pad: pitch (USB/trigger)
///     nods about X (negated), roll (grips) banks about Y, yaw spins about the
///     face normal (negated).
/// </summary>
internal static class MotionViewerRotation
{
    public static MotionViewerAxisAngle Pitch(double pitchDegrees)
    {
        // +X is right; right-hand positive nods the USB/trigger edge away
        // from the camera, so estimator nose-up is a negative Helix angle.
        return new MotionViewerAxisAngle(1, 0, 0, -pitchDegrees);
    }

    public static MotionViewerAxisAngle Roll(double rollDegrees)
    {
        return new MotionViewerAxisAngle(0, 1, 0, rollDegrees);
    }

    public static MotionViewerAxisAngle Yaw(double yawDegrees)
    {
        // +Z faces the camera; right-hand positive is CCW on screen, so
        // clockwise-from-above (positive estimator yaw) is a negative angle.
        return new MotionViewerAxisAngle(0, 0, 1, -yawDegrees);
    }

    public static void FromEuler(
        double pitchDegrees,
        double rollDegrees,
        double yawDegrees,
        out MotionViewerAxisAngle pitch,
        out MotionViewerAxisAngle roll,
        out MotionViewerAxisAngle yaw)
    {
        pitch = Pitch(pitchDegrees);
        roll = Roll(rollDegrees);
        yaw = Yaw(yawDegrees);
    }
}
