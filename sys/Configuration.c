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
// Translates a friendly name string into the corresponding DS_DEVICE_PAIRING_MODE value
// 
static DS_DEVICE_PAIRING_MODE DS_DEVICE_PAIRING_MODE_FROM_NAME(PSTR ModeName)
{
	if (!_strcmpi(ModeName, G_DEVICE_PAIRING_MODE_NAMES[2]))
	{
		return DsDevicePairingModeDisabled;
	}

	if (!_strcmpi(ModeName, G_DEVICE_PAIRING_MODE_NAMES[1]))
	{
		return DsDevicePairingModeCustom;
	}

	if (!_strcmpi(ModeName, G_DEVICE_PAIRING_MODE_NAMES[0]))
	{
		return DsDevicePairingModeAuto;
	}

	return DsDevicePairingModeDisabled;
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

//
// Parse button combo settings
// 
#pragma warning(push)
#pragma warning( disable : 4706 )
static void
ConfigParseButtonComboSettings(
	_In_ const cJSON* ComboSettings,
	_Inout_ PDS_BUTTON_COMBO Combo
)
{
	cJSON* pNode = NULL;

	if ((pNode = cJSON_GetObjectItem(ComboSettings, "IsEnabled")))
	{
		Combo->IsEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(ComboSettings->string, "IsEnabled",
			Combo->IsEnabled);
	}

	if ((pNode = cJSON_GetObjectItem(ComboSettings, "HoldTime")))
	{
		Combo->HoldTime = (ULONG)cJSON_GetNumberValue(pNode);
		EventWriteOverrideSettingUInt(ComboSettings->string, "HoldTime",
			Combo->HoldTime);
	}

	for (ULONGLONG buttonIndex = 0; buttonIndex < _countof(Combo->Buttons); buttonIndex++)
	{
		if ((pNode = cJSON_GetObjectItem(ComboSettings, G_DS_BUTTON_COMBO_NAMES[buttonIndex])))
		{
			const UCHAR offset = (UCHAR)cJSON_GetNumberValue(pNode);
			if (offset <= DS_BUTTON_COMBO_MAX_OFFSET)
			{
				Combo->Buttons[buttonIndex] = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(ComboSettings->string, G_DS_BUTTON_COMBO_NAMES[buttonIndex],
					Combo->Buttons[buttonIndex]);
			}
			else
			{
				TraceError(
					TRACE_CONFIG,
					"Provided button offset %d for %s out of range, ignoring",
					offset,
					G_DS_BUTTON_COMBO_NAMES[buttonIndex]
				);
			}
		}
	}
}
#pragma warning(pop)

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

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableLeft")))
	{
		Config->RumbleSettings.DisableLeft = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.DisableLeft", Config->RumbleSettings.DisableLeft);
	}

	if ((pNode = cJSON_GetObjectItem(RumbleSettings, "DisableRight")))
	{
		Config->RumbleSettings.DisableRight = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.DisableRight", Config->RumbleSettings.DisableRight);
	}

	const cJSON* pHeavyRescale = cJSON_GetObjectItem(RumbleSettings, "HeavyRescale");

	if (pHeavyRescale)
	{
		if ((pNode = cJSON_GetObjectItem(pHeavyRescale, "IsEnabled")))
		{
			Config->RumbleSettings.HeavyRescaling.IsEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.IsEnabled", Config->RumbleSettings.HeavyRescaling.IsEnabled);
		}

		if ((pNode = cJSON_GetObjectItem(pHeavyRescale, "RescaleMinRange")))
		{
			Config->RumbleSettings.HeavyRescaling.MinRange = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.MinRange", Config->RumbleSettings.HeavyRescaling.MinRange);
		}

		if ((pNode = cJSON_GetObjectItem(pHeavyRescale, "RescaleMaxRange")))
		{
			Config->RumbleSettings.HeavyRescaling.MaxRange = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.MaxRange", Config->RumbleSettings.HeavyRescaling.MaxRange);
		}
	}

	const cJSON* pAlternativeMode = cJSON_GetObjectItem(RumbleSettings, "AlternativeMode");

	if (pAlternativeMode)
	{
		if ((pNode = cJSON_GetObjectItem(pAlternativeMode, "IsEnabled")))
		{
			Config->RumbleSettings.AlternativeMode.IsEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.IsEnabled", Config->RumbleSettings.AlternativeMode.IsEnabled);
		}

		if ((pNode = cJSON_GetObjectItem(pAlternativeMode, "RescaleMinRange")))
		{
			Config->RumbleSettings.AlternativeMode.MinRange = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.MinRange", Config->RumbleSettings.AlternativeMode.MinRange);
		}

		if ((pNode = cJSON_GetObjectItem(pAlternativeMode, "RescaleMaxRange")))
		{
			Config->RumbleSettings.AlternativeMode.MaxRange = (UCHAR)cJSON_GetNumberValue(pNode);
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.MaxRange", Config->RumbleSettings.AlternativeMode.MaxRange);
		}

		if ((pNode = cJSON_GetObjectItem(pAlternativeMode, "ToggleCombo")))
		{
			ConfigParseButtonComboSettings(pNode, &Config->RumbleSettings.AlternativeMode.ToggleButtonCombo);
		}

		const cJSON* pForced = cJSON_GetObjectItem(pAlternativeMode, "ForcedRight");

		if (pForced)
		{
			if ((pNode = cJSON_GetObjectItem(pForced, "IsHeavyThresholdEnabled")))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled", Config->RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled);
			}

			if ((pNode = cJSON_GetObjectItem(pForced, "IsLightThresholdEnabled")))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled = (BOOLEAN)cJSON_IsTrue(pNode);
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled", Config->RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled);
			}

			if ((pNode = cJSON_GetObjectItem(pForced, "HeavyThreshold")))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold", Config->RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold);
			}

			if ((pNode = cJSON_GetObjectItem(pForced, "LightThreshold")))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.LightThreshold = (UCHAR)cJSON_GetNumberValue(pNode);
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.LightThreshold", Config->RumbleSettings.AlternativeMode.ForcedRight.LightThreshold);
			}
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

		cJSON* pPlayer = NULL;
		for (ULONGLONG playerIndex = 0; playerIndex < _countof(playerSlotNames); playerIndex++)
		{
			if (pPlayer = cJSON_GetObjectItem(pCustomPatterns, playerSlotNames[playerIndex]))
			{
				if ((pNode = cJSON_GetObjectItem(pPlayer, "TotalDuration")))
				{
					pPlayerSlots[playerIndex]->TotalDuration = (UCHAR)cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "TotalDuration",
						pPlayerSlots[playerIndex]->TotalDuration);
				}

				if ((pNode = cJSON_GetObjectItem(pPlayer, "BasePortionDuration")))
				{
					pPlayerSlots[playerIndex]->BasePortionDuration = (USHORT)cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "BasePortionDuration",
						pPlayerSlots[playerIndex]->BasePortionDuration);
				}

				if ((pNode = cJSON_GetObjectItem(pPlayer, "OffPortionMultiplier")))
				{
					pPlayerSlots[playerIndex]->OffPortionMultiplier = (UCHAR)cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "OffPortionMultiplier",
						pPlayerSlots[playerIndex]->OffPortionMultiplier);
				}

				if ((pNode = cJSON_GetObjectItem(pPlayer, "OnPortionMultiplier")))
				{
					pPlayerSlots[playerIndex]->OnPortionMultiplier = (UCHAR)cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "OnPortionMultiplier",
						pPlayerSlots[playerIndex]->OnPortionMultiplier);
				}
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
	FuncEntry(TRACE_CONFIG);

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

	if ((pNode = cJSON_GetObjectItem(ParentNode, "DevicePairingMode")))
	{
		pCfg->DevicePairingMode = DS_DEVICE_PAIRING_MODE_FROM_NAME(cJSON_GetStringValue(pNode));
		EventWriteOverrideSettingUInt(ParentNode->string, "DevicePairingMode", pCfg->DevicePairingMode);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "PairOnHotReload")))
	{
		pCfg->PairOnHotReload = (BOOLEAN)cJSON_IsTrue(pNode);
		EventWriteOverrideSettingUInt(ParentNode->string, "PairOnHotReload", pCfg->PairOnHotReload);
	}

	if ((pNode = cJSON_GetObjectItem(ParentNode, "CustomPairingAddress")))
	{
		char* eptr; // not used
		long long addressAsNumber = strtoll(cJSON_GetStringValue(pNode), &eptr, 16);
		for (int i = 0; i < 6; i++)
		{

			pCfg->CustomHostAddress[5 - i] = (UCHAR)((addressAsNumber >> (8 * i)) & 0xFF);
		}
		TraceVerbose(
			TRACE_DS3,
			"Configuration custom address: %02X:%02X:%02X:%02X:%02X:%02X",
			pCfg->CustomHostAddress[0],
			pCfg->CustomHostAddress[1],
			pCfg->CustomHostAddress[2],
			pCfg->CustomHostAddress[3],
			pCfg->CustomHostAddress[4],
			pCfg->CustomHostAddress[5]
		);
		//EventWriteOverrideSettingUInt(ParentNode->string, "CustomPairingAddress", pCfg->DevicePairingMode);
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
	if ((pNode = cJSON_GetObjectItem(ParentNode, "QuickDisconnectCombo")))
	{
		ConfigParseButtonComboSettings(pNode, &pCfg->WirelessDisconnectButtonCombo);
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
					EventWriteOverrideSettingUInt(pDeadZoneLeft->string, "ThumbSettings.DeadZoneLeft.Apply",
						pCfg->ThumbSettings.DeadZoneLeft.Apply);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneLeft, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneLeft.PolarValue = cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingDouble(pDeadZoneLeft->string, "ThumbSettings.DeadZoneLeft.PolarValue",
						pCfg->ThumbSettings.DeadZoneLeft.PolarValue);
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
					EventWriteOverrideSettingUInt(pDeadZoneRight->string, "ThumbSettings.DeadZoneRight.Apply",
						pCfg->ThumbSettings.DeadZoneRight.Apply);
				}

				if ((pNode = cJSON_GetObjectItem(pDeadZoneRight, "PolarValue")))
				{
					pCfg->ThumbSettings.DeadZoneRight.PolarValue = cJSON_GetNumberValue(pNode);
					EventWriteOverrideSettingDouble(pDeadZoneRight->string, "ThumbSettings.DeadZoneRight.PolarValue",
						pCfg->ThumbSettings.DeadZoneRight.PolarValue);
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

	FuncExitNoReturn(TRACE_CONFIG);
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
	// Verify if desired new range for heavy rumbling rescale is valid and attempt to calculate rescaling constants if so
	// 
	DS_RUMBLE_SETTINGS* rumbSet = &Context->Configuration.RumbleSettings;
	if (
		rumbSet->AlternativeMode.MaxRange > rumbSet->AlternativeMode.MinRange
		&& rumbSet->AlternativeMode.MinRange > 0
		)
	{
		Context->RumbleControlState.AltMode.IsEnabled = rumbSet->AlternativeMode.IsEnabled;

		DOUBLE LConstA = (DOUBLE)(rumbSet->AlternativeMode.MaxRange - rumbSet->AlternativeMode.MinRange) / (254);
		DOUBLE LConstB = rumbSet->AlternativeMode.MaxRange - LConstA * 255;

		Context->RumbleControlState.AltMode.LightRescale.ConstA = LConstA;
		Context->RumbleControlState.AltMode.LightRescale.ConstB = LConstB;
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
			"Disallowing light rumble rescalling because an invalid range was defined"
		);
		Context->RumbleControlState.AltMode.LightRescale.IsAllowed = FALSE;
	}

	//
	// Verify if desired new range for light rumbling rescale when in alternative mode is valid and attempt to calculate rescaling constants if so
	// 
	if (
		rumbSet->HeavyRescaling.MaxRange > rumbSet->HeavyRescaling.MinRange
		&& rumbSet->HeavyRescaling.MinRange > 0
		)
	{
		Context->RumbleControlState.HeavyRescaleEnabled = rumbSet->HeavyRescaling.IsEnabled;

		DOUBLE HConstA = (DOUBLE)(rumbSet->HeavyRescaling.MaxRange - rumbSet->HeavyRescaling.MinRange) / (254);
		DOUBLE HConstB = rumbSet->HeavyRescaling.MaxRange - HConstA * 255;

		Context->RumbleControlState.HeavyRescale.ConstA = HConstA;
		Context->RumbleControlState.HeavyRescale.ConstB = HConstB;
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
	FuncEntry(TRACE_CONFIG);

	//
	// Common
	// 

	Config->HidDeviceMode = DsHidMiniDeviceModeXInputHIDCompatible;
	Config->DisableAutoPairing = FALSE;
	Config->DevicePairingMode = DsDevicePairingModeAuto;
	Config->PairOnHotReload = FALSE;
	for (int i = 0; i < 6; i++)
	{
		Config->CustomHostAddress[i] = 0x00;
	}
	Config->IsOutputRateControlEnabled = TRUE;
	Config->OutputRateControlPeriodMs = 150;
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

	Config->RumbleSettings.DisableLeft = FALSE;
	Config->RumbleSettings.DisableRight = FALSE;
	Config->RumbleSettings.HeavyRescaling.IsEnabled = TRUE;
	Config->RumbleSettings.HeavyRescaling.MinRange = 64;
	Config->RumbleSettings.HeavyRescaling.MaxRange = 255;
	Config->RumbleSettings.AlternativeMode.IsEnabled = FALSE;
	Config->RumbleSettings.AlternativeMode.MinRange = 1;
	Config->RumbleSettings.AlternativeMode.MaxRange = 90;
	Config->RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled = TRUE;
	Config->RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold = 242;
	Config->RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled = FALSE;
	Config->RumbleSettings.AlternativeMode.ForcedRight.LightThreshold = 242;
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.IsEnabled = FALSE;
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.HoldTime = 1000;
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[0] = 0; // Select
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[1] = 0; // Select
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[2] = 16; // PS

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
		pPlayerSlots[playerIndex]->TotalDuration = 0xFF;
		pPlayerSlots[playerIndex]->BasePortionDuration = 0x01;
		pPlayerSlots[playerIndex]->OffPortionMultiplier = 0x00;
		pPlayerSlots[playerIndex]->OnPortionMultiplier = 0x01;
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

	FuncExitNoReturn(TRACE_CONFIG);
}
