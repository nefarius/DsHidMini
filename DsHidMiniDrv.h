#pragma once



NTSTATUS
DsHidMini_GetInputReport(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _In_ HID_XFER_PACKET* Packet,
    _Out_ ULONG* ReportSize
);

NTSTATUS
DsHidMini_RetrieveNextInputReport(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _Out_ UCHAR** Buffer,
    _Out_ ULONG* BufferSize
);
