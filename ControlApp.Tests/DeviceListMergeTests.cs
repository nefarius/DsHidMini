using Nefarius.DsHidMini.ControlApp.Models;
using Nefarius.DsHidMini.IPC;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class DeviceListMergeTests
{
    [Fact]
    public void FirstDevice_IsAdded()
    {
        DeviceListMerge.Result result = DeviceListMerge.Compute([], ["USB\\VID_054C&PID_0268\\1"]);

        Assert.Empty(result.InstanceIdsToRemove);
        Assert.Empty(result.InstanceIdsToKeep);
        Assert.Equal(["USB\\VID_054C&PID_0268\\1"], result.InstanceIdsToAdd);
    }

    [Fact]
    public void SecondDeviceArrival_KeepsFirstAndAddsSecond()
    {
        DeviceListMerge.Result result = DeviceListMerge.Compute(
            ["USB\\VID_054C&PID_0268\\1"],
            ["USB\\VID_054C&PID_0268\\1", "BTHPS3BUS\\DEV\\2"]);

        Assert.Empty(result.InstanceIdsToRemove);
        Assert.Equal(["USB\\VID_054C&PID_0268\\1"], result.InstanceIdsToKeep);
        Assert.Equal(["BTHPS3BUS\\DEV\\2"], result.InstanceIdsToAdd);
    }

    [Fact]
    public void SecondDeviceRemoval_KeepsFirst()
    {
        DeviceListMerge.Result result = DeviceListMerge.Compute(
            ["USB\\VID_054C&PID_0268\\1", "BTHPS3BUS\\DEV\\2"],
            ["USB\\VID_054C&PID_0268\\1"]);

        Assert.Equal(["BTHPS3BUS\\DEV\\2"], result.InstanceIdsToRemove);
        Assert.Equal(["USB\\VID_054C&PID_0268\\1"], result.InstanceIdsToKeep);
        Assert.Empty(result.InstanceIdsToAdd);
    }

    [Fact]
    public void RepeatedSecondDeviceArrivalAndRemoval_NeverRecreatesFirst()
    {
        string first = "USB\\VID_054C&PID_0268\\1";
        string second = "BTHPS3BUS\\DEV\\2";
        string[] current = [first];

        for (int i = 0; i < 8; i++)
        {
            DeviceListMerge.Result arrived = DeviceListMerge.Compute(current, [first, second]);
            Assert.Empty(arrived.InstanceIdsToRemove);
            Assert.Equal([first], arrived.InstanceIdsToKeep);
            Assert.Equal([second], arrived.InstanceIdsToAdd);
            current = [first, second];

            DeviceListMerge.Result removed = DeviceListMerge.Compute(current, [first]);
            Assert.Equal([second], removed.InstanceIdsToRemove);
            Assert.Equal([first], removed.InstanceIdsToKeep);
            Assert.Empty(removed.InstanceIdsToAdd);
            current = [first];
        }
    }

    [Fact]
    public void IdenticalRefresh_IsANoOp()
    {
        string[] ids = ["USB\\1", "BTH\\2"];
        DeviceListMerge.Result result = DeviceListMerge.Compute(ids, ids);

        Assert.Empty(result.InstanceIdsToRemove);
        Assert.Empty(result.InstanceIdsToAdd);
        Assert.Equal(ids, result.InstanceIdsToKeep);
    }

    [Fact]
    public void CompareIsCaseInsensitive()
    {
        DeviceListMerge.Result result = DeviceListMerge.Compute(
            ["usb\\vid_054c\\abc"],
            ["USB\\VID_054C\\ABC"]);

        Assert.Empty(result.InstanceIdsToRemove);
        Assert.Empty(result.InstanceIdsToAdd);
        Assert.Single(result.InstanceIdsToKeep);
    }

    [Theory]
    [InlineData(1, 28, 65536u, 0UL)]
    [InlineData(2, 28, 65536u, 28UL)]
    public void MetricsSlotOffset_StaysInsideMappedRegion(
        int deviceIndex, int slotSize, uint granularity, ulong expectedOffset)
    {
        Assert.True(DsHidMiniInterop.TryGetMappedSlotOffset(
            deviceIndex, slotSize, granularity, out nuint byteOffset));
        Assert.Equal(expectedOffset, byteOffset);
        Assert.Equal(DsInputReportMetrics.Size, slotSize);
    }

    [Fact]
    public void MetricsSlotOffset_RejectsSlotPastMappedRegion()
    {
        Assert.False(DsHidMiniInterop.TryGetMappedSlotOffset(
            deviceIndex: 8, slotSize: 28, allocationGranularity: 100, out _));
        Assert.False(DsHidMiniInterop.TryGetMappedSlotOffset(
            deviceIndex: 1, slotSize: 28, allocationGranularity: 0, out _));
    }
}
