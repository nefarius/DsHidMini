#include "Driver.h"
#include "Configuration.tmh"


void
ConfigSetDefaults(
	_Inout_ PDS_DRIVER_CONFIGURATION Config
);

#pragma region Helpers

//
// Translates a friendly name string into the corresponding DS_HID_DEVICE_MODE value
// 
static DS_HID_DEVICE_MODE HID_DEVICE_MODE_FROM_NAME(PSTR ModeName)
{
	for (DS_HID_DEVICE_MODE value = 1; value < (DS_HID_DEVICE_MODE)_countof(G_HID_DEVICE_MODE_NAMES); value++)
	{
		if (strcmp(G_HID_DEVICE_MODE_NAMES[value], ModeName) == 0)
		{
			return value;
		}
	}

	return DsHidMiniDeviceModeUnknown;
}

//
// Translates a friendly name string into the corresponding DS_PRESSURE_EXPOSURE_MODE value
// 
static DS_PRESSURE_EXPOSURE_MODE DS_PRESSURE_EXPOSURE_MODE_FROM_NAME(PSTR ModeName)
{
	if (!_strcmpi(ModeName, G_PRESSURE_EXPOSURE_MODE_NAMES[2]))
	{
		return DsPressureExposureModeDefault;
	}

	if (!_strcmpi(ModeName, G_PRESSURE_EXPOSURE_MODE_NAMES[1]))
	{
		return DsPressureExposureModeAnalogue;
	}

	if (!_strcmpi(ModeName, G_PRESSURE_EXPOSURE_MODE_NAMES[0]))
	{
		return DsPressureExposureModeDigital;
	}

	return DsPressureExposureModeDefault;
}

//
// Translates a friendly name string into the corresponding DS_DPAD_EXPOSURE_MODE value
// 
static DS_DPAD_EXPOSURE_MODE DS_DPAD_EXPOSURE_MODE_FROM_NAME(PSTR ModeName)
{
	if (!_strcmpi(ModeName, G_DPAD_EXPOSURE_MODE_NAMES[2]))
	{
		return DsDPadExposureModeDefault;
	}

	if (!_strcmpi(ModeName, G_DPAD_EXPOSURE_MODE_NAMES[1]))
	{
		return DsDPadExposureModeIndividualButtons;
	}

	if (!_strcmpi(ModeName, G_DPAD_EXPOSURE_MODE_NAMES[0]))
	{
		return DsDPadExposureModeHAT;
	}

	return DsDPadExposureModeDefault;
}

//
// Translates a friendly name string into the corresponding DS_LED_MODE value
// 
static DS_LED_MODE DS_LED_MODE_FROM_NAME(PSTR ModeName)
{
	if (!_strcmpi(ModeName, G_LED_MODE_NAMES[2]))
	{
		return DsLEDModeCustomPattern;
	}

	if (!_strcmpi(ModeName, G_LED_MODE_NAMES[1]))
	{
		return DsLEDModeBatteryIndicatorBarGraph;
	}

	if (!_strcmpi(ModeName, G_LED_MODE_NAMES[0]))
	{
		return DsLEDModeBatteryIndicatorPlayerIndex;
	}

	return DsLEDModeBatteryIndicatorPlayerIndex;
}

//
// Translates a friendly name string into the corresponding DS_LED_AUTHORITY value
// 
static DS_LED_AUTHORITY DS_LED_AUTHORITY_FROM_NAME(PSTR AuthorityName)
{
	if (!_strcmpi(AuthorityName, G_DS_LED_AUTHORITY_NAMES[2]))
	{
		return DsLEDAuthorityApplication;
	}

	if (!_strcmpi(AuthorityName, G_DS_LED_AUTHORITY_NAMES[1]))
	{
		return DsLEDAuthorityDriver;
	}

	if (!_strcmpi(AuthorityName, G_DS_LED_AUTHORITY_NAMES[0]))
	{
		return DsLEDAuthorityAutomatic;
	}

	return DsLEDAuthorityAutomatic;
}

#pragma endregion

#pragma region Parsers

//
// Parses rumble settings
// 
#pragma warning(push)
#pragma warning( disable : 4706 )
static void
ConfigParseRumbleSettings(
	_In_ const cJSON* RumbleSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	cJSON* pNode = NULL;

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableBM")))
	{
		Config->RumbleSettings.DisableBM = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(RumbleSettings->string, "DisableBM", Config->RumbleSettings.DisableBM);
	}

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableSM")))
	{
		Config->RumbleSettings.DisableSM = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(RumbleSettings->string, "DisableSM", Config->RumbleSettings.DisableSM);
	}

	const cJSON* pBMStrRescale = cJSON_GetObjectItem(RumbleSettings, "BMStrRescale");

	if (pBMStrRescale)
	{
		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "Enabled")))
		{
			Config->RumbleSettings.BMStrRescale.Enabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "BMStrRescale.Enabled", Config->RumbleSettings.BMStrRescale.Enabled);
		}

		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "MinValue")))
		{
			Config->RumbleSettings.BMStrRescale.MinValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "BMStrRescale.MinValue", Config->RumbleSettings.BMStrRescale.MinValue);
		}

		if ((pNode = cJSON_GetObjectItem(pBMStrRescale, "MaxValue")))
		{
			Config->RumbleSettings.BMStrRescale.MaxValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "BMStrRescale.MaxValue", Config->RumbleSettings.BMStrRescale.MaxValue);
		}
	}

	const cJSON* pSMToBMConversion = cJSON_GetObjectItem(RumbleSettings, "SMToBMConversion");

	if (pSMToBMConversion)
	{
		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "Enabled")))
		{
			Config->RumbleSettings.SMToBMConversion.Enabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "SMToBMConversion.Enabled", Config->RumbleSettings.SMToBMConversion.Enabled);
		}

		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "RescaleMinValue")))
		{
			Config->RumbleSettings.SMToBMConversion.RescaleMinValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "SMToBMConversion.RescaleMinValue", Config->RumbleSettings.SMToBMConversion.RescaleMinValue);
		}

		if ((pNode = cJSON_GetObjectItem(pSMToBMConversion, "RescaleMaxValue")))
		{
			Config->RumbleSettings.SMToBMConversion.RescaleMaxValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "SMToBMConversion.RescaleMaxValue", Config->RumbleSettings.SMToBMConversion.RescaleMaxValue);
		}
	}

	const cJSON* pForcedSM = cJSON_GetObjectItem(RumbleSettings, "ForcedSM");

	if (pForcedSM)
	{
		if ((pNode = cJSON_GetObjectItem(pForcedSM, "BMThresholdEnabled")))
		{
			Config->RumbleSettings.ForcedSM.BMThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "ForcedSM.BMThresholdEnabled", Config->RumbleSettings.ForcedSM.BMThresholdEnabled);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "BMThresholdValue")))
		{
			Config->RumbleSettings.ForcedSM.BMThresholdValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "ForcedSM.BMThresholdValue", Config->RumbleSettings.ForcedSM.BMThresholdValue);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "SMThresholdEnabled")))
		{
			Config->RumbleSettings.ForcedSM.SMThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "ForcedSM.SMThresholdEnabled", Config->RumbleSettings.ForcedSM.SMThresholdEnabled);
		}

		if ((pNode = cJSON_GetObjectItem(pForcedSM, "SMThresholdValue")))
		{
			Config->RumbleSettings.ForcedSM.SMThresholdValue = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "ForcedSM.SMThresholdValue", Config->RumbleSettings.ForcedSM.SMThresholdValue);
		}
	}
}
#pragma warning(pop)

//
// Parses LED settings
// 
#pragma warning(push)
#pragma warning( disable : 4706 )
static void
ConfigParseLEDSettings(
	_In_ const cJSON* LEDSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	cJSON* pNode = NULL;

	if ((pNode = cJSON_GetObjectItem(LEDSettings, "Mode")))
	{
		Config->LEDSettings.Mode = DS_LED_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
		EventWriteOverrideSettingUInt(LEDSettings->string, "Mode", Config->LEDSettings.Mode);
	}

	if ((pNode = cJSON_GetObjectItem(LEDSettings, "Authority")))
	{
		Config->LEDSettings.Authority = DS_LED_AUTHORITY_FROM_NAME(cJSON_GetStringValue(pNode));
		EventWriteOverrideSettingUInt(LEDSettings->string, "Authority", Config->LEDSettings.Authority);
	}

	if (Config->LEDSettings.Mode == DsLEDModeCustomPattern)
	{
		const cJSON* pCustomPatterns = cJSON_GetObjectItem(LEDSettings, "CustomPatterns");

		if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "LEDFlags")))
		{
			Config->LEDSettings.CustomPatterns.LEDFlags = (UCHAR)cJSON_GetNumberValue(pNode);
		}

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
				EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "Duration", pPlayerSlots[playerIndex]->Duration);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalDuration")))
			{
				pPlayerSlots[playerIndex]->IntervalDuration = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "IntervalDuration", pPlayerSlots[playerIndex]->IntervalDuration);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "Enabled")))
			{
				pPlayerSlots[playerIndex]->EnabledFlags = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "Enabled", pPlayerSlots[playerIndex]->EnabledFlags);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalPortionOff")))
			{
				pPlayerSlots[playerIndex]->IntervalPortionOff = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "IntervalPortionOff", pPlayerSlots[playerIndex]->IntervalPortionOff);
			}

			if ((pNode = cJSON_GetObjectItem(pCustomPatterns, "IntervalPortionOn")))
			{
				pPlayerSlots[playerIndex]->IntervalPortionOn = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "IntervalPortionOn", pPlayerSlots[playerIndex]->IntervalPortionOn);
			}
		}
	}
}
#pragma warning(pop)

//
// Parses mode-specific properties
// 
#pragma warning(push)
#pragma warning( disable : 4706 )
static void
ConfigParseHidDeviceModeSpecificSettings(
	_In_ const cJSON* NodeSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	cJSON* pNode = NULL;

	//
	// SDF/GPJ-specific settings
	// 
	switch (Config->HidDeviceMode)
	{
		//
		// Properties only present in SDF mode
		// 
	case DsHidMiniDeviceModeSDF:
		if ((pNode = cJSON_GetObjectItem(NodeSettings, "PressureExposureMode")))
		{
			Config->SDF.PressureExposureMode = DS_PRESSURE_EXPOSURE_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
			EventWriteOverrideSettingUInt(NodeSettings->string, "SDF.PressureExposureMode", Config->SDF.PressureExposureMode);
		}

		if ((pNode = cJSON_GetObjectItem(NodeSettings, "DPadExposureMode")))
		{
			Config->SDF.DPadExposureMode = DS_DPAD_EXPOSURE_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
			EventWriteOverrideSettingUInt(NodeSettings->string, "SDF.DPadExposureMode", Config->SDF.DPadExposureMode);
		}
		break;
		//
		// Properties only present in GPJ mode
		// 
	case DsHidMiniDeviceModeGPJ:
		if ((pNode = cJSON_GetObjectItem(NodeSettings, "PressureExposureMode")))
		{
			Config->GPJ.PressureExposureMode = DS_PRESSURE_EXPOSURE_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
			EventWriteOverrideSettingUInt(NodeSettings->string, "GPJ.PressureExposureMode", Config->GPJ.PressureExposureMode);
		}

		if ((pNode = cJSON_GetObjectItem(NodeSettings, "DPadExposureMode")))
		{
			Config->GPJ.DPadExposureMode = DS_DPAD_EXPOSURE_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
			EventWriteOverrideSettingUInt(NodeSettings->string, "GPJ.DPadExposureMode", Config->GPJ.DPadExposureMode);
		}
		break;
	}
}
#pragma warning(pop)

//
// Reads/refreshes configuration from disk (JSON) to provided context
// 
#pragma warning(push)
#pragma warning( disable : 4706 )
static void ConfigNodeParse(
	_In_ const cJSON* ParentNode,
	_Inout_ PDEVICE_CONTEXT Context,
	_In_opt_ BOOLEAN IsHotReload
)
{
	const PDS_DRIVER_CONFIGURATION pCfg = &Context->Configuration;
	cJSON* pNode = NULL;

    if (IsHotReload)
    {
        //
        // Reset device's idle disconnect timer
        //
        Context->Connection.Bth.IdleDisconnectTimestamp.QuadPart = 0;
    }

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
			pCfg->HidDeviceMode = HID_DEVICE_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
			EventWriteOverrideSettingUInt(ParentNode->string, "HidDeviceMode", pCfg->HidDeviceMode);
		}

		if ((pNode = cJSON_GetObjectItem(ParentNode, "DisableAutoPairing")))
		{
			pCfg->DisableAutoPairing = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(ParentNode->string, "DisableAutoPairing", pCfg->DisableAutoPairing);
		}
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputRateControlEnabled")))
	{
		pCfg->IsOutputRateControlEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "IsOutputRateControlEnabled", pCfg->IsOutputRateControlEnabled);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "OutputRateControlPeriodMs")))
	{
		pCfg->OutputRateControlPeriodMs = (UCHAR)cJSON_GetNumberValue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "OutputRateControlPeriodMs", pCfg->OutputRateControlPeriodMs);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "IsOutputDeduplicatorEnabled")))
	{
		pCfg->IsOutputDeduplicatorEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "IsOutputDeduplicatorEnabled", pCfg->IsOutputDeduplicatorEnabled);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "WirelessIdleTimeoutPeriodMs")))
	{
		pCfg->WirelessIdleTimeoutPeriodMs = (ULONG)cJSON_GetNumberValue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "WirelessIdleTimeoutPeriodMs", pCfg->WirelessIdleTimeoutPeriodMs);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "DisableWirelessIdleTimeout")))
	{
		pCfg->DisableWirelessIdleTimeout = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "DisableWirelessIdleTimeout", pCfg->DisableWirelessIdleTimeout);
	}

    //
    // Wireless quick disconnect combo
    // 
    const cJSON* pDisconnectCombo = cJSON_GetObjectItem(ParentNode, "QuickDisconnectCombo");

    if (pDisconnectCombo)
    {
        if ((pNode = cJSON_GetObjectItem(pDisconnectCombo, "IsEnabled")))
        {
            pCfg->WirelessDisconnectButtonCombo.IsEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
            EventWriteOverrideSettingUInt(pDisconnectCombo->string, "WirelessDisconnectButtonCombo.IsEnabled", pCfg->WirelessDisconnectButtonCombo.IsEnabled);
        }

        if ((pNode = cJSON_GetObjectItem(pDisconnectCombo, "HoldTime")))
        {
            pCfg->WirelessDisconnectButtonCombo.HoldTime = (ULONG)cJSON_GetNumberValue(pNode);
            EventWriteOverrideSettingUInt(pDisconnectCombo->string, "WirelessDisconnectButtonCombo.HoldTime", pCfg->WirelessDisconnectButtonCombo.HoldTime);
        }

        const PSTR comboButtonsNames[] =
        {
            "Button1",
            "Button2",
            "Button3",
        };

        for (ULONGLONG buttonIndex = 0; buttonIndex < _countof(pCfg->WirelessDisconnectButtonCombo.Buttons); buttonIndex++)
        {
            if ((pNode = cJSON_GetObjectItem(pDisconnectCombo, comboButtonsNames[buttonIndex])))
            {
                pCfg->WirelessDisconnectButtonCombo.Buttons[buttonIndex] = (UCHAR)cJSON_GetNumberValue(pNode);
                EventWriteOverrideSettingUInt(pDisconnectCombo->string, comboButtonsNames[buttonIndex], pCfg->WirelessDisconnectButtonCombo.Buttons[buttonIndex]);
            }
        }
    }

	//
	// Every mode can have the same properties configured independently
	// 
	if (pCfg->HidDeviceMode > 0 && pCfg->HidDeviceMode < (DS_HID_DEVICE_MODE)_countof(G_HID_DEVICE_MODE_NAMES))
	{
		const cJSON* pModeSpecific = cJSON_GetObjectItem(ParentNode, G_HID_DEVICE_MODE_NAMES[pCfg->HidDeviceMode]);

		if (pModeSpecific)
		{
			//
			// SDF/GPJ-specific settings
			// 
			ConfigParseHidDeviceModeSpecificSettings(pModeSpecific, pCfg);

			//
			// DeadZone Left
			// 
			const cJSON* pDeadZoneLeft = cJSON_GetObjectItem(pModeSpecific, "DeadZoneLeft");

			if (pDeadZoneLeft)
			{
				if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "Apply")))
				{
					pCfg->ThumbSettings.DeadZoneLeft.Apply = (BOOLEAN)cJSON_IsTrue(pNode);
					EventWriteOverrideSettingUInt(pDeadZoneLeft->string, "ThumbSettings.DeadZoneLeft.Apply", pCfg->ThumbSettings.DeadZoneLeft.Apply);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneLeft.PolarValue = cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingDouble(pDeadZoneLeft->string, "ThumbSettings.DeadZoneLeft.PolarValue", pCfg->ThumbSettings.DeadZoneLeft.PolarValue);
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
					EventWriteOverrideSettingUInt(pDeadZoneRight->string, "ThumbSettings.DeadZoneRight.Apply", pCfg->ThumbSettings.DeadZoneRight.Apply);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneRight.PolarValue = cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingDouble(pDeadZoneRight->string, "ThumbSettings.DeadZoneRight.PolarValue", pCfg->ThumbSettings.DeadZoneRight.PolarValue);
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

			//
			// Flip Axis settings
			// 
			const cJSON* pFlipAxis = cJSON_GetObjectItem(pModeSpecific, "FlipAxis");

			if (pFlipAxis)
			{
				if ((pNode = cJSON_GetObjectItem(pFlipAxis, "LeftX")))
				{
					pCfg->FlipAxis.LeftX = (BOOLEAN)cJSON_IsTrue(pNode);
				}

				if ((pNode = cJSON_GetObjectItem(pFlipAxis, "LeftY")))
				{
					pCfg->FlipAxis.LeftY = (BOOLEAN)cJSON_IsTrue(pNode);
				}

				if ((pNode = cJSON_GetObjectItem(pFlipAxis, "RightX")))
				{
					pCfg->FlipAxis.RightX = (BOOLEAN)cJSON_IsTrue(pNode);
				}

				if ((pNode = cJSON_GetObjectItem(pFlipAxis, "RightY")))
				{
					pCfg->FlipAxis.RightY = (BOOLEAN)cJSON_IsTrue(pNode);
				}
			}
		}
	}
}
#pragma warning(pop)

#pragma endregion

//
// Load/refresh device-specific overrides
// 
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
	HANDLE hFile = NULL;
	PCHAR content = NULL;
	cJSON* config_json = NULL;
	LARGE_INTEGER size = { 0 };

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

		//
		// Protection against nonsense
		// 
		if (size.QuadPart > 20000000 /* 20 MB of JSON, w00t?! */)
		{
            TraceError(
				TRACE_CONFIG,
				"Configuration file too big to parse, reported size: %I64d",
				size.QuadPart
			);
			EventWriteFailedWithWin32Error(__FUNCTION__, L"Reading configuration file", ERROR_BUFFER_OVERFLOW);
			status = STATUS_BUFFER_OVERFLOW;
			break;
		}

		content = (char*)calloc((size_t)size.QuadPart, sizeof(char));

		if (content == NULL)
		{
			status = STATUS_NO_MEMORY;
			break;
		}

		DWORD bytesRead = 0;

		if (!ReadFile(hFile, content, (DWORD)size.QuadPart, &bytesRead, NULL))
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

		config_json = cJSON_ParseWithLength(content, size.QuadPart);

		if (config_json == NULL)
		{
            TraceError(
                TRACE_CONFIG,
                "JSON parsing failed"
            );

			const char* error_ptr = cJSON_GetErrorPtr();
			if (error_ptr != NULL)
			{
				TraceError(
					TRACE_CONFIG,
					"JSON parsing error: %s",
					error_ptr
				);
                EventWriteJSONParseError(error_ptr);
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
		const cJSON* deviceNode = cJSON_GetObjectItem(devicesNode, Context->DeviceAddressString);

		if (deviceNode)
		{
			TraceVerbose(
				TRACE_CONFIG,
				"Found device-specific (%s) config, loading",
				Context->DeviceAddressString
			);

			EventWriteLoadingDeviceSpecificConfig(Context->DeviceAddressString);

			ConfigNodeParse(deviceNode, Context, IsHotReload);
		}
        else
        {
            TraceVerbose(
                TRACE_CONFIG,
                "Device-specific (%s) config not found",
                Context->DeviceAddressString
            );
        }

	} while (FALSE);

	//
	// Verify if SMtoBMConversion values are valid and attempt to calculate rescaling constants in case they are
	// 
	if (
		Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue > Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue
		&& Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue > 0
		)
	{
		Context->Configuration.RumbleSettings.SMToBMConversion.ConstA =
			(DOUBLE)(Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue - Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMinValue) / (254);

		Context->Configuration.RumbleSettings.SMToBMConversion.ConstB =
			Context->Configuration.RumbleSettings.SMToBMConversion.RescaleMaxValue - Context->Configuration.RumbleSettings.SMToBMConversion.ConstA * 255;

		TraceVerbose(
			TRACE_CONFIG,
			"SMToBMConversion rescaling constants: A = %f and B = %f.",
			Context->Configuration.RumbleSettings.SMToBMConversion.ConstA,
			Context->Configuration.RumbleSettings.SMToBMConversion.ConstB
		);

	}
	else
	{
		TraceVerbose(
			TRACE_CONFIG,
			"Invalid values found for SMToBMConversion. Setting disabled."
		);
		Context->Configuration.RumbleSettings.SMToBMConversion.Enabled = FALSE;
	}

	//
	// Verify if BMStrRescale values are valid and attempt to calculate rescaling constants in case they are
	// 
	if (
		Context->Configuration.RumbleSettings.BMStrRescale.MaxValue > Context->Configuration.RumbleSettings.BMStrRescale.MinValue
		&& Context->Configuration.RumbleSettings.BMStrRescale.MinValue > 0
		)
	{
		Context->Configuration.RumbleSettings.BMStrRescale.ConstA =
			(DOUBLE)(Context->Configuration.RumbleSettings.BMStrRescale.MaxValue - Context->Configuration.RumbleSettings.BMStrRescale.MinValue) / (254);

		Context->Configuration.RumbleSettings.BMStrRescale.ConstB =
			Context->Configuration.RumbleSettings.BMStrRescale.MaxValue - Context->Configuration.RumbleSettings.BMStrRescale.ConstA * 255;

		TraceVerbose(
			TRACE_CONFIG,
			"BMStrRescale rescaling constants: A = %f and B = %f.",
			Context->Configuration.RumbleSettings.BMStrRescale.ConstA,
			Context->Configuration.RumbleSettings.BMStrRescale.ConstB
		);
	}
	else
	{
		TraceVerbose(
			TRACE_CONFIG,
			"Invalid values found for BMStrRescale. Setting disabled."
		);

		Context->Configuration.RumbleSettings.BMStrRescale.Enabled = FALSE;
	}

	if (config_json)
	{
		cJSON_Delete(config_json);
	}

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
	Config->DisableWirelessIdleTimeout = FALSE;

    Config->WirelessDisconnectButtonCombo.IsEnabled = TRUE;
    Config->WirelessDisconnectButtonCombo.HoldTime = 1000;
    Config->WirelessDisconnectButtonCombo.Buttons[0] = 10;
    Config->WirelessDisconnectButtonCombo.Buttons[1] = 11;
    Config->WirelessDisconnectButtonCombo.Buttons[2] = 16;

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
	Config->LEDSettings.CustomPatterns.LEDFlags = 0x02;

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
		pPlayerSlots[playerIndex]->EnabledFlags = 0x10;
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
