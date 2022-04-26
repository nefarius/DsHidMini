#pragma once

#define CONFIG_ENV_VAR_NAME		"ProgramData"
#define CONFIG_SUB_DIR_NAME		"DsHidMini"
#define CONFIG_FILE_NAME		"DsHidMini.json"

typedef struct _DEVICE_CONTEXT* PDEVICE_CONTEXT;

DS_HID_DEVICE_MODE
HID_DEVICE_MODE_FROM_NAME(
	PSTR ModeName
);

_Must_inspect_result_
NTSTATUS
ConfigLoadForDevice(
	_Inout_ PDEVICE_CONTEXT Context,
	_In_opt_ BOOLEAN IsHotReload
);

void
ConfigSetDefaults(
	_Inout_ PDS_DRIVER_CONFIGURATION Config
);
