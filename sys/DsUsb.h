#pragma once

const __declspec(selectany) LONGLONG DEFAULT_CONTROL_TRANSFER_TIMEOUT = 5 * -1 * WDF_TIMEOUT_TO_SEC;
#define INTERRUPT_IN_BUFFER_LENGTH          128
#define CONTROL_TRANSFER_BUFFER_LENGTH      64

NTSTATUS
SendControlRequest(
    _In_ PDEVICE_CONTEXT Context,
    _In_ WDF_USB_BMREQUEST_DIRECTION Direction,
    _In_ WDF_USB_BMREQUEST_TYPE Type,
    _In_ BYTE Request,
    _In_ USHORT Value,
    _In_ USHORT Index,
    _In_ PVOID Buffer,
    _In_ ULONG BufferLength);

NTSTATUS
DsUsbConfigContReaderForInterruptEndPoint(
    _In_ WDFDEVICE Device
);

EVT_WDF_USB_READER_COMPLETION_ROUTINE DsUsb_EvtUsbInterruptPipeReadComplete;
EVT_WDF_USB_READERS_FAILED DsUsbEvtUsbInterruptReadersFailed;
