using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models;
using Nefarius.DsHidMini.IPC.Models.Public;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class IpcRuntimeOutputTests
{
    private const uint StatusSuccess = 0x00000000;
    private const uint StatusAccessDenied = 0xC0000022;
    private const uint StatusInvalidParameter = 0xC000000D;
    private const uint StatusInvalidUserBuffer = 0xC00000E8;

    [Theory]
    [InlineData(1, Ds3PlayerLeds.Led1)]
    [InlineData(2, Ds3PlayerLeds.Led2)]
    [InlineData(3, Ds3PlayerLeds.Led3)]
    [InlineData(4, Ds3PlayerLeds.Led4)]
    [InlineData(5, Ds3PlayerLeds.Led1 | Ds3PlayerLeds.Led4)]
    [InlineData(6, Ds3PlayerLeds.Led2 | Ds3PlayerLeds.Led4)]
    [InlineData(7, Ds3PlayerLeds.Led3 | Ds3PlayerLeds.Led4)]
    public void PlayerIndex_MapsToConsoleLedFlags(byte playerIndex, int expectedFlags)
    {
        Assert.True(Ds3PlayerLeds.TryGetFlags(playerIndex, out byte flags));
        Assert.Equal((byte)expectedFlags, flags);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(255)]
    public void PlayerIndex_RejectsOutOfRange(byte playerIndex)
    {
        Assert.False(Ds3PlayerLeds.TryGetFlags(playerIndex, out byte flags));
        Assert.Equal(0, flags);
    }

    [Fact]
    public void DeviceCommandOrdinals_PreserveExistingAbiAndAppendNewCommands()
    {
        Assert.Equal(0u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_INVALID);
        Assert.Equal(1u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO);
        Assert.Equal(2u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX);
        Assert.Equal(3u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_USB_POWER_OFF);
        Assert.Equal(4u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_RUMBLE);
        Assert.Equal(5u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_ALTERNATE_RUMBLE_MODE);
    }

    [Fact]
    public void PlayerIndexReply_UsesSetPlayerIndexCommandTag()
    {
        DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY reply = default;
        reply.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
        reply.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT;
        reply.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX;
        reply.Header.TargetIndex = 1;
        reply.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>();
        reply.NtStatus = StatusSuccess;

        Assert.Equal(
            DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX,
            reply.Header.Command.Device);
        Assert.NotEqual(
            DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO,
            reply.Header.Command.Device);
        Assert.Equal((uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>(), reply.Header.Size);
    }

    [Fact]
    public void RuntimeOutputMessageLayouts_MatchPackedRequestReplySizes()
    {
        Assert.Equal(20, Marshal.SizeOf<DSHM_IPC_MSG_HEADER>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_RUMBLE_REQUEST>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_RUMBLE_REPLY>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY>());
    }

    [Theory]
    [InlineData(StatusSuccess, true)]
    [InlineData(StatusAccessDenied, false)]
    [InlineData(StatusInvalidParameter, false)]
    [InlineData(StatusInvalidUserBuffer, false)]
    public void PlayerIndexAndRumbleNtStatus_UsesDriverNtSuccess(uint status, bool expected)
    {
        Assert.Equal(expected, PowerOffUsbResult.IsNtSuccess(status));
    }
}
