#pragma once

#include "DsCommon.h"

#ifndef STATUS_SUCCESS
#define STATUS_SUCCESS ((NTSTATUS)0x00000000L)
#endif

#ifndef STATUS_INVALID_PARAMETER
#define STATUS_INVALID_PARAMETER ((NTSTATUS)0xC000000DL)
#endif

#ifndef STATUS_DATA_ERROR
#define STATUS_DATA_ERROR ((NTSTATUS)0xC000003EL)
#endif

#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

#ifdef __cplusplus
extern "C" {
#endif

//
// Practical upper bound for DsHidMini.json. Real profiles are a few kilobytes.
// 
#define CONFIG_JSON_MAX_BYTES           (256u * 1024u)

//
// Must stay in sync with the CJSON_NESTING_LIMIT preprocessor define used
// when compiling cJSON.c for the driver and the host parser tests.
// 
#define CONFIG_JSON_MAX_NESTING         32

typedef struct _DS_CONFIG_RUMBLE_DERIVED
{
	BOOLEAN HeavyRescaleEnabled;

	BOOLEAN HeavyRescaleIsAllowed;

	DOUBLE HeavyRescaleConstA;

	DOUBLE HeavyRescaleConstB;

	BOOLEAN AltModeIsEnabled;

	BOOLEAN LightRescaleIsAllowed;

	DOUBLE LightRescaleConstA;

	DOUBLE LightRescaleConstB;
} DS_CONFIG_RUMBLE_DERIVED, * PDS_CONFIG_RUMBLE_DERIVED;

void
ConfigSetDefaults(
	_Inout_ PDS_DRIVER_CONFIGURATION Config
);

void
ConfigDeriveRumbleState(
	_In_ const DS_DRIVER_CONFIGURATION* Config,
	_Out_ PDS_CONFIG_RUMBLE_DERIVED Derived
);

//
// Parses one complete JSON document into a candidate configuration.
// Json[Length] must be a terminating NUL; Json[0..Length) is the payload.
// On failure, Parsed is left untouched.
// 
_Must_inspect_result_
NTSTATUS
ConfigParseJsonDocument(
	_In_reads_bytes_(Length) const CHAR* Json,
	_In_ SIZE_T Length,
	_In_opt_z_ const CHAR* DeviceAddress,
	_In_ BOOLEAN IsHotReload,
	_In_opt_ const DS_DRIVER_CONFIGURATION* Current,
	_Out_ PDS_DRIVER_CONFIGURATION Parsed,
	_Out_ PDS_CONFIG_RUMBLE_DERIVED RumbleDerived,
	_Out_opt_ PSIZE_T ErrorOffset
);

//
// Reads the root-level IPCEnabled gate. Missing or omitted defaults to TRUE.
// A present non-boolean value fails and leaves Enabled untouched.
// 
_Must_inspect_result_
NTSTATUS
ConfigParseIpcEnabled(
	_In_reads_bytes_(Length) const CHAR* Json,
	_In_ SIZE_T Length,
	_Out_ PBOOLEAN Enabled,
	_Out_opt_ PSIZE_T ErrorOffset
);

#ifdef __cplusplus
}
#endif
