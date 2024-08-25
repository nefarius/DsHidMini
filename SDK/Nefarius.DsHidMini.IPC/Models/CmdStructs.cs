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
///     Requests the driver host process PID and a wait handle for new input reports
/// </summary>
/// <remarks>Post-processing this command requires elevated privileges.</remarks>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[StructLayout(LayoutKind.Sequential)]
internal struct DSHM_IPC_MSG_GET_HID_WAIT_HANDLE_RESPONSE
{
    public DSHM_IPC_MSG_HEADER Header;

    /// <summary>
    ///     The driver hosting process PID
    /// </summary>
    public UInt32 ProcessId;

    /// <summary>
    ///     A handle to an auto-reset event
    /// </summary>
    /// <remarks>The requester of this handle must duplicate it into the current process before it becomes usable.</remarks>
    public IntPtr WaitHandle;
}
