using Nefarius.DsHidMini.ControlApp.Models.Motion;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionViewerRotationTests
{
    [Fact]
    public void FromEuler_MapsFaceOnAxesAndYawSign()
    {
        MotionViewerRotation.FromEuler(
            -90,
            -80,
            15,
            out MotionViewerAxisAngle pitch,
            out MotionViewerAxisAngle roll,
            out MotionViewerAxisAngle yaw);

        Assert.Equal(1, pitch.AxisX);
        Assert.Equal(0, pitch.AxisY);
        Assert.Equal(0, pitch.AxisZ);
        Assert.Equal(90, pitch.AngleDegrees);

        Assert.Equal(0, roll.AxisX);
        Assert.Equal(1, roll.AxisY);
        Assert.Equal(0, roll.AxisZ);
        Assert.Equal(-80, roll.AngleDegrees);

        Assert.Equal(0, yaw.AxisX);
        Assert.Equal(0, yaw.AxisY);
        Assert.Equal(1, yaw.AxisZ);
        Assert.Equal(-15, yaw.AngleDegrees);
    }

    [Fact]
    public void Pitch_UsesXAxisNegated()
    {
        MotionViewerAxisAngle mapped = MotionViewerRotation.Pitch(-90);

        Assert.Equal((1, 0, 0, 90), (mapped.AxisX, mapped.AxisY, mapped.AxisZ, mapped.AngleDegrees));
    }

    [Fact]
    public void Roll_UsesYAxisUnflipped()
    {
        MotionViewerAxisAngle mapped = MotionViewerRotation.Roll(-80);

        Assert.Equal((0, 1, 0, -80), (mapped.AxisX, mapped.AxisY, mapped.AxisZ, mapped.AngleDegrees));
    }

    [Fact]
    public void Yaw_UsesZAxisNegated()
    {
        MotionViewerAxisAngle mapped = MotionViewerRotation.Yaw(15);

        Assert.Equal((0, 0, 1, -15), (mapped.AxisX, mapped.AxisY, mapped.AxisZ, mapped.AngleDegrees));
    }
}
