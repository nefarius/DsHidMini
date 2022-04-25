#include "Driver.h"
#include "Configuration.tmh"


void
ConfigParseRumbleSettings(
	_In_ const cJSON* RumbleSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	cJSON* pNode = NULL;

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableBM")))
	{
		Config->RumbleSettings.DisableBM = (BOOLEAN)cJSON_IsTrue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableSM")))
	{
		Config->RumbleSettings.DisableSM = (BOOLEAN)cJSON_IsTrue(pNode);
	}

	const cJSON* pBMStrRescale = cJSON_GetObjectItem(RumbleSettings, "BMStrRescale");

	if (pBMStrRescale)
	{
		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "Enabled")))
		{
			Config->RumbleSettings.BMStrRescale.Enabled = (BOOLEAN)cJSON_IsTrue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "MinValue")))
		{
			Config->RumbleSettings.BMStrRescale.MinValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "MaxValue")))
		{
			Config->RumbleSettings.BMStrRescale.MaxValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}
	}

	const cJSON* pSMToBMConversion = cJSON_GetObjectItem(RumbleSettings, "SMToBMConversion");

	if (pSMToBMConversion)
	{
		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "Enabled")))
		{
			Config->RumbleSettings.SMToBMConversion.Enabled = (BOOLEAN)cJSON_IsTrue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "RescaleMinValue")))
		{
			Config->RumbleSettings.SMToBMConversion.RescaleMinValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "RescaleMaxValue")))
		{
			Config->RumbleSettings.SMToBMConversion.RescaleMaxValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}
	}

	const cJSON* pForcedSM = cJSON_GetObjectItem(RumbleSettings, "ForcedSM");

	if (pForcedSM)
	{
		if ((pNode = cJSON_GetObjectItem(pForcedSM, "BMThresholdEnabled")))
		{
			Config->RumbleSettings.ForcedSM.BMThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "BMThresholdValue")))
		{
			Config->RumbleSettings.ForcedSM.BMThresholdValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "SMThresholdEnabled")))
		{
			Config->RumbleSettings.ForcedSM.SMThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "SMThresholdValue")))
		{
			Config->RumbleSettings.ForcedSM.SMThresholdValue = (UCHAR)cJSON_GetNumberValue(pNode);
		}
	}
}

void
ConfigParseLEDSettings(
	_In_ const cJSON* LEDSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	cJSON* pNode = NULL;

	if ((pNode = cJSON_GetObjectItem(LEDSettings, "Mode")))
	{
		Config->LEDSettings.Mode = (DS_LED_MODE)cJSON_GetNumberValue(pNode);
	}

	if (Config->LEDSettings.Mode == DsLEDModeCustomPattern)
	{
		const cJSON* pCustomPatterns = cJSON_GetObjectItem(LEDSettings, "CustomPatterns");

		const PSTR playerSlotNames[] =
		{
			"Player1",
			"Player2",
			"Player3",
			"Player4",
		};

		const PDS_LED pPlayerSlots[] =
		{
			&Config->LEDSettings.CustomPatterns.Player1,
			&Config->LEDSettings.CustomPatterns.Player2,
			&Config->LEDSettings.CustomPatterns.Player3,
			&Config->LEDSettings.CustomPatterns.Player4,
		};

		for (ULONGLONG playerIndex = 0; playerIndex < _countof(playerSlotNames); playerIndex++)
		{
			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "Duration")))
			{
				pPlayerSlots[playerIndex]->Duration = (UCHAR)cJSON_GetNumberValue(pNode);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalDuration")))
			{
				pPlayerSlots[playerIndex]->IntervalDuration = (UCHAR)cJSON_GetNumberValue(pNode);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "Enabled")))
			{
				pPlayerSlots[playerIndex]->Enabled = (UCHAR)cJSON_GetNumberValue(pNode);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalPortionOff")))
			{
				pPlayerSlots[playerIndex]->IntervalPortionOff = (UCHAR)cJSON_GetNumberValue(pNode);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalPortionOn")))
			{
				pPlayerSlots[playerIndex]->IntervalPortionOn = (UCHAR)cJSON_GetNumberValue(pNode);
			}
		}
	}
}

//
// Reads/refreshes configuration from disk (JSON) to provided context
// 
void ConfigNodeParse(
	_In_ const cJSON* ParentNode,
	_Inout_ PDEVICE_CONTEXT Context,
	_In_opt_ BOOLEAN IsHotReload
)
{
	PDS_DRIVER_CONFIGURATION pCfg = &Context->Configuration;
	cJSON* pNode = NULL;

	//
	// Common
	// 

	if (!IsHotReload)
	{
		//
		// These values must not be altered during runtime
		// 

		if ((pNode = cJSON_GetObjectItem(ParentNode, "HidDeviceMode")))
		{
			pCfg->HidDeviceMode = (DS_HID_DEVICE_MODE)cJSON_GetNumberValue(pNode);
		}

		if ((pNode = cJSON_GetObjectItem(ParentNode, "DisableAutoPairing")))
		{
			pCfg->DisableAutoPairing = (BOOLEAN)cJSON_IsTrue(pNode);
		}
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputRateControlEnabled")))
	{
		pCfg->IsOutputRateControlEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "OutputRateControlPeriodMs")))
	{
		pCfg->OutputRateControlPeriodMs = (UCHAR)cJSON_GetNumberValue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputDeduplicatorEnabled")))
	{
		pCfg->IsOutputDeduplicatorEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "WirelessIdleTimeoutPeriodMs")))
	{
		pCfg->WirelessIdleTimeoutPeriodMs = (ULONG)cJSON_GetNumberValue(pNode);
	}

	//
	// TODO: this is getting cluttered, split up into some sub-routines
	// 

	//
	// Every mode can have the same properties configured independently
	// 
	if (pCfg->HidDeviceMode > 0 && pCfg->HidDeviceMode <= (DS_HID_DEVICE_MODE)_countof(G_HID_DEVICE_MODE_NAMES))
	{
		const cJSON* pModeSpecific = cJSON_GetObjectItem(ParentNode, G_HID_DEVICE_MODE_NAMES[pCfg->HidDeviceMode]);

		if (pModeSpecific)
		{
			if ((pNode = cJSON_GetObjectItem(pModeSpecific, "PressureExposureMode")))
			{
				pCfg->SDF.PressureExposureMode = (DS_PRESSURE_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
			}

			if ((pNode = cJSON_GetObjectItem(pModeSpecific, "DPadExposureMode")))
			{
				pCfg->SDF.DPadExposureMode = (DS_DPAD_EXPOSURE_MODE)cJSON_GetNumberValue(pNode);
			}

			//
			// DeadZone Left
			// 
			const cJSON* pDeadZoneLeft = cJSON_GetObjectItem(pModeSpecific, "DeadZoneLeft");

			if (pDeadZoneLeft)
			{
				if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "Apply")))
				{
					pCfg->ThumbSettings.DeadZoneLeft.Apply = (BOOLEAN)cJSON_IsTrue(pNode);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneLeft.PolarValue = cJSON_GetNumberValue(pNode);
				}
			}

			//
			// DeadZone Right
			// 
			const cJSON* pDeadZoneRight = cJSON_GetObjectItem(pModeSpecific, "DeadZoneRight");

			if (pDeadZoneRight)
			{
				if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "Apply")))
				{
					pCfg->ThumbSettings.DeadZoneRight.Apply = (BOOLEAN)cJSON_IsTrue(pNode);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneRight.PolarValue = cJSON_GetNumberValue(pNode);
				}
			}

			//
			// Rumble settings
			// 
			const cJSON* pRumbleSettings = cJSON_GetObjectItem(pModeSpecific, "RumbleSettings");

			if (pRumbleSettings)
			{
				ConfigParseRumbleSettings(pRumbleSettings, pCfg);
			}

			//
			// LED settings
			// 
			const cJSON* pLEDSettings = cJSON_GetObjectItem(pModeSpecific, "LEDSettings");

			if (pLEDSettings)
			{
				ConfigParseLEDSettings(pLEDSettings, pCfg);
			}
		}
	}
}

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
	FILE* fp = NULL;
	PCHAR content = NULL;
	cJSON* config_json = NULL;

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

		TraceVerbose(
			TRACE_CONFIG,
			"Set config file path to %s",
			configFilePath
		);

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

		TraceVerbose(
			TRACE_CONFIG,
			"File size in bytes: %d",
			numBytes
		);

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

		//
		// Read global configuration first, then overwrite device-specific ones
		// 
		const cJSON* globalNode = cJSON_GetObjectItem(config_json, "Global");

		if (globalNode)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Loading global configuration"
			);

			ConfigNodeParse(globalNode, Context, IsHotReload);
		}

		const cJSON* devicesNode = cJSON_GetObjectItem(config_json, "Devices");

		if (!devicesNode)
		{
			break;
		}

		//
		// Try to read device-specific properties
		// 
		cJSON* deviceNode = cJSON_GetObjectItem(devicesNode, Context->DeviceAddressString);

		if (deviceNode)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Found device-specific (%s) config, loading",
				Context->DeviceAddressString
			);

			ConfigNodeParse(deviceNode, Context, IsHotReload);
		}

		//
		// Verify if SMtoBMConversion values are valid and attempt to calculate rescaling constants in case they are
		// 
		if(
			Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue > Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue
			&& Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue > 0
		)
		{
			Context->Configuration.RumbleSettings.SMToBMConversion.ConstA =
				(DOUBLE)(Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue - Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue) / (254);

			Context->Configuration.RumbleSettings.SMToBMConversion.ConstB =
			Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue - Context->Configuration.RumbleSettings.SMToBMConversion.ConstA * 255;
			
		}
		else
		{
			Context->Configuration.RumbleSettings.SMToBMConversion.Enabled = FALSE ;
		}

		//
		// Verify if BMStrRescale values are valid and attempt to calculate rescaling constants in case they are
		// 
		if(
			Context->Configuration.RumbleSettings.BMStrRescale.MaxValue > Context->Configuration.RumbleSettings.BMStrRescale.MinValue
			&& Context->Configuration.RumbleSettings.BMStrRescale.MinValue > 0
		)
		{
			Context->Configuration.RumbleSettings.BMStrRescale.ConstA =
				(DOUBLE)(Context->Configuration.RumbleSettings.BMStrRescale.MaxValue - Context->Configuration.RumbleSettings.BMStrRescale.MinValue) / (254);

			Context->Configuration.RumbleSettings.BMStrRescale.ConstB =
				Context->Configuration.RumbleSettings.BMStrRescale.MaxValue - Context->Configuration.RumbleSettings.BMStrRescale.ConstA * 255;		
		
		}
		else
		{
		Context->Configuration.RumbleSettings.BMStrRescale.Enabled = FALSE;
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

	Config->ThumbSettings.DeadZoneLeft.Apply = TRUE;
	Config->ThumbSettings.DeadZoneLeft.PolarValue = 3.0;
	Config->ThumbSettings.DeadZoneRight.Apply = TRUE;
	Config->ThumbSettings.DeadZoneRight.PolarValue = 3.0;

	Config->RumbleSettings.DisableBM = FALSE;
	Config->RumbleSettings.DisableSM = FALSE;
	Config->RumbleSettings.BMStrRescale.Enabled = TRUE;
	Config->RumbleSettings.BMStrRescale.MinValue = 64;
	Config->RumbleSettings.BMStrRescale.MaxValue = 255;
	Config->RumbleSettings.SMToBMConversion.Enabled = FALSE;
	Config->RumbleSettings.SMToBMConversion.RescaleMinValue = 1;
	Config->RumbleSettings.SMToBMConversion.RescaleMaxValue = 140;
	Config->RumbleSettings.ForcedSM.BMThresholdEnabled = TRUE;
	Config->RumbleSettings.ForcedSM.BMThresholdValue = 230;
	Config->RumbleSettings.ForcedSM.SMThresholdEnabled = FALSE;
	Config->RumbleSettings.ForcedSM.SMThresholdValue = 230;

	Config->LEDSettings.Mode = DsLEDModeBatteryIndicatorPlayerIndex;

	const PDS_LED pPlayerSlots[] =
	{
		&Config->LEDSettings.CustomPatterns.Player1,
		&Config->LEDSettings.CustomPatterns.Player2,
		&Config->LEDSettings.CustomPatterns.Player3,
		&Config->LEDSettings.CustomPatterns.Player4,
	};

	for (ULONGLONG playerIndex = 0; playerIndex < _countof(pPlayerSlots); playerIndex++)
	{
		pPlayerSlots[playerIndex]->Duration = 0xFF;
		pPlayerSlots[playerIndex]->IntervalDuration = 0xFF;
		pPlayerSlots[playerIndex]->Enabled = 0x10;
		pPlayerSlots[playerIndex]->IntervalPortionOff = 0x00;
		pPlayerSlots[playerIndex]->IntervalPortionOn = 0xFF;
	}

	//
	// SDF-specific
	// 

	Config->SDF.PressureExposureMode = DsPressureExposureModeDefault;
	Config->SDF.DPadExposureMode = DsDPadExposureModeDefault;

	//
	// GPJ-specific
	// 

	Config->GPJ.PressureExposureMode = DsPressureExposureModeDefault;
	Config->GPJ.DPadExposureMode = DsDPadExposureModeDefault;
}
