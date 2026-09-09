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
        Assert.Equal(6u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO_CURRENT_HOST);
        Assert.Equal(7u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_DISCONNECT_BLUETOOTH);
        Assert.Equal(8u, (uint)DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_LED_PATTERN);
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
        Assert.Equal(20, Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST>());
        Assert.Equal(28, Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY>());
        Assert.Equal(20, Marshal.SizeOf<DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY>());
        Assert.Equal(6, Marshal.SizeOf<DSHM_IPC_LED_EFFECT>());
        Assert.Equal(48, Marshal.SizeOf<DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST>());
        Assert.Equal(24, Marshal.SizeOf<DSHM_IPC_MSG_SET_LED_PATTERN_REPLY>());
    }

    [Fact]
    public void PairToCurrentHostReply_UsesDedicatedCommandTag()
    {
        DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY reply = default;
        reply.Header.Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_REPLY;
        reply.Header.Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_CLIENT;
        reply.Header.Command.Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO_CURRENT_HOST;
        reply.Header.TargetIndex = 1;
        reply.Header.Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY>();
        reply.WriteStatus = StatusSuccess;
        reply.ReadStatus = StatusSuccess;

        Assert.Equal(
            DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO_CURRENT_HOST,
            reply.Header.Command.Device);
        Assert.NotEqual(
            DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO,
            reply.Header.Command.Device);
    }

    [Theory]
    [InlineData(0x00, true)]
    [InlineData(Ds3PlayerLeds.Led1, true)]
    [InlineData(Ds3PlayerLeds.Led1 | Ds3PlayerLeds.Led4, true)]
    [InlineData(Ds3PlayerLeds.LedOff, true)]
    [InlineData(0x01, false)]
    [InlineData(0x40, false)]
    [InlineData(0xFF, false)]
    public void LedPattern_RejectsReservedFlagBits(byte flags, bool expected)
    {
        Assert.Equal(expected, Ds3LedPattern.AreFlagsValid(flags));
    }

    [Fact]
    public void LedPattern_FromPlayerIndex_UsesStaticEffects()
    {
        Assert.True(Ds3LedPattern.TryFromPlayerIndex(5, out Ds3LedPattern pattern));
        Assert.Equal(Ds3PlayerLeds.Led1 | Ds3PlayerLeds.Led4, pattern.Flags);
        Assert.Equal(Ds3LedEffect.Static.TotalDuration, pattern.Player1.TotalDuration);
        Assert.Equal(Ds3LedEffect.Static.BasePortionDuration, pattern.Player1.BasePortionDuration);
        Assert.False(Ds3LedPattern.TryFromPlayerIndex(0, out _));
    }

    [Fact]
    public void LedPattern_MarshalsIndependentEffectBlocks()
    {
        Ds3LedPattern pattern = new(
            Ds3PlayerLeds.Led1 | Ds3PlayerLeds.Led2,
            new Ds3LedEffect(0xFF, 0x0001, 0x00, 0x01),
            new Ds3LedEffect(0x10, 0x000F, 0x7F, 0x7F),
            Ds3LedEffect.None,
            Ds3LedEffect.FastFlash);

        DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST request = new()
        {
            Header =
            {
                Type = DSHM_IPC_MSG_TYPE.DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE,
                Target = DSHM_IPC_MSG_TARGET.DSHM_IPC_MSG_TARGET_DEVICE,
                Command = { Device = DSHM_IPC_MSG_CMD_DEVICE.DSHM_IPC_MSG_CMD_DEVICE_SET_LED_PATTERN },
                TargetIndex = 1,
                Size = (uint)Marshal.SizeOf<DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST>()
            },
            Flags = pattern.Flags,
            Player1 =
            {
                TotalDuration = pattern.Player1.TotalDuration,
                BasePortionDuration = pattern.Player1.BasePortionDuration,
                OffPortionMultiplier = pattern.Player1.OffPortionMultiplier,
                OnPortionMultiplier = pattern.Player1.OnPortionMultiplier
            },
            Player2 =
            {
                TotalDuration = pattern.Player2.TotalDuration,
                BasePortionDuration = pattern.Player2.BasePortionDuration,
                OffPortionMultiplier = pattern.Player2.OffPortionMultiplier,
                OnPortionMultiplier = pattern.Player2.OnPortionMultiplier
            },
            Player3 =
            {
                TotalDuration = pattern.Player3.TotalDuration,
                BasePortionDuration = pattern.Player3.BasePortionDuration,
                OffPortionMultiplier = pattern.Player3.OffPortionMultiplier,
                OnPortionMultiplier = pattern.Player3.OnPortionMultiplier
            },
            Player4 =
            {
                TotalDuration = pattern.Player4.TotalDuration,
                BasePortionDuration = pattern.Player4.BasePortionDuration,
                OffPortionMultiplier = pattern.Player4.OffPortionMultiplier,
                OnPortionMultiplier = pattern.Player4.OnPortionMultiplier
            }
        };

        Assert.Equal(pattern.Flags, request.Flags);
        Assert.Equal(0x10, request.Player2.TotalDuration);
        Assert.Equal(0x000F, request.Player2.BasePortionDuration);
        Assert.Equal(0x00, request.Player3.OnPortionMultiplier);
        Assert.Equal(Ds3LedEffect.FastFlash.BasePortionDuration, request.Player4.BasePortionDuration);
        Assert.Equal(48u, request.Header.Size);
    }

    [Theory]
    [InlineData(StatusSuccess, StatusSuccess, true)]
    [InlineData(StatusSuccess, StatusAccessDenied, false)]
    [InlineData(StatusInvalidParameter, StatusSuccess, false)]
    public void PairingNtStatus_UsesDriverNtSuccess(uint writeStatus, uint readStatus, bool expected)
    {
        SetHostResult result = new() { WriteStatus = writeStatus, ReadStatus = readStatus };
        Assert.Equal(expected, result.Succeeded);
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
