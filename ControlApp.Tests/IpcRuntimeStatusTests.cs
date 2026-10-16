using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.ControlApp.Models.DshmConfigManager.DshmConfig;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class IpcRuntimeStatusTests
{
    [Theory]
    [InlineData(true, DriverIpcConfigReading.Disabled, true, true)]
    [InlineData(false, DriverIpcConfigReading.Enabled, true, true)]
    [InlineData(true, DriverIpcConfigReading.MissingDefaultsToEnabled, true, true)]
    [InlineData(false, DriverIpcConfigReading.MissingDefaultsToEnabled, true, false)]
    [InlineData(false, DriverIpcConfigReading.Disabled, true, false)]
    [InlineData(false, DriverIpcConfigReading.Unreadable, true, false)]
    [InlineData(true, DriverIpcConfigReading.Enabled, false, false)]
    [InlineData(true, DriverIpcConfigReading.Disabled, false, false)]
    [InlineData(false, DriverIpcConfigReading.Enabled, false, false)]
    public void ExpectsLiveIpc_RequiresAConnectedDevice(
        bool shownEnabled,
        DriverIpcConfigReading driverConfig,
        bool hasConnectedDevice,
        bool expected)
    {
        Assert.Equal(
            expected,
            IpcRuntimeStatus.ExpectsLiveIpc(shownEnabled, driverConfig, hasConnectedDevice));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void IsDisplayedStateStale_OnlyWhenIpcShouldBeLiveAndPingFails(
        bool expectsLiveIpc,
        bool driverResponded,
        bool expected)
    {
        Assert.Equal(expected, IpcRuntimeStatus.IsDisplayedStateStale(expectsLiveIpc, driverResponded));
    }

    [Fact]
    public void DescribeStaleState_TellsTheUserTheCheckboxIsNotTheLiveDriver()
    {
        string shownEnabled = IpcRuntimeStatus.DescribeStaleState(shownEnabled: true);
        string configEnabled = IpcRuntimeStatus.DescribeStaleState(shownEnabled: false);

        Assert.Contains("checkbox", shownEnabled, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Restart Windows", shownEnabled, StringComparison.Ordinal);
        Assert.Contains("Restart Windows", configEnabled, StringComparison.Ordinal);
    }

    [Fact]
    public void TryPingDriver_ReturnsFalseWithoutCallingPingWhenIpcObjectsAreMissing()
    {
        bool pingCalled = false;

        bool responded = IpcRuntimeProbe.TryPingDriver(() => false, () => pingCalled = true);

        Assert.False(responded);
        Assert.False(pingCalled);
    }

    [Fact]
    public void TryPingDriver_ReturnsFalseWhenTheDriverDoesNotAnswer()
    {
        bool responded = IpcRuntimeProbe.TryPingDriver(() => true, () => throw new InvalidOperationException("no reply"));

        Assert.False(responded);
    }

    [Fact]
    public void TryPingDriver_ReturnsTrueWhenTheDriverAnswers()
    {
        Assert.True(IpcRuntimeProbe.TryPingDriver(() => true, () => { }));
    }

    [Fact]
    public void ReadDriverConfig_DistinguishesMissingEnabledDisabledAndUnreadable()
    {
        string root = Path.Combine(Path.GetTempPath(), "dshm-ipc-read-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Assert.Equal(
                DriverIpcConfigReading.MissingDefaultsToEnabled,
                IpcRuntimeStatus.ReadDriverConfig(root));

            string path = DshmConfigSerialization.GetDriverConfigFilePath(root);
            File.WriteAllText(path, """{ "IPCEnabled": true }""");
            Assert.Equal(DriverIpcConfigReading.Enabled, IpcRuntimeStatus.ReadDriverConfig(root));

            File.WriteAllText(path, """{ "IPCEnabled": false }""");
            Assert.Equal(DriverIpcConfigReading.Disabled, IpcRuntimeStatus.ReadDriverConfig(root));

            File.WriteAllText(path, "{");
            Assert.Equal(DriverIpcConfigReading.Unreadable, IpcRuntimeStatus.ReadDriverConfig(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
