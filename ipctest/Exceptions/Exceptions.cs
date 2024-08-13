namespace Nefarius.DsHidMini.IPC.Exceptions;

public sealed class DsHidMiniInteropExclusiveAccessException : Exception
{
    internal DsHidMiniInteropExclusiveAccessException() : base(
        "Another client is already connected to the driver, can't continue initialization.")
    {
    }
}

public sealed class DsHidMiniInteropUnavailableException : Exception
{
    internal DsHidMiniInteropUnavailableException() : base(
        "Driver IPC unavailable, make sure that at least one compatible controller is connected and operational.")
    {
    }
}

public sealed class DsHidMiniInteropReplyTimeoutException : Exception
{
    internal DsHidMiniInteropReplyTimeoutException() : base("Operation timed out while waiting for a request reply.")
    {
    }
}

public sealed class DsHidMiniInteropUnexpectedReplyException : Exception
{
    internal DsHidMiniInteropUnexpectedReplyException() : base("A request reply was malformed.")
    {
    }
}