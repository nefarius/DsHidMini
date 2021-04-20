#pragma once

VOID FORCEINLINE REVERSE_BYTE_ARRAY(PUCHAR start, int size)
{
    PUCHAR lo = start;
    PUCHAR hi = start + size - 1;
    UCHAR swap;
    while (lo < hi) {
        swap = *lo;
        *lo++ = *hi;
        *hi-- = swap;
    }
}

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

NTSTATUS
DsHidMini_GetFeature(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _In_ HID_XFER_PACKET* Packet,
    _Out_ ULONG* ReportSize
);

NTSTATUS
DsHidMini_SetFeature(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _In_ HID_XFER_PACKET* Packet,
    _Out_ ULONG* ReportSize
);

NTSTATUS
DsHidMini_SetOutputReport(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _In_ HID_XFER_PACKET* Packet,
    _Out_ ULONG* ReportSize
);

NTSTATUS
DsHidMini_WriteReport(
    _In_ DMFMODULE DmfModule,
    _In_ WDFREQUEST Request,
    _In_ HID_XFER_PACKET* Packet,
    _Out_ ULONG* ReportSize
);

VOID Ds_ProcessHidInputReport(PDEVICE_CONTEXT Context, PDS3_RAW_INPUT_REPORT Report);

NTSTATUS Ds_SendOutputReport(PDEVICE_CONTEXT Context, DS_OUTPUT_REPORT_SOURCE Source);

VOID DumpAsHex(PCSTR Prefix, PVOID Buffer, ULONG BufferLength);
