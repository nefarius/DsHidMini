namespace Nefarius.DsHidMini.IPC.Models.Public;

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

    /// <summary>
    ///     <see langword="true" /> when both the pairing write and the verify read succeeded.
    /// </summary>
    public bool Succeeded => PowerOffUsbResult.IsNtSuccess(WriteStatus) && PowerOffUsbResult.IsNtSuccess(ReadStatus);

    public override string ToString()
    {
        return $"Pairing result: 0x{WriteStatus:X}, query result: 0x{ReadStatus:X}";
    }
}