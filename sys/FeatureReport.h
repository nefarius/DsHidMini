#pragma once

_Must_inspect_result_
_Success_(return == STATUS_SUCCESS)
NTSTATUS
DSHM_GetFeature(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
);

_Must_inspect_result_
_Success_(return == STATUS_SUCCESS)
NTSTATUS
DSHM_SetFeature(
	_In_ const PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
);
