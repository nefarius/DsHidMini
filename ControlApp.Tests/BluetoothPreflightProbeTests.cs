using Nefarius.DsHidMini.ControlApp.Models.Diagnostics;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class BluetoothPreflightProbeTests
{
    [Fact]
    public void CreateElevationCheck_WhenElevated_Passes()
    {
        PreflightCheckResult result = BluetoothPreflightProbe.CreateElevationCheck(true);

        Assert.Equal(PreflightCheckId.RunningAsAdministrator, result.Id);
        Assert.True(result.Passed);
        Assert.False(result.CanAutoRepair);
    }

    [Fact]
    public void CreateElevationCheck_WhenNotElevated_AsksToRestartAsAdministrator()
    {
        PreflightCheckResult result = BluetoothPreflightProbe.CreateElevationCheck(false);

        Assert.Equal(PreflightCheckId.RunningAsAdministrator, result.Id);
        Assert.False(result.Passed);
        Assert.Contains("Administrator", result.Detail, StringComparison.Ordinal);
        Assert.False(result.CanAutoRepair);
    }
}
