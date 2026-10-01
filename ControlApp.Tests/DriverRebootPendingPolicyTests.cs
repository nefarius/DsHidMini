using Nefarius.DsHidMini.ControlApp.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DriverRebootPendingPolicyTests
{
    private static readonly DateTime Boot = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DriverRebootMarker NoMarker = new(false, null, null);
    private static readonly Version Package = new(3, 6, 0, 100);

    private static DriverDeviceSnapshot Device(
        bool reboot = false, uint? problem = null, uint? status = null, Version? bound = null)
    {
        return new DriverDeviceSnapshot("USB\\VID_054C", reboot, problem, status, bound);
    }

    [Fact]
    public void MarkerWrittenAfterBoot_IsPending()
    {
        DriverRebootPendingState state = DriverRebootPendingPolicy.Evaluate(
            new DriverRebootMarker(true, Boot.AddHours(1), "x"), Boot, Package, []);
        Assert.True(state.IsPending);
        Assert.False(state.MarkerIsStale);
    }

    [Fact]
    public void MarkerWrittenBeforeBoot_IsStaleNotPending()
    {
        DriverRebootPendingState state = DriverRebootPendingPolicy.Evaluate(
            new DriverRebootMarker(true, Boot.AddHours(-1), "x"), Boot, Package, []);
        Assert.False(state.IsPending);
        Assert.True(state.MarkerIsStale);
    }

    [Fact]
    public void MarkerWrittenJustBeforeBoot_IsStale()
    {
        DriverRebootPendingState state = DriverRebootPendingPolicy.Evaluate(
            new DriverRebootMarker(true, Boot.AddSeconds(-30), "x"), Boot, Package, []);
        Assert.False(state.IsPending);
        Assert.True(state.MarkerIsStale);
    }

    [Fact]
    public void DeviceFlags_ArePendingWithoutMarker()
    {
        Assert.True(DriverRebootPendingPolicy.Evaluate(NoMarker, Boot, Package, [Device(reboot: true)]).IsPending);
        Assert.True(DriverRebootPendingPolicy.Evaluate(NoMarker, Boot, Package, [Device(problem: 14)]).IsPending);
        Assert.True(DriverRebootPendingPolicy.Evaluate(NoMarker, Boot, Package, [Device(status: 0x100)]).IsPending);
    }

    [Fact]
    public void OlderBoundVersion_IsPending()
    {
        DriverRebootPendingState state = DriverRebootPendingPolicy.Evaluate(
            NoMarker, Boot, Package, [Device(bound: new Version(3, 5, 0, 1))]);
        Assert.True(state.IsPending);
        Assert.Equal(1, state.AffectedDeviceCount);
        Assert.Contains("3.5.0.1", state.Reasons[0]);
    }

    [Fact]
    public void Clean_IsNotPending()
    {
        Assert.False(DriverRebootPendingPolicy.Evaluate(
            NoMarker, Boot, Package, [Device(problem: 0, status: 0, bound: Package)]).IsPending);
    }

    [Fact]
    public void UnreadableVersions_NeverPending()
    {
        Assert.False(DriverRebootPendingPolicy.Evaluate(NoMarker, Boot, Package, [Device(bound: null)]).IsPending);
        Assert.False(DriverRebootPendingPolicy.Evaluate(
            NoMarker, Boot, null, [Device(bound: new Version(1, 0))]).IsPending);
    }
}
