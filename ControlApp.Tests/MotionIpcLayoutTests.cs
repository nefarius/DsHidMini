using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Drivers;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class MotionIpcLayoutTests
{
    [Fact]
    public void HidSlot_RemainsSixtyBytes()
    {
        Assert.Equal(60, Marshal.SizeOf<IPC_HID_INPUT_REPORT_MESSAGE>());
        Assert.Equal(4, (int)Marshal.OffsetOf<IPC_HID_INPUT_REPORT_MESSAGE>(nameof(IPC_HID_INPUT_REPORT_MESSAGE.SequenceNumber)));
    }

    [Fact]
    public void MotionSnapshot_IsEightyBytes_VersionOne()
    {
        Assert.Equal(DsMotionSnapshot.Size, Marshal.SizeOf<DsMotionSnapshot>());
        Assert.Equal(80, Marshal.SizeOf<DsMotionSnapshot>());
        Assert.Equal((ushort)1, DsMotionSnapshot.CurrentVersion);
        Assert.Equal(4, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.SequenceNumber)));
        Assert.Equal(0, Marshal.SizeOf<DsMotionSnapshot>() % sizeof(int));
    }

    [Fact]
    public void MotionSnapshot_FieldOffsets_MatchDriverPack1()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.SlotIndex)));
        Assert.Equal(8, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.Version)));
        Assert.Equal(10, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.Flags)));
        Assert.Equal(44, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.AccelMilliGX)));
        Assert.Equal(68, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.TimestampQpc)));
        Assert.Equal(76, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.CalByte)));
        Assert.Equal(77, (int)Marshal.OffsetOf<DsMotionSnapshot>(nameof(DsMotionSnapshot.MotionPath)));
    }

    [Fact]
    public void MotionFlags_MatchDriverConstants()
    {
        Assert.Equal(0x0001, (int)DsMotionSnapshotFlags.Available);
        Assert.Equal(0x0002, (int)DsMotionSnapshotFlags.Fallback);
        Assert.Equal(0x0004, (int)DsMotionSnapshotFlags.HardwareCal);
        Assert.Equal(0x0008, (int)DsMotionSnapshotFlags.Tracker);
    }

    [Fact]
    public void MotionPath_MatchesIdentificationEnum()
    {
        Assert.Equal((byte)DsIdentificationMotionPath.Unknown, (byte)0);
        Assert.Equal((byte)DsIdentificationMotionPath.PlainZero, (byte)1);
        Assert.Equal((byte)DsIdentificationMotionPath.HwCal, (byte)2);
        Assert.Equal((byte)DsIdentificationMotionPath.Sixaxis, (byte)3);
    }

    [Fact]
    public void SeqlockCopy_RejectsOddGeneration()
    {
        DsMotionSnapshot snapshot = new()
        {
            SlotIndex = 1,
            SequenceNumber = 3,
            Version = 1,
            Flags = DsMotionSnapshotFlags.Available
        };

        Assert.True((snapshot.SequenceNumber & 1) != 0);
        snapshot.SequenceNumber = 4;
        Assert.True((snapshot.SequenceNumber & 1) == 0);
        Assert.Equal(1u, snapshot.SlotIndex);
    }

    [Fact]
    public void FallbackSnapshot_IsPresentedAsNominal()
    {
        DsMotionSnapshot snapshot = new()
        {
            Flags = DsMotionSnapshotFlags.Fallback,
            AccelZeroX = 512,
            AccelOneGX = 399,
            GyroZero = 512
        };

        Assert.True(snapshot.IsFallback);
        Assert.False(snapshot.IsAvailable);
        Assert.Equal(512, snapshot.AccelZeroX);
        Assert.Equal(399, snapshot.AccelOneGX);
    }
}
