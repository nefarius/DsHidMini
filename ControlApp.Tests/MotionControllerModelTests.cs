using System.Numerics;

using HelixToolkit.SharpDX;

using Nefarius.DsHidMini.ControlApp.Models.Motion;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionControllerModelTests
{
    [Fact]
    public void EmbeddedModel_LoadsWithNormalsAndExpectedOrientation()
    {
        // Exercise the embedded resource through the runtime OBJ reader, so a
        // missing publish asset or incompatible export fails without a GPU.
        IReadOnlyList<Object3D> parts = MotionControllerModel.Load();
        Assert.Contains(parts, p => p.Name == "ShellUpper");
        Assert.Contains(parts, p => p.Name == "Triangle");
        Assert.Contains(parts, p => p.Name == "Trigger");
        foreach (Object3D part in parts)
        {
            MeshGeometry3D mesh = Assert.IsType<MeshGeometry3D>(part.Geometry);
            Assert.NotNull(mesh.Indices);
            Assert.NotNull(mesh.Positions);
            Assert.NotNull(mesh.Normals);
            Assert.NotEmpty(mesh.Indices);
            Assert.Equal(0, mesh.Indices.Count % 3);
            Assert.Equal(mesh.Positions.Count, mesh.Normals.Count);
            Assert.All(mesh.Indices, index => Assert.InRange(index, 0, mesh.Positions.Count - 1));
            Assert.All(mesh.Positions, v => Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z)));
            Assert.All(mesh.Normals, n => Assert.InRange(n.Length(), 0.99f, 1.01f));
        }

        Vector3[] shell = parts.Where(p => p.Name == "ShellUpper")
            .SelectMany(p => p.Geometry!.Positions!).ToArray();
        Assert.InRange(shell.Max(p => p.X) - shell.Min(p => p.X), 4f, 4.5f);
        Assert.InRange(shell.Min(p => p.Y), -1.8f, -1.5f); // Grips toward -Y.
        Assert.InRange(shell.Max(p => p.Y), 0.8f, 1.0f); // Nose toward +Y.
        Assert.All(parts.Where(p => p.Name == "StickRubber").SelectMany(p => p.Geometry!.Positions!),
            p => Assert.True(p.Z > 0.5f && p.Y < 0)); // Controls face +Z.
    }
}
