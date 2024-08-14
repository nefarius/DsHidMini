using System.Diagnostics.CodeAnalysis;

namespace Nefarius.DsHidMini.IPC.Models;

// Describes the type of IPC message response behavior
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_TYPE : UInt32
{
    // Invalid/reserved, do not use
    DSHM_IPC_MSG_TYPE_INVALID = 0,

    // Client-to-driver data incoming, no acknowledgment/reply requested
    DSHM_IPC_MSG_TYPE_REQUEST_ONLY,

    // Client-to-driver data incoming, must be acknowledged by reply
    DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE,

    // Client requested data, there is nothing to read for the driver
    DSHM_IPC_MSG_TYPE_RESPONSE_ONLY,

    // Driver-to-client response to a previous DSHM_IPC_MSG_TYPE_REQUEST_RESPONSE
    DSHM_IPC_MSG_TYPE_REQUEST_REPLY
}

// Describes the message receiver
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_TARGET : UInt32
{
    // Invalid/reserved, do not use
    DSHM_IPC_MSG_TARGET_INVALID = 0,

    // The message is targeted at the driver
    DSHM_IPC_MSG_TARGET_DRIVER,

    // The message is targeted at a device
    DSHM_IPC_MSG_TARGET_DEVICE,

    // The message is targeted at the client/caller/app
    DSHM_IPC_MSG_TARGET_CLIENT
}

// Describes a per-driver command
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_CMD_DRIVER : UInt32
{
    // Invalid/reserved, do not use
    DSHM_IPC_MSG_CMD_DRIVER_INVALID = 0,

    // Message without payload, useful to check for functionality
    DSHM_IPC_MSG_CMD_DRIVER_PING
}

// Describes a per-device command
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum DSHM_IPC_MSG_CMD_DEVICE : UInt32
{
    // Invalid/reserved, do not use
    DSHM_IPC_MSG_CMD_DEVICE_INVALID = 0,

    // Pair a given device to a new host
    DSHM_IPC_MSG_CMD_DEVICE_PAIR_TO
}