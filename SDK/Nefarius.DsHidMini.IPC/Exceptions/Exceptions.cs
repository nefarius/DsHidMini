using Nefarius.DsHidMini.IPC.Models;

namespace Nefarius.DsHidMini.IPC.Exceptions;

/// <summary>
///     Driver IPC unavailable, make sure that at least one compatible controller is connected and operational.
/// </summary>
public sealed class DsHidMiniInteropUnavailableException : Exception
{
    internal DsHidMiniInteropUnavailableException() : base(
        "Driver IPC unavailable, make sure that at least one compatible controller is connected and operational.")
    {
    }
}

/// <summary>
///     Operation timed out while waiting for a request reply.
/// </summary>
public sealed class DsHidMiniInteropReplyTimeoutException : Exception
{
    internal DsHidMiniInteropReplyTimeoutException() : base("Operation timed out while waiting for a request reply.")
    {
    }
}

/// <summary>
///     A request reply was malformed.
/// </summary>
public sealed class DsHidMiniInteropUnexpectedReplyException : Exception
{
    private readonly unsafe DSHM_IPC_MSG_HEADER* _header = null;

    internal DsHidMiniInteropUnexpectedReplyException() { }

    internal unsafe DsHidMiniInteropUnexpectedReplyException(DSHM_IPC_MSG_HEADER* header) : base(
        "A request reply was malformed.")
    {
        _header = header;
    }

    /// <inheritdoc />
    public override unsafe string Message =>
        _header is not null
            ? $"{base.Message}, Type: {_header->Type}, Target: {_header->Target}, Command: {_header->Command.Driver}, TargetIndex: {_header->TargetIndex}, Size: {_header->Size}"
            : base.Message;
}

/// <summary>
///     Multiple threads tried to invoke IPC methods at the same time, which is not supported.
/// </summary>
public sealed class DsHidMiniInteropConcurrencyException : Exception
{
    internal DsHidMiniInteropConcurrencyException() : base(
        "Multiple threads tried to invoke IPC methods at the same time, which is not supported.")
    {
    }
}

/// <summary>
///     The provided device index was out of range; allowed values must remain between (including) 1 and 255.
/// </summary>
public sealed class DsHidMiniInteropInvalidDeviceIndexException : Exception
{
    internal DsHidMiniInteropInvalidDeviceIndexException(int deviceIndex) : base(
        $"The provided device index {deviceIndex} was out of range; allowed values must remain between (including) 1 and 255.")
    {
    }
}