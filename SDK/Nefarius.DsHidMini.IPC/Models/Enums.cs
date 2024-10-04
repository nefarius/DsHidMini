using System.Diagnostics.CodeAnalysis;

namespace Nefarius.DsHidMini.IPC.Models;

/// <summary>
///     Describes the type of IPC message response behavior
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_TYPE : UInt32
{
    /// <summary>
    ///     Invalid/reserved, do not use
    /// </summary>
    DSHM_IPC_MSG_TYPE_INVALID = 0,

    /// <summary>
    ///     Client-to-driver data incoming, no acknowledgment/reply requested
    /// </summary>
    DSHM_IPC_MSG_TYPE_REQUEST_ONLY,

    /// <summary>
    ///     Client-to-driver data incoming, must be acknowledged by reply
    /// </summary>
    DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE,

    /// <summary>
    ///     Client requested data, there is nothing to read for the driver
    /// </summary>
    DSHM_IPC_MSG_TYPE_RESPONSE_ONLY,

    /// <summary>
    ///     Driver-to-client response to a previous DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE
    /// </summary>
    DSHM_IPC_MSG_TYPE_REQUEST_REPLY
}

/// <summary>
///     Describes the message receiver
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_TARGET : UInt32
{
    /// <summary>
    ///     Invalid/reserved, do not use
    /// </summary>
    DSHM_IPC_MSG_TARGET_INVALID = 0,

    /// <summary>
    ///     The message is targeted at the driver
    /// </summary>
    DSHM_IPC_MSG_TARGET_DRIVER,

    /// <summary>
    ///     The message is targeted at a device
    /// </summary>
    DSHM_IPC_MSG_TARGET_DEVICE,

    /// <summary>
    ///     The message is targeted at the client/caller/app
    /// </summary>
    DSHM_IPC_MSG_TARGET_CLIENT
}

/// <summary>
///     Describes a per-driver command
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_CMD_DRIVER : UInt32
{
    /// <summary>
    ///     Invalid/reserved, do not use
    /// </summary>
    DSHM_IPC_MSG_CMD_DRIVER_INVALID = 0,

    /// <summary>
    ///     Message without payload, useful to check for functionality
    /// </summary>
    DSHM_IPC_MSG_CMD_DRIVER_PING
}

// Describes a per-device command
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_CMD_DEVICE : UInt32
{
    /// <summary>
    ///     Invalid/reserved, do not use
    /// </summary>
    DSHM_IPC_MSG_CMD_DEVICE_INVALID = 0,

    /// <summary>
    ///     Pair a given device to a new host
    /// </summary>
    DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO,

    /// <summary>
    ///     Requests a player index update (switch player LED etc.)
    /// </summary>
    DSHM_IPC_MSG_CMD_DEVICE_SET_PLAYER_INDEX,

    /// <summary>
    ///     Requests a wait handle for input report state changes
    /// </summary>
    DSHM_IPC_MSG_CMD_DEVICE_GET_HID_WAIT_HANDLE
}