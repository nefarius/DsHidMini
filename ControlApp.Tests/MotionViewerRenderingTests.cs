using System.Windows.Interop;

using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionViewerRenderingTests
{
    [Fact]
    public void Select_HardwareStaysOnTheGraphicsDriver()
    {
        Assert.Equal(RenderMode.Default, MotionViewerRendering.Select(false));
    }

    [Fact]
    public void Select_SoftwareUsesTheCpuRasterizer()
    {
        Assert.Equal(RenderMode.SoftwareOnly, MotionViewerRendering.Select(true));
    }

    [Fact]
    public void Apply_NullWindowIsIgnored()
    {
        MotionViewerRendering.Apply(null, true);
    }

    [Fact]
    public void ViewModel_SoftwarePreferenceRaisesOneChange()
    {
        int changes = 0;
        MotionViewerViewModel viewModel = new(1, "test", useSoftwareRendering: true);
        viewModel.SoftwareRenderingChanged += (_, _) => changes++;

        viewModel.UseSoftwareRendering = false;
        viewModel.UseSoftwareRendering = false;

        Assert.False(viewModel.UseSoftwareRendering);
        Assert.Equal(1, changes);
        viewModel.Dispose();
    }

    [Fact]
    public void ApplicationConfiguration_JsonRoundTrip_PreservesMotionViewerSoftwareRendering()
    {
        ApplicationConfiguration original = new()
        {
            MotionViewerSoftwareRendering = true
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
        ApplicationConfiguration? loaded =
            Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationConfiguration>(json);

        Assert.NotNull(loaded);
        Assert.True(loaded.MotionViewerSoftwareRendering);
        Assert.False(new ApplicationConfiguration().MotionViewerSoftwareRendering);
    }
}
