#include "Driver.h"
#include "Configuration.tmh"


//
// Reads/refreshes configuration from disk (JSON) to provided context
// 
void ConfigNodeParse(
	_In_ const cJSON* ParentNode,
	_Inout_ PDEVICE_CONTEXT Context
)
{
	PDS_DRIVER_CONFIGURATION pCfg = &Context->Configuration;
	cJSON* pNode = NULL;

	//
	// Common
	// 

	if ((pNode = cJSON_GetObjectItem(ParentNode, "HidDeviceMode")))
	{
		pCfg->HidDeviceMode = (DS_HID_DEVICE_MODE)cJSON_GetNumberValue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "DisableAutoPairing")))
	{
		pCfg->DisableAutoPairing = cJSON_GetNumberValue(pNode) > 0.0;
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputRateControlEnabled")))
	{
		pCfg->IsOutputRateControlEnabled = cJSON_GetNumberValue(pNode) > 0.0;
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "OutputRateControlPeriodMs")))
	{
		pCfg->OutputRateControlPeriodMs = (UCHAR)cJSON_GetNumberValue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputDeduplicatorEnabled")))
	{
		pCfg->IsOutputDeduplicatorEnabled = cJSON_GetNumberValue(pNode) > 0.0;
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "WirelessIdleTimeoutPeriodMs")))
	{
		pCfg->WirelessIdleTimeoutPeriodMs = (ULONG)cJSON_GetNumberValue(pNode);
	}

	//
	// SDF
	// 

	const cJSON* pSDF = cJSON_GetObjectItem(ParentNode, "SDF");

	if (pSDF)
	{
		if ((pNode = cJSON_GetObjectItem(pSDF, "PressureExposureMode")))
		{
			pCfg->SDF.PressureExposureMode = (DS_PRESSURE_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pSDF, "DPadExposureMode")))
		{
			pCfg->SDF.DPadExposureMode = (DS_DPAD_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
		}

		const cJSON* pDeadZoneLeft = cJSON_GetObjectItem(pSDF, "DeadZoneLeft");

		if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "Apply")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneLeft.Apply = cJSON_GetNumberValue(pNode) > 0.0;
		}

		if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "PolarValue")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneLeft.PolarValue = cJSON_GetNumberValue(pNode);
		}

		const cJSON* pDeadZoneRight = cJSON_GetObjectItem(pSDF, "DeadZoneRight");

		if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "Apply")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneRight.Apply = cJSON_GetNumberValue(pNode) > 0.0;
		}

		if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "PolarValue")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneRight.PolarValue = cJSON_GetNumberValue(pNode);
		}
	}

	//
	// GPJ
	// 

	const cJSON* pGPJ = cJSON_GetObjectItem(ParentNode, "GPJ");

	if (pGPJ)
	{
		if ((pNode = cJSON_GetObjectItem(pGPJ, "PressureExposureMode")))
		{
			pCfg->GPJ.PressureExposureMode = (DS_PRESSURE_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pGPJ, "DPadExposureMode")))
		{
			pCfg->GPJ.DPadExposureMode = (DS_DPAD_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
		}

		const cJSON* pDeadZoneLeft = cJSON_GetObjectItem(pGPJ, "DeadZoneLeft");

		if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "Apply")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneLeft.Apply = cJSON_GetNumberValue(pNode) > 0.0;
		}

		if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "PolarValue")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneLeft.PolarValue = cJSON_GetNumberValue(pNode);
		}

		const cJSON* pDeadZoneRight = cJSON_GetObjectItem(pGPJ, "DeadZoneRight");

		if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "Apply")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneRight.Apply = cJSON_GetNumberValue(pNode) > 0.0;
		}

		if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "PolarValue")))
		{
			pCfg->SDF.ThumbSettings.DeadZoneRight.PolarValue = cJSON_GetNumberValue(pNode);
		}
	}
}

_Must_inspect_result_
NTSTATUS
ConfigLoadForDevice(
	_Inout_ PDEVICE_CONTEXT Context
)
{
	NTSTATUS status = STATUS_SUCCESS;
	CHAR programDataPath[MAX_PATH];
	CHAR configFilePath[MAX_PATH];
	CHAR deviceAddress[13];
	FILE* fp = NULL;
	PCHAR content = NULL;
	cJSON* config_json = NULL;

	FuncEntry(TRACE_CONFIG);

	ConfigSetDefaults(&Context->Configuration);

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

		if (sprintf_s(
			configFilePath,
			MAX_PATH / sizeof(WCHAR),
			"%s\\%s\\%s",
			programDataPath,
			CONFIG_SUB_DIR_NAME,
			CONFIG_FILE_NAME
		) == -1)
		{
			status = STATUS_BUFFER_OVERFLOW;
			break;
		}

		errno_t error;

		if ((error = fopen_s(&fp, configFilePath, "r")) != 0)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Configuration file %s not accessible (errno: %d)",
				configFilePath,
				error
			);

			status = STATUS_ACCESS_DENIED;
			break;
		}

		fseek(fp, 0L, SEEK_END);
		const long numBytes = ftell(fp);
		fseek(fp, 0L, SEEK_SET);

		content = (char*)calloc(numBytes, sizeof(char));

		if (content == NULL)
		{
			status = STATUS_NO_MEMORY;
			break;
		}

		fread(content, sizeof(char), numBytes, fp);

		config_json = cJSON_Parse(content);

		if (config_json == NULL)
		{
			const char* error_ptr = cJSON_GetErrorPtr();
			if (error_ptr != NULL)
			{
				TraceError(
					TRACE_CONFIG,
					"JSON error: %s",
					error_ptr
				);
			}

			status = STATUS_ACCESS_VIOLATION;
			break;
		}

		switch (Context->ConnectionType)
		{
		case DsDeviceConnectionTypeUsb:

			sprintf_s(
				deviceAddress,
				ARRAYSIZE(deviceAddress),
				"%02X%02X%02X%02X%02X%02X",
				Context->DeviceAddress.Address[0],
				Context->DeviceAddress.Address[1],
				Context->DeviceAddress.Address[2],
				Context->DeviceAddress.Address[3],
				Context->DeviceAddress.Address[4],
				Context->DeviceAddress.Address[5]
			);

			break;
		case DsDeviceConnectionTypeBth:

			sprintf_s(
				deviceAddress,
				ARRAYSIZE(deviceAddress),
				"%012llX",
				*(PULONGLONG)&Context->DeviceAddress
			);

			break;
		default:
			status = STATUS_INVALID_PARAMETER;
			break;
		}

		if (!NT_SUCCESS(status))
		{
			break;
		}

		//
		// Read global configuration first, then overwrite device-specific ones
		// 
		const cJSON* globalNode = cJSON_GetObjectItem(config_json, "Global");

		if (globalNode)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Reading global configuration"
			);

			ConfigNodeParse(globalNode, Context);
		}

		const cJSON* devicesNode = cJSON_GetObjectItem(config_json, "Devices");

		//
		// Try to read device-specific properties
		// 
		cJSON* deviceNode = cJSON_GetObjectItem(devicesNode, deviceAddress);

		if (deviceNode)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Found device config for %s",
				deviceAddress
			);

			ConfigNodeParse(deviceNode, Context);
		}

	} while (FALSE);

	if (config_json)
	{
		cJSON_Delete(config_json);
	}

	if (content)
	{
		free(content);
	}

	if (fp)
	{
		fclose(fp);
	}

	FuncExit(TRACE_CONFIG, "status=%!STATUS!", status);

	return status;
}

//
// Set default values (if no customized configuration is available)
// 
void
ConfigSetDefaults(
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	//
	// Common
	// 

	Config->HidDeviceMode = DsHidMiniDeviceModeXInputHIDCompatible;
	Config->DisableAutoPairing = FALSE;
	Config->IsOutputRateControlEnabled = TRUE;
	Config->OutputRateControlPeriodMs = 150;
	Config->IsOutputDeduplicatorEnabled = FALSE;
	Config->WirelessIdleTimeoutPeriodMs = 300000;

	//
	// SDF
	// 

	Config->SDF.PressureExposureMode = DsPressureExposureModeDefault;
	Config->SDF.DPadExposureMode = DsDPadExposureModeDefault;
	Config->SDF.ThumbSettings.DeadZoneLeft.Apply = TRUE;
	Config->SDF.ThumbSettings.DeadZoneLeft.PolarValue = 3.0;
	Config->SDF.ThumbSettings.DeadZoneRight.Apply = TRUE;
	Config->SDF.ThumbSettings.DeadZoneRight.PolarValue = 3.0;

	//
	// GPJ
	// 

	Config->GPJ.PressureExposureMode = DsPressureExposureModeDefault;
	Config->GPJ.DPadExposureMode = DsDPadExposureModeDefault;
	Config->GPJ.ThumbSettings.DeadZoneLeft.Apply = TRUE;
	Config->GPJ.ThumbSettings.DeadZoneLeft.PolarValue = 3.0;
	Config->GPJ.ThumbSettings.DeadZoneRight.Apply = TRUE;
	Config->GPJ.ThumbSettings.DeadZoneRight.PolarValue = 3.0;
}
