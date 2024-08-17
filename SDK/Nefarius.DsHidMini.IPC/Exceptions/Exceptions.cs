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
    private readonly DSHM_IPC_MSG_HEADER? _header;

    internal DsHidMiniInteropUnexpectedReplyException() : base("A request reply was malformed.") { }

    internal DsHidMiniInteropUnexpectedReplyException(ref DSHM_IPC_MSG_HEADER header) : this()
    {
        _header = header;
    }

    /// <inheritdoc />
    public override string Message =>
        _header.HasValue
            ? $"{base.Message}, Type: {_header.Value.Type}, Target: {_header.Value.Target}, Command: {_header.Value.Command.Driver}, TargetIndex: {_header.Value.TargetIndex}, Size: {_header.Value.Size}"
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