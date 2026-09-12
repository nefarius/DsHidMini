using Nefarius.DsHidMini.ControlApp.Models.Motion;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionViewerRotationTests
{
    [Fact]
    public void Yaw_UsesZAxisNegated()
    {
        MotionViewerAxisAngle mapped = MotionViewerRotation.Yaw(15);

        Assert.Equal((0, 0, 1, -15), (mapped.AxisX, mapped.AxisY, mapped.AxisZ, mapped.AngleDegrees));
    }

    [Fact]
    public void TiltFromUp_Flat_IsIdentity()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(0, 0, -1);

        Assert.True(MotionViewerRotation.AlmostEqual(q, MotionViewerQuaternion.Identity));
    }

    [Fact]
    public void TiltFromUp_UsbUp_EqualsRxNeg90()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(0, 1, 0);
        MotionViewerQuaternion expected = MotionViewerRotation.FromAxisAngle(1, 0, 0, -90);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
    }

    [Fact]
    public void TiltFromUp_GripDown_EqualsRyNeg90()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(1, 0, 0);
        MotionViewerQuaternion expected = MotionViewerRotation.FromAxisAngle(0, 1, 0, -90);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
    }

    [Fact]
    public void TiltFromUp_OnGripNoise_StaysNearRyNeg90()
    {
        MotionViewerQuaternion nominal = MotionViewerRotation.TiltFromUp(1, 0, 0);
        MotionViewerQuaternion noisy = MotionViewerRotation.TiltFromUp(0.997, 0.05, -0.06);

        Assert.True(MotionViewerRotation.AlmostEqual(noisy, nominal, 0.02));
        Assert.True(double.IsFinite(noisy.X));
        Assert.True(double.IsFinite(noisy.Y));
        Assert.True(double.IsFinite(noisy.Z));
        Assert.True(double.IsFinite(noisy.W));
    }

    [Fact]
    public void TiltFromUp_FaceDown_IsFinite180AboutX()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(0, 0, 1);
        MotionViewerQuaternion expected = new(1, 0, 0, 0);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
        Assert.True(double.IsFinite(q.X + q.Y + q.Z + q.W));
    }

    [Fact]
    public void ComposeYawThenTilt_FlatYawMatchesZ()
    {
        MotionViewerQuaternion q = MotionViewerRotation.ComposeYawThenTilt(15, 0, 0, -1);
        MotionViewerQuaternion expected = MotionViewerRotation.FromAxisAngle(0, 0, 1, -15);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
    }
}
