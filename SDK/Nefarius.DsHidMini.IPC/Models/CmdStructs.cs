using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Nefarius.DsHidMini.IPC.Models.Public;

namespace Nefarius.DsHidMini.IPC.Models;

/// <summary>
///     What command is this message carrying
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal struct DSHM_IPC_MSG_COMMAND
{
    /// <summary>
    ///     Driver global command
    /// </summary>
    [FieldOffset(0)]
    public DSHM_IPC_MSG_CMD_DRIVER Driver;

    /// <summary>
    ///     Device-specific command
    /// </summary>
    [FieldOffset(0)]
    public DSHM_IPC_MSG_CMD_DEVICE Device;
}

/// <summary>
///     Prefix of every packet describing the message
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_HEADER
{
    /// <summary>
    ///     What request-behavior is expected (request, request-reply, ...)
    /// </summary>
    public DSHM_IPC_MSG_TYPE Type;

    /// <summary>
    ///     What component is this message targeting (driver, device, ...)
    /// </summary>
    public DSHM_IPC_MSG_TARGET Target;

    /// <summary>
    ///     What command is this message carrying
    /// </summary>
    public DSHM_IPC_MSG_COMMAND Command;

    /// <summary>
    ///     One-based index of which device is this message for
    /// </summary>
    /// <remarks>Set to 0 if driver is targeted</remarks>
    public uint TargetIndex;

    /// <summary>
    ///     The size of the entire message (header + payload) in bytes
    /// </summary>
    /// <remarks>A size of 0 is invalid</remarks>
    public uint Size;
}

/// <summary>
///     Updates a specified devices' host address
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal unsafe struct DSHM_IPC_MSG_PAIR_TO_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    public fixed byte Address[6];
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_PAIR_TO_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_PAIR_TO_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     NTSTATUS of the set address action
    /// </summary>
    public UInt32 WriteStatus;

    /// <summary>
    ///     NTSTATUS of the get address action
    /// </summary>
    public UInt32 ReadStatus;
}

/// <summary>
///     Updates the player index of a given device
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     The new player index to set
    /// </summary>
    /// <remarks>Valid values are 1 to 7</remarks>
    public byte PlayerIndex;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_SET_PLAYER_INDEX_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_PLAYER_INDEX_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 NtStatus;
}

/// <summary>
///     Requests the console-style USB power-off sequence (issue #366).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_USB_POWER_OFF_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_USB_POWER_OFF_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_USB_POWER_OFF_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     NTSTATUS of the 48-byte zero output report (LEDs/rumble off)
    /// </summary>
    public UInt32 IndicatorsOffStatus;

    /// <summary>
    ///     NTSTATUS of the Feature 0xF4 disable transfer
    /// </summary>
    public UInt32 ShutdownStatus;
}

/// <summary>
///     Updates rumble motor strengths on a given device.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_RUMBLE_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     Heavy / left motor strength (0-255).
    /// </summary>
    public byte LargeMotor;

    /// <summary>
    ///     Light / right motor strength (0-255).
    /// </summary>
    public byte SmallMotor;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_SET_RUMBLE_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_RUMBLE_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 NtStatus;
}

/// <summary>
///     Toggles alternative rumble mode for a given device (volatile).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     Non-zero enables alternative rumble mode.
    /// </summary>
    public byte IsEnabled;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_ALTERNATE_RUMBLE_MODE_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 NtStatus;
}

/// <summary>
///     One DS3 LED effect block for IPC. The reserved byte keeps
///     <see cref="BasePortionDuration" /> naturally aligned.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_LED_EFFECT
{
    public byte TotalDuration;
    public byte Reserved;
    public ushort BasePortionDuration;
    public byte OffPortionMultiplier;
    public byte OnPortionMultiplier;
}

/// <summary>
///     Pair a given device to the active local Bluetooth radio.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_PAIR_TO_CURRENT_HOST_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 WriteStatus;

    public UInt32 ReadStatus;
}

/// <summary>
///     Disconnect a currently wireless device from the host radio.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_DISCONNECT_BLUETOOTH_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 NtStatus;
}

/// <summary>
///     Apply a full volatile LED pattern (flags + four independent effects).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    public byte Flags;

    public byte Reserved0;
    public byte Reserved1;
    public byte Reserved2;

    public DSHM_IPC_LED_EFFECT Player1;
    public DSHM_IPC_LED_EFFECT Player2;
    public DSHM_IPC_LED_EFFECT Player3;
    public DSHM_IPC_LED_EFFECT Player4;
}

/// <summary>
///     Reply to <see cref="DSHM_IPC_MSG_SET_LED_PATTERN_REQUEST" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal struct DSHM_IPC_MSG_SET_LED_PATTERN_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 NtStatus;
}
