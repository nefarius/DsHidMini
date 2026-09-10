using Nefarius.DsHidMini.ControlApp.Models.Motion;
using Nefarius.DsHidMini.ControlApp.ViewModels.Windows;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionViewerLifecycleTests
{
    [Fact]
    public void SessionDispose_CancelsToken()
    {
        MotionViewerViewModel vm = new(1, "test");
        Assert.False(vm.IsSessionCancelled);

        vm.Dispose();

        Assert.True(vm.IsSessionCancelled);
    }

    [Fact]
    public void FallbackStatus_MentionsNominalCalibration()
    {
        string text = MotionStatusFormatter.StatusText(true, true, true, isFallback: true);
        Assert.Contains("nominal", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingTelemetry_MentionsOlderDriver()
    {
        string text = MotionStatusFormatter.StatusText(true, telemetryMapped: false, false, false);
        Assert.Contains("no motion telemetry", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingIpc_IsUnavailable()
    {
        string text = MotionStatusFormatter.StatusText(false, false, false, false);
        Assert.Contains("IPC", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DsIdentificationMotionPath.PlainZero, "Software zero")]
    [InlineData(DsIdentificationMotionPath.HwCal, "Hardware-calibrated")]
    [InlineData(DsIdentificationMotionPath.Sixaxis, "SIXAXIS")]
    [InlineData(DsIdentificationMotionPath.Unknown, "Unknown")]
    public void PathLabel_MatchesControlAppCopy(DsIdentificationMotionPath path, string expected)
    {
        Assert.Contains(expected, MotionStatusFormatter.PathLabel(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SnapshotHelpers_ExposeFallbackAndAvailability()
    {
        DsMotionSnapshot snapshot = new()
        {
            Flags = DsMotionSnapshotFlags.Available | DsMotionSnapshotFlags.Fallback | DsMotionSnapshotFlags.Tracker
        };

        Assert.True(snapshot.IsAvailable);
        Assert.True(snapshot.IsFallback);
        Assert.True(snapshot.HasTracker);
    }
}
