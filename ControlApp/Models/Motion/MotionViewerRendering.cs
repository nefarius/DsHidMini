using HelixToolkit.SharpDX;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Creates the DirectX 11 device for the Motion viewer.
///     Software mode uses the WARP rasterizer for that viewport only.
/// </summary>
internal static class MotionViewerRendering
{
    public static EffectsManagerConfiguration CreateConfiguration(bool useSoftwareRendering)
    {
        return new EffectsManagerConfiguration
        {
            EnableSoftwareRendering = useSoftwareRendering
        };
    }

    public static DefaultEffectsManager CreateEffectsManager(bool useSoftwareRendering)
    {
        return new DefaultEffectsManager(CreateConfiguration(useSoftwareRendering));
    }
}
