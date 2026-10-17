using Nefarius.DsHidMini.ControlApp.Models.Drivers;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DsHidMiniDeviceFaultPolicyTests
{
    [Fact]
    public void Code10WithDeviceDataError_DescribesTheDeviceManagerFailure()
    {
        DsHidMiniDevNodeSnapshot node = Node(
            "DS3 Compatible HID Device",
            "dshidmini.USB",
            problemCode: 10,
            problemStatus: 0xC000009C);

        IReadOnlyList<DsHidMiniDevNodeSnapshot> faulted = DsHidMiniDeviceFaultPolicy.SelectFaulted([node]);
        DsHidMiniDeviceFaultNotice? notice = DsHidMiniDeviceFaultPolicy.Describe(faulted);

        Assert.Single(faulted);
        Assert.NotNull(notice);
        Assert.Equal(1, notice!.DeviceCount);
        Assert.Equal("DS3 Compatible HID Device could not start", notice.Title);
        Assert.Contains("This device cannot start. (Code 10)", notice.Message);
        Assert.Contains("STATUS_DEVICE_DATA_ERROR (0xC000009C)", notice.Message);
    }

    [Theory]
    [InlineData("dshidmini.USB")]
    [InlineData("dshidmini.USB.Win11")]
    [InlineData("DsHidMini.BTH")]
    public void DsHidMiniSections_MatchRegardlessOfInstallDecoration(string section)
    {
        Assert.True(DsHidMiniDeviceFaultPolicy.IsDsHidMiniDriverSection(section));
    }

    [Fact]
    public void HealthyDsHidMiniDevice_IsIgnored()
    {
        DsHidMiniDevNodeSnapshot node = Node("DS3 Compatible HID Device", "dshidmini.USB", problemCode: 0);

        Assert.Empty(DsHidMiniDeviceFaultPolicy.SelectFaulted([node]));
        Assert.Null(DsHidMiniDeviceFaultPolicy.Describe([]));
    }

    [Fact]
    public void UnrelatedHidDevice_IsIgnoredEvenWithCode10()
    {
        DsHidMiniDevNodeSnapshot node = Node("USB Input Device", "hidusb.Inf", problemCode: 10,
            problemStatus: 0xC000009C);

        Assert.Empty(DsHidMiniDeviceFaultPolicy.SelectFaulted([node]));
    }

    [Fact]
    public void MissingProblemStatus_StillDescribesTheDevnode()
    {
        DsHidMiniDevNodeSnapshot node = Node("DS3 Compatible HID Device", "dshidmini.BTH.Win11", problemCode: 10);

        DsHidMiniDeviceFaultNotice? notice =
            DsHidMiniDeviceFaultPolicy.Describe(DsHidMiniDeviceFaultPolicy.SelectFaulted([node]));

        Assert.NotNull(notice);
        Assert.Contains("This device cannot start. (Code 10)", notice!.Message);
        Assert.Contains("Windows did not report a failure status.", notice.Message);
        Assert.DoesNotContain("STATUS_DEVICE_DATA_ERROR", notice.Message);
    }

    [Fact]
    public void MissingDisplayName_UsesTheInstanceId()
    {
        DsHidMiniDevNodeSnapshot node = Node(null, "dshidmini.USB", problemCode: 43, problemStatus: 0xC0000001,
            instanceId: @"USB\VID_054C&PID_0268\5");

        DsHidMiniDeviceFaultNotice? notice =
            DsHidMiniDeviceFaultPolicy.Describe(DsHidMiniDeviceFaultPolicy.SelectFaulted([node]));

        Assert.NotNull(notice);
        Assert.StartsWith(@"USB\VID_054C&PID_0268\5", notice!.Title);
        Assert.Contains("Windows has stopped this device because it has reported problems. (Code 43)", notice.Message);
        Assert.Contains("STATUS_UNSUCCESSFUL (0xC0000001)", notice.Message);
    }

    [Fact]
    public void Code22_IsSelectedAndDescribedAsDisabled()
    {
        DsHidMiniDevNodeSnapshot node = Node("DS3 Compatible HID Device", "dshidmini.USB", problemCode: 22);

        IReadOnlyList<DsHidMiniDevNodeSnapshot> faulted = DsHidMiniDeviceFaultPolicy.SelectFaulted([node]);
        DsHidMiniDeviceFaultNotice? notice = DsHidMiniDeviceFaultPolicy.Describe(faulted);

        Assert.Single(faulted);
        Assert.NotNull(notice);
        Assert.Equal("DS3 Compatible HID Device is disabled", notice!.Title);
        Assert.Contains("This device is disabled. (Code 22)", notice.Message);
        Assert.DoesNotContain("could not start", notice.Title);
        Assert.DoesNotContain("could not start", notice.Message);
        Assert.DoesNotContain("stays out of the device list", notice.Message);
    }

    [Fact]
    public void MultipleDisabledDevices_AreDescribedAsDisabled()
    {
        DsHidMiniDevNodeSnapshot[] nodes =
        [
            Node("Wireless DS3", "dshidmini.BTH", problemCode: 22, instanceId: "BTHPS3BUS\\SECOND"),
            Node("Wired DS3", "dshidmini.USB", problemCode: 22, instanceId: "USB\\FIRST")
        ];

        DsHidMiniDeviceFaultNotice? notice =
            DsHidMiniDeviceFaultPolicy.Describe(DsHidMiniDeviceFaultPolicy.SelectFaulted(nodes));

        Assert.NotNull(notice);
        Assert.Equal("2 controllers are disabled", notice!.Title);
        Assert.Contains("This device is disabled. (Code 22)", notice.Message);
        Assert.Contains("1 other controller is also disabled.", notice.Message);
        Assert.DoesNotContain("could not start", notice.Title);
        Assert.DoesNotContain("failed to start", notice.Message);
    }

    [Fact]
    public void MixedDisabledAndStartFailure_DescribesEachCategory()
    {
        DsHidMiniDevNodeSnapshot[] nodes =
        [
            Node("Wireless DS3", "dshidmini.BTH", problemCode: 22, instanceId: "BTHPS3BUS\\SECOND"),
            Node("Wired DS3", "dshidmini.USB", problemCode: 10, problemStatus: 0xC000009C,
                instanceId: "USB\\FIRST")
        ];

        DsHidMiniDeviceFaultNotice? notice =
            DsHidMiniDeviceFaultPolicy.Describe(DsHidMiniDeviceFaultPolicy.SelectFaulted(nodes));

        Assert.NotNull(notice);
        Assert.Equal("2 controllers have a problem", notice!.Title);
        Assert.Contains("This device cannot start. (Code 10)", notice.Message);
        Assert.Contains("1 controller could not start.", notice.Message);
        Assert.Contains("1 controller is disabled.", notice.Message);
        Assert.DoesNotContain("2 controllers could not start", notice.Title);
        Assert.DoesNotContain("also failed to start", notice.Message);
    }

    [Fact]
    public void MultipleFailedDevices_ReportsTheCount()
    {
        DsHidMiniDevNodeSnapshot[] nodes =
        [
            Node("Wireless DS3", "dshidmini.BTH", problemCode: 10, problemStatus: 0xC000009C,
                instanceId: "BTHPS3BUS\\SECOND"),
            Node("Wired DS3", "dshidmini.USB.Win11", problemCode: 10, problemStatus: null,
                instanceId: "USB\\FIRST")
        ];

        IReadOnlyList<DsHidMiniDevNodeSnapshot> faulted = DsHidMiniDeviceFaultPolicy.SelectFaulted(nodes);
        DsHidMiniDeviceFaultNotice? notice = DsHidMiniDeviceFaultPolicy.Describe(faulted);

        Assert.Equal(2, faulted.Count);
        Assert.Equal("Wired DS3", faulted[0].DisplayName);
        Assert.NotNull(notice);
        Assert.Equal(2, notice!.DeviceCount);
        Assert.Equal("2 controllers could not start", notice.Title);
        Assert.Contains("Wired DS3:", notice.Message);
        Assert.Contains("Windows did not report a failure status.", notice.Message);
        Assert.Contains("1 other controller also failed to start.", notice.Message);
    }

    [Fact]
    public void UnknownStatus_FallsBackToHex()
    {
        Assert.Equal("Windows reported NTSTATUS 0xC000009A.", DsHidMiniDeviceFaultPolicy.DescribeStatus(0xC000009A));
    }

    private static DsHidMiniDevNodeSnapshot Node(
        string? name,
        string? section,
        uint? problemCode,
        uint? problemStatus = null,
        string instanceId = @"USB\VID_054C&PID_0268\1")
    {
        return new DsHidMiniDevNodeSnapshot(instanceId, name, section, problemCode, problemStatus);
    }
}
