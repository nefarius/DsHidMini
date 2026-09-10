#pragma once

#define CONFIG_ENV_VAR_NAME		"ProgramData"
#define CONFIG_SUB_DIR_NAME		"DsHidMini"
#define CONFIG_FILE_NAME		"DsHidMini.json"

typedef struct _DEVICE_CONTEXT* PDEVICE_CONTEXT;

_Must_inspect_result_
NTSTATUS
ConfigLoadForDevice(
	_Inout_ PDEVICE_CONTEXT Context,
	_In_opt_ BOOLEAN IsHotReload
);

//
// Reads the driver-wide IPCEnabled gate from DsHidMini.json.
// A missing file or missing property reports Enabled = TRUE.
// On parse/I/O failure Enabled is left untouched.
// 
_Must_inspect_result_
NTSTATUS
ConfigLoadIpcEnabled(
	_Out_ PBOOLEAN Enabled
);
