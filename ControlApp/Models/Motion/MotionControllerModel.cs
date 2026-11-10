using System.IO;

using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Original, texture-free controller mesh. X points right, +Y toward the
///     USB port and +Z out of the face, matching the viewer's existing pose map.
/// </summary>
internal static class MotionControllerModel
{
    internal static IReadOnlyList<Object3D> Load()
    {
        using Stream stream = typeof(MotionControllerModel).Assembly
            .GetManifestResourceStream("MotionController.obj")
            ?? throw new InvalidOperationException("The motion controller model is missing.");
        // This bundled, geometry-only OBJ needs no native Assimp dependency.
#pragma warning disable CS0618 // Use the existing managed reader for this fixed asset.
        ObjReader reader = new() { IgnoreErrors = false };
#pragma warning restore CS0618
        reader.Read(stream);
        // Helix 3.1.2 does not copy OBJ group names into Object3D.Name.
        // Retain them explicitly because they identify the part materials.
        return reader.Groups.SelectMany(group => group.CreateModels(reader.ModelInfo)
            .Select(model =>
            {
                model.Name = group.Name;
                return model;
            })).ToArray();
    }

    internal static PhongMaterial MaterialFor(string part)
    {
        // Linear colours: keep the shell readable under the diagnostic scene's
        // lighting, with enough contrast to distinguish the rubber and controls.
        (float r, float g, float b, float shine) = part switch
        {
            "ShellUpper" => (0.16f, 0.19f, 0.24f, 64f),
            "ShellLower" => (0.075f, 0.09f, 0.12f, 40f),
            "Seam" or "ControlWell" or "UsbRim" => (0.018f, 0.023f, 0.032f, 12f),
            "StickRubber" or "StickRim" => (0.045f, 0.05f, 0.06f, 8f),
            "StickSocket" or "HomeBezel" => (0.10f, 0.12f, 0.15f, 80f),
            "Triangle" => (0.10f, 0.85f, 0.55f, 24f),
            "Circle" => (0.95f, 0.20f, 0.28f, 24f),
            "Cross" => (0.25f, 0.55f, 1.0f, 24f),
            "Square" => (0.90f, 0.35f, 0.65f, 24f),
            "Home" => (0.35f, 0.42f, 0.52f, 80f),
            "LedOn" => (0.95f, 0.12f, 0.08f, 24f),
            "LedOff" => (0.13f, 0.035f, 0.03f, 24f),
            _ => (0.035f, 0.043f, 0.058f, 40f)
        };
        return new PhongMaterial
        {
            DiffuseColor = new Color4(r, g, b, 1),
            AmbientColor = new Color4(r * 0.5f, g * 0.5f, b * 0.5f, 1),
            SpecularColor = new Color4(0.22f, 0.22f, 0.22f, 1),
            SpecularShininess = shine
        };
    }
}
