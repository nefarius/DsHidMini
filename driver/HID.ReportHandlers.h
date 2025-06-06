#pragma once

_Must_inspect_result_
_Success_(return == STATUS_SUCCESS)
NTSTATUS
DSHM_GetFeature(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
);

_Must_inspect_result_
_Success_(return == STATUS_SUCCESS)
NTSTATUS
DSHM_SetFeature(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
);

_Must_inspect_result_
_Success_(return == STATUS_SUCCESS)
NTSTATUS
DSHM_WriteReport(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ const DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ HID_XFER_PACKET* Packet,
	_Out_ ULONG* ReportSize
);

void
DSHM_ParseInputReport(
	_In_ PDEVICE_CONTEXT DeviceContext,
	_In_ DMF_CONTEXT_DsHidMini* ModuleContext,
	_In_ PDS3_RAW_INPUT_REPORT Report
);
