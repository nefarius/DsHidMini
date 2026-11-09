using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.IPC.Models.Public;

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
    public void TiltFromUp_UsbUp_EqualsRxPos90()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(0, 1, 0);
        MotionViewerQuaternion expected = MotionViewerRotation.FromAxisAngle(1, 0, 0, 90);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
    }

    [Fact]
    public void TiltFromUp_RightGripDown_EqualsRyPos90()
    {
        MotionViewerQuaternion q = MotionViewerRotation.TiltFromUp(1, 0, 0);
        MotionViewerQuaternion expected = MotionViewerRotation.FromAxisAngle(0, 1, 0, 90);

        Assert.True(MotionViewerRotation.AlmostEqual(q, expected));
    }

    [Fact]
    public void TiltFromUp_OnGripNoise_StaysNearRyPos90()
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

    [Fact]
    public void ComposeYawThenTilt_GripDownLowersRightGrip()
    {
        MotionViewerQuaternion q = MotionViewerRotation.ComposeYawThenTilt(25, 1, 0, 0);
        (double x, double y, double z) = Rotate(q, 1, 0, 0);

        Assert.InRange(x, -0.02, 0.02);
        Assert.InRange(y, -0.02, 0.02);
        Assert.InRange(z, -1.02, -0.98);
    }

    [Theory]
    [InlineData(0, 500, -866, 0, 1, 0.5)] // USB edge up.
    [InlineData(0, -500, -866, 0, 1, -0.5)] // USB edge down.
    [InlineData(500, 0, -866, 1, 0, -0.5)] // Right grip down.
    [InlineData(-500, 0, -866, 1, 0, 0.5)] // Right grip up.
    [InlineData(0, 1000, 0, 0, 1, 1.0)]
    [InlineData(0, -1000, 0, 0, 1, -1.0)]
    [InlineData(1000, 0, 0, 1, 0, -1.0)]
    [InlineData(-1000, 0, 0, 1, 0, 1.0)]
    public void SensorTilt_MovesModelLandmarksInPhysicalDirection(
        int ax, int ay, int az, double modelX, double modelY, double expectedHeight)
    {
        MotionOrientationEstimator estimator = new(1_000_000, smoothing: 1.0);
        estimator.Update(new DsMotionSnapshot
        {
            Flags = DsMotionSnapshotFlags.Available,
            AccelMilliGX = ax,
            AccelMilliGY = ay,
            AccelMilliGZ = az,
            SampleIndex = 1,
            TimestampQpc = 1_000_000
        });

        // Check physical model points, rather than just quaternion signs.
        // Turning the heading must not reverse which edge is raised.
        foreach (double yaw in new[] { 0.0, 45.0, 90.0 })
        {
            MotionViewerQuaternion q = MotionViewerRotation.ComposeYawThenTilt(
                yaw, estimator.UpX, estimator.UpY, estimator.UpZ);
            (_, _, double height) = Rotate(q, modelX, modelY, 0);
            Assert.InRange(height, expectedHeight - 0.001, expectedHeight + 0.001);
        }
    }

    private static (double X, double Y, double Z) Rotate(MotionViewerQuaternion q, double x, double y, double z)
    {
        MotionViewerQuaternion point = new(x, y, z, 0);
        MotionViewerQuaternion inverse = new(-q.X, -q.Y, -q.Z, q.W);
        MotionViewerQuaternion rotated = MotionViewerRotation.Multiply(
            MotionViewerRotation.Multiply(q, point),
            inverse);
        return (rotated.X, rotated.Y, rotated.Z);
    }
}
