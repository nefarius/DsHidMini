#include "Driver.h"
#include "Configuration.Json.h"
#include "Configuration.tmh"

_Must_inspect_result_
NTSTATUS
ConfigLoadForDevice(
	_Inout_ PDEVICE_CONTEXT Context,
	_In_opt_ BOOLEAN IsHotReload
)
{
	NTSTATUS status = STATUS_SUCCESS;
	CHAR programDataPath[MAX_PATH];
	CHAR configFilePath[MAX_PATH];
	HANDLE hFile = INVALID_HANDLE_VALUE;
	PCHAR content = NULL;
	SIZE_T contentLength = 0;
	LARGE_INTEGER size = { 0 };
	DS_DRIVER_CONFIGURATION parsed;
	DS_CONFIG_RUMBLE_DERIVED rumbleDerived;
	SIZE_T parseOffset = 0;

	FuncEntry(TRACE_CONFIG);

	if (!IsHotReload)
	{
		ConfigSetDefaults(&Context->Configuration);
	}

	do
	{
		if (GetEnvironmentVariableA(
			CONFIG_ENV_VAR_NAME,
			programDataPath,
			MAX_PATH
		) == 0)
		{
			status = STATUS_NOT_FOUND;
			break;
		}

		TraceVerbose(
			TRACE_CONFIG,
			"Expanded environment variable to %s",
			programDataPath
		);

		if (sprintf_s(
			configFilePath,
			ARRAYSIZE(configFilePath),
			"%s\\%s\\%s",
			programDataPath,
			CONFIG_SUB_DIR_NAME,
			CONFIG_FILE_NAME
		) == -1)
		{
			status = STATUS_BUFFER_OVERFLOW;
			break;
		}

		TraceVerbose(
			TRACE_CONFIG,
			"Set config file path to %s",
			configFilePath
		);

		hFile = CreateFileA(configFilePath,
			GENERIC_READ,
			FILE_SHARE_READ,
			NULL,
			OPEN_EXISTING,
			FILE_ATTRIBUTE_NORMAL,
			NULL
		);

		DWORD error = GetLastError();

		if (hFile == INVALID_HANDLE_VALUE)
		{
			TraceError(
				TRACE_CONFIG,
				"Configuration file %s not accessible, error: %!WINERROR!",
				configFilePath,
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"Reading configuration file", error);

			status = STATUS_ACCESS_DENIED;
			break;
		}

		if (!GetFileSizeEx(hFile, &size))
		{
			error = GetLastError();

			TraceError(
				TRACE_CONFIG,
				"Failed to get configuration file size, error: %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"Getting configuration file size", error);

			status = STATUS_ACCESS_DENIED;
			break;
		}

		TraceVerbose(
			TRACE_CONFIG,
			"File size in bytes: %Iu64",
			(size_t)size.QuadPart
		);

		if (size.QuadPart <= 0 || size.QuadPart > CONFIG_JSON_MAX_BYTES)
		{
			TraceError(
				TRACE_CONFIG,
				"Configuration file size is not usable, reported size: %I64d",
				size.QuadPart
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"Reading configuration file", ERROR_BUFFER_OVERFLOW);
			status = STATUS_BUFFER_OVERFLOW;
			break;
		}

		contentLength = (SIZE_T)size.QuadPart;
		content = (char*)calloc(contentLength + 1, sizeof(char));

		if (content == NULL)
		{
			status = STATUS_NO_MEMORY;
			break;
		}

		DWORD bytesRead = 0;

		if (!ReadFile(hFile, content, (DWORD)contentLength, &bytesRead, NULL))
		{
			error = GetLastError();

			TraceError(
				TRACE_CONFIG,
				"Failed to read configuration file content, error: %!WINERROR!",
				error
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"Reading configuration file content", error);
			status = STATUS_UNSUCCESSFUL;
			break;
		}

		if (bytesRead != (DWORD)contentLength)
		{
			TraceError(
				TRACE_CONFIG,
				"Configuration file read was incomplete: %lu of %Iu bytes",
				bytesRead,
				contentLength
			);
			status = STATUS_UNSUCCESSFUL;
			break;
		}

		status = ConfigParseJsonDocument(
			content,
			contentLength,
			Context->DeviceAddressString,
			IsHotReload,
			IsHotReload ? &Context->Configuration : NULL,
			&parsed,
			&rumbleDerived,
			&parseOffset
		);

		if (!NT_SUCCESS(status))
		{
			TraceError(
				TRACE_CONFIG,
				"JSON parsing failed at offset %Iu",
				parseOffset
			);
			EventWriteJSONParseError("invalid configuration document");
			break;
		}

		Context->Configuration = parsed;

		if (rumbleDerived.LightRescaleIsAllowed)
		{
			Context->RumbleControlState.AltMode.IsEnabled = rumbleDerived.AltModeIsEnabled;
			Context->RumbleControlState.AltMode.LightRescale.ConstA = rumbleDerived.LightRescaleConstA;
			Context->RumbleControlState.AltMode.LightRescale.ConstB = rumbleDerived.LightRescaleConstB;
			Context->RumbleControlState.AltMode.LightRescale.IsAllowed = TRUE;

			TraceVerbose(
				TRACE_CONFIG,
				"Light rumble rescaling constants: A = %f and B = %f.",
				Context->RumbleControlState.AltMode.LightRescale.ConstA,
				Context->RumbleControlState.AltMode.LightRescale.ConstB
			);
		}
		else
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Disallowing light rumble rescaling because an invalid range was defined"
			);
			Context->RumbleControlState.AltMode.LightRescale.IsAllowed = FALSE;
		}

		if (rumbleDerived.HeavyRescaleIsAllowed)
		{
			Context->RumbleControlState.HeavyRescaleEnabled = rumbleDerived.HeavyRescaleEnabled;
			Context->RumbleControlState.HeavyRescale.ConstA = rumbleDerived.HeavyRescaleConstA;
			Context->RumbleControlState.HeavyRescale.ConstB = rumbleDerived.HeavyRescaleConstB;
			Context->RumbleControlState.HeavyRescale.IsAllowed = TRUE;

			TraceVerbose(
				TRACE_CONFIG,
				"Heavy rumble rescaling constants:  A = %f and B = %f.",
				Context->RumbleControlState.HeavyRescale.ConstA,
				Context->RumbleControlState.HeavyRescale.ConstB
			);
		}
		else
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Disallowing heavy rumble rescalling because an invalid range was defined"
			);
			Context->RumbleControlState.HeavyRescale.IsAllowed = FALSE;
		}

		if (IsHotReload)
		{
			Context->Connection.Bth.IdleDisconnectTimestamp.QuadPart = 0;
		}

	} while (FALSE);

	if (content)
	{
		free(content);
	}

	if (hFile != INVALID_HANDLE_VALUE)
	{
		CloseHandle(hFile);
	}

	FuncExit(TRACE_CONFIG, "status=%!STATUS!", status);

	return status;
}
