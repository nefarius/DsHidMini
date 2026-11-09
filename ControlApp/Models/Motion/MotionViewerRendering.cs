using System.Windows.Interop;

namespace Nefarius.DsHidMini.ControlApp.Models.Motion;

/// <summary>
///     Selects the rasterizer for the Motion viewer window only.
///     Software mode avoids the WPF Direct3D 9 device without changing the rest of ControlApp.
/// </summary>
internal static class MotionViewerRendering
{
    public static RenderMode Select(bool useSoftwareRendering)
    {
        return useSoftwareRendering ? RenderMode.SoftwareOnly : RenderMode.Default;
    }

    public static void Apply(HwndSource? source, bool useSoftwareRendering)
    {
        if (source?.CompositionTarget is null)
        {
            return;
        }

        source.CompositionTarget.RenderMode = Select(useSoftwareRendering);
    }
}
