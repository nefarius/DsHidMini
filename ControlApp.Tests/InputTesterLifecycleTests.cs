using Nefarius.DsHidMini.ControlApp.Models.Input;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class InputTesterLifecycleTests
{
    [Fact]
    public void SessionDispose_CancelsToken()
    {
        InputTesterViewModel vm = new(1, "test");
        Assert.False(vm.IsSessionCancelled);

        vm.Dispose();

        Assert.True(vm.IsSessionCancelled);
    }

    [Fact]
    public void NewSession_StartsIdleAndWaiting()
    {
        InputTesterViewModel vm = new(1, "AA:BB:CC:DD:EE:FF");

        Assert.Equal("Input tester — AA:BB:CC:DD:EE:FF", vm.Title);
        Assert.Equal(InputTesterStatusFormatter.StatusText(true, false), vm.StatusText);
        Assert.Equal(ControllerStick.Center, vm.State.LeftStick.RawX);
        Assert.Equal(ControllerStick.Center, vm.State.RightStick.RawY);
        Assert.False(vm.State.Ps);
    }
}
