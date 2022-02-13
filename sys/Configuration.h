#pragma once

typedef struct _DEVICE_CONTEXT *PDEVICE_CONTEXT;

_Must_inspect_result_
NTSTATUS
ConfigLoadForDeviceAddress(
	_In_ PBD_ADDR Address,
	_Outptr_ PDEVICE_CONTEXT Context
);
