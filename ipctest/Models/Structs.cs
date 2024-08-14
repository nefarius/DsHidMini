using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nefarius.DsHidMini.IPC.Models;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public struct DSHM_IPC_MSG_COMMAND
{
    [FieldOffset(0)]
    public DSHM_IPC_MSG_CMD_DRIVER Driver;

    [FieldOffset(0)]
    public DSHM_IPC_MSG_CMD_DEVICE Device;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct DSHM_IPC_MSG_HEADER
{
    // What request-behavior is expected (request, request-reply, ...)
    public DSHM_IPC_MSG_TYPE Type;

    // What component is this message targeting (driver, device, ...)
    public DSHM_IPC_MSG_TARGET Target;

    // What command is this message carrying
    public DSHM_IPC_MSG_COMMAND Command;

    // One-based index of which device is this message for
    // Set to 0 if driver is targeted
    public uint TargetIndex;

    // The size of the entire message (header + payload) in bytes
    // A size of 0 is invalid
    public uint Size;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public unsafe struct DSHM_IPC_MSG_PAIR_TO_REQUEST
{
    public DSHM_IPC_MSG_HEADER Header;

    public fixed byte Address[6];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct DSHM_IPC_MSG_PAIR_TO_REPLY
{
    public DSHM_IPC_MSG_HEADER Header;

    public UInt32 WriteStatus;
    
    public UInt32 ReadStatus;
}

public struct SetHostResult
{
    /// <summary>
    ///     The NTSTATUS value of the "pairing" or address overwrite action.
    /// </summary>
    public UInt32 WriteStatus;
    
    /// <summary>
    ///     The NTSTATUS value of the address read/query action to verify the new address.
    /// </summary>
    public UInt32 ReadStatus;

    public override string ToString()
    {
        return $"Pairing result: 0x{WriteStatus:X}, query result: 0x{ReadStatus:X}";
    }
}
