#ifdef DSHM_CONFIG_PARSER_HOST_TEST
#include <Windows.h>
#include <math.h>
#include <float.h>
#include "cJSON.h"
#include "DsCommon.h"
#include "Configuration.Json.h"

#define DS3_LED_1                           0x02
#define DS3_BUTTON_COMBO_OFFSET_SELECT      0
#define DS3_BUTTON_COMBO_OFFSET_L1          10
#define DS3_BUTTON_COMBO_OFFSET_R1          11
#define DS3_BUTTON_COMBO_OFFSET_PS          16

#define FuncEntry(Flags)                        ((void)0)
#define FuncExitNoReturn(Flags)                 ((void)0)
#define TraceError(Flags, Msg, ...)             ((void)0)
#define TraceWarning(Flags, Msg, ...)           ((void)0)
#define TraceVerbose(Flags, Msg, ...)           ((void)0)
#define EventWriteOverrideSettingUInt(...)      ((void)0)
#define EventWriteOverrideSettingDouble(...)    ((void)0)
#define EventWriteLoadingDeviceSpecificConfig(...) ((void)0)
#define TRACE_CONFIG                            0
#define TRACE_DS3                               0
#else
#include "Driver.h"
#include <math.h>
#include <float.h>
#include "Configuration.Json.h"
#include "Configuration.Json.tmh"
#endif

static BOOLEAN
ConfigIsFiniteNumber(
	_In_ DOUBLE Value
)
{
	return (Value == Value) && (Value <= DBL_MAX) && (Value >= -DBL_MAX);
}

static BOOLEAN
ConfigIsIntegralInRange(
	_In_ DOUBLE Value,
	_In_ DOUBLE MinValue,
	_In_ DOUBLE MaxValue
)
{
	if (!ConfigIsFiniteNumber(Value))
	{
		return FALSE;
	}

	if (Value < MinValue || Value > MaxValue)
	{
		return FALSE;
	}

	return Value == floor(Value);
}

static const cJSON*
ConfigGetUniqueItem(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name
)
{
	const cJSON* match = NULL;
	int count = 0;

	if (!cJSON_IsObject(Object) || Name == NULL)
	{
		return NULL;
	}

	for (const cJSON* child = Object->child; child != NULL; child = child->next)
	{
		if (child->string != NULL && _stricmp(child->string, Name) == 0)
		{
			count++;
			match = child;
		}
	}

	if (count == 0)
	{
		return NULL;
	}

	if (count > 1)
	{
		TraceWarning(
			TRACE_CONFIG,
			"Duplicate configuration key %s ignored",
			Name
		);
		return NULL;
	}

	return match;
}

static const cJSON*
ConfigGetObjectMember(
	_In_opt_ const cJSON* Parent,
	_In_z_ const CHAR* Name
)
{
	const cJSON* node = ConfigGetUniqueItem(Parent, Name);

	if (node == NULL)
	{
		return NULL;
	}

	if (!cJSON_IsObject(node))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is not an object, ignoring",
			Name
		);
		return NULL;
	}

	return node;
}

static BOOLEAN
ConfigTryGetBool(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_ PBOOLEAN Value
)
{
	const cJSON* node = ConfigGetUniqueItem(Object, Name);

	*Value = FALSE;

	if (node == NULL)
	{
		return FALSE;
	}

	if (!cJSON_IsBool(node))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is not a boolean, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = (BOOLEAN)cJSON_IsTrue(node);
	return TRUE;
}

static BOOLEAN
ConfigTryGetNumber(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_ DOUBLE* Value
)
{
	const cJSON* node = ConfigGetUniqueItem(Object, Name);

	*Value = 0.0;

	if (node == NULL)
	{
		return FALSE;
	}

	if (!cJSON_IsNumber(node) || !ConfigIsFiniteNumber(node->valuedouble))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is not a finite number, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = node->valuedouble;
	return TRUE;
}

static BOOLEAN
ConfigTryGetUChar(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_ PUCHAR Value
)
{
	DOUBLE number = 0.0;

	*Value = 0;

	if (!ConfigTryGetNumber(Object, Name, &number))
	{
		return FALSE;
	}

	if (!ConfigIsIntegralInRange(number, 0.0, 255.0))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is outside the 0-255 integer range, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = (UCHAR)number;
	return TRUE;
}

static BOOLEAN
ConfigTryGetUShort(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_ PUSHORT Value
)
{
	DOUBLE number = 0.0;

	*Value = 0;

	if (!ConfigTryGetNumber(Object, Name, &number))
	{
		return FALSE;
	}

	if (!ConfigIsIntegralInRange(number, 0.0, 65535.0))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is outside the 0-65535 integer range, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = (USHORT)number;
	return TRUE;
}

static BOOLEAN
ConfigTryGetULong(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_ PULONG Value
)
{
	DOUBLE number = 0.0;

	*Value = 0;

	if (!ConfigTryGetNumber(Object, Name, &number))
	{
		return FALSE;
	}

	if (!ConfigIsIntegralInRange(number, 0.0, 4294967295.0))
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is outside the unsigned 32-bit integer range, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = (ULONG)number;
	return TRUE;
}

static BOOLEAN
ConfigTryGetString(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Outptr_result_z_ const CHAR** Value
)
{
	const cJSON* node = ConfigGetUniqueItem(Object, Name);

	*Value = NULL;

	if (node == NULL)
	{
		return FALSE;
	}

	if (!cJSON_IsString(node) || node->valuestring == NULL)
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s is not a string, ignoring",
			Name
		);
		return FALSE;
	}

	*Value = node->valuestring;
	return TRUE;
}

static BOOLEAN
ConfigParseHexByte(
	_In_ CHAR High,
	_In_ CHAR Low,
	_Out_ PUCHAR Value
)
{
	int high;
	int low;

	*Value = 0;

	if (High >= '0' && High <= '9')
	{
		high = High - '0';
	}
	else if (High >= 'A' && High <= 'F')
	{
		high = High - 'A' + 10;
	}
	else if (High >= 'a' && High <= 'f')
	{
		high = High - 'a' + 10;
	}
	else
	{
		return FALSE;
	}

	if (Low >= '0' && Low <= '9')
	{
		low = Low - '0';
	}
	else if (Low >= 'A' && Low <= 'F')
	{
		low = Low - 'A' + 10;
	}
	else if (Low >= 'a' && Low <= 'f')
	{
		low = Low - 'a' + 10;
	}
	else
	{
		return FALSE;
	}

	*Value = (UCHAR)((high << 4) | low);
	return TRUE;
}

static BOOLEAN
ConfigTryGetPairingAddress(
	_In_opt_ const cJSON* Object,
	_In_z_ const CHAR* Name,
	_Out_writes_(6) UCHAR Address[6]
)
{
	const CHAR* text = NULL;
	SIZE_T length;

	RtlZeroMemory(Address, 6);

	if (!ConfigTryGetString(Object, Name, &text))
	{
		return FALSE;
	}

	length = strlen(text);
	if (length != 12)
	{
		TraceWarning(
			TRACE_CONFIG,
			"Configuration value %s must be exactly 12 hexadecimal digits, ignoring",
			Name
		);
		return FALSE;
	}

	for (int i = 0; i < 6; i++)
	{
		if (!ConfigParseHexByte(text[i * 2], text[(i * 2) + 1], &Address[i]))
		{
			TraceWarning(
				TRACE_CONFIG,
				"Configuration value %s is not a hexadecimal address, ignoring",
				Name
			);
			RtlZeroMemory(Address, 6);
			return FALSE;
		}
	}

	return TRUE;
}

static BOOLEAN
ConfigTryGetHidDeviceMode(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_HID_DEVICE_MODE Mode
)
{
	const CHAR* name = NULL;

	*Mode = DsHidMiniDeviceModeUnknown;

	if (!ConfigTryGetString(Object, "HidDeviceMode", &name))
	{
		return FALSE;
	}

	for (DS_HID_DEVICE_MODE value = 1;
		value < (DS_HID_DEVICE_MODE)_countof(G_HID_DEVICE_MODE_NAMES);
		value++)
	{
		if (strcmp(G_HID_DEVICE_MODE_NAMES[value], name) == 0)
		{
			*Mode = value;
			return TRUE;
		}
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown HidDeviceMode '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetPairingMode(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_HOST_PAIRING_MODE Mode
)
{
	const CHAR* name = NULL;

	*Mode = DsDevicePairingModeDisabled;

	if (!ConfigTryGetString(Object, "DevicePairingMode", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_DEVICE_PAIRING_MODE_NAMES[0]))
	{
		*Mode = DsDevicePairingModeAuto;
		return TRUE;
	}

	if (!_strcmpi(name, G_DEVICE_PAIRING_MODE_NAMES[1]))
	{
		*Mode = DsDevicePairingModeCustom;
		return TRUE;
	}

	if (!_strcmpi(name, G_DEVICE_PAIRING_MODE_NAMES[2]))
	{
		*Mode = DsDevicePairingModeDisabled;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown DevicePairingMode '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetUsbTransport(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_USB_OUTPUT_REPORT_TRANSPORT Transport
)
{
	const CHAR* name = NULL;

	*Transport = DsUsbOutputReportTransportAuto;

	if (!ConfigTryGetString(Object, "UsbOutputReportTransport", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_USB_OUTPUT_REPORT_TRANSPORT_NAMES[0]))
	{
		*Transport = DsUsbOutputReportTransportAuto;
		return TRUE;
	}

	if (!_strcmpi(name, G_USB_OUTPUT_REPORT_TRANSPORT_NAMES[1]))
	{
		*Transport = DsUsbOutputReportTransportInterruptOut;
		return TRUE;
	}

	if (!_strcmpi(name, G_USB_OUTPUT_REPORT_TRANSPORT_NAMES[2]))
	{
		*Transport = DsUsbOutputReportTransportControlEndpoint;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown UsbOutputReportTransport '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetBluetoothTransport(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_BLUETOOTH_OUTPUT_REPORT_TRANSPORT Transport
)
{
	const CHAR* name = NULL;

	*Transport = DsBluetoothOutputReportTransportControl;

	if (!ConfigTryGetString(Object, "BluetoothOutputReportTransport", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_BLUETOOTH_OUTPUT_REPORT_TRANSPORT_NAMES[0]))
	{
		*Transport = DsBluetoothOutputReportTransportControl;
		return TRUE;
	}

	if (!_strcmpi(name, G_BLUETOOTH_OUTPUT_REPORT_TRANSPORT_NAMES[1]))
	{
		*Transport = DsBluetoothOutputReportTransportInterrupt;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown BluetoothOutputReportTransport '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetPressureMode(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_PRESSURE_EXPOSURE_MODE Mode
)
{
	const CHAR* name = NULL;

	*Mode = DsPressureExposureModeDefault;

	if (!ConfigTryGetString(Object, "PressureExposureMode", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_PRESSURE_EXPOSURE_MODE_NAMES[0]))
	{
		*Mode = DsPressureExposureModeDigital;
		return TRUE;
	}

	if (!_strcmpi(name, G_PRESSURE_EXPOSURE_MODE_NAMES[1]))
	{
		*Mode = DsPressureExposureModeAnalogue;
		return TRUE;
	}

	if (!_strcmpi(name, G_PRESSURE_EXPOSURE_MODE_NAMES[2]))
	{
		*Mode = DsPressureExposureModeDefault;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown PressureExposureMode '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetDPadMode(
	_In_opt_ const cJSON* Object,
	_Out_ PDS_DPAD_EXPOSURE_MODE Mode
)
{
	const CHAR* name = NULL;

	*Mode = DsDPadExposureModeDefault;

	if (!ConfigTryGetString(Object, "DPadExposureMode", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_DPAD_EXPOSURE_MODE_NAMES[0]))
	{
		*Mode = DsDPadExposureModeHAT;
		return TRUE;
	}

	if (!_strcmpi(name, G_DPAD_EXPOSURE_MODE_NAMES[1]))
	{
		*Mode = DsDPadExposureModeIndividualButtons;
		return TRUE;
	}

	if (!_strcmpi(name, G_DPAD_EXPOSURE_MODE_NAMES[2]))
	{
		*Mode = DsDPadExposureModeDefault;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown DPadExposureMode '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetLedMode(
	_In_opt_ const cJSON* Object,
	_Out_ DS_LED_MODE* Mode
)
{
	const CHAR* name = NULL;

	*Mode = DsLEDModeUnknown;

	if (!ConfigTryGetString(Object, "Mode", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_LED_MODE_NAMES[0]))
	{
		*Mode = DsLEDModeBatteryIndicatorPlayerIndex;
		return TRUE;
	}

	if (!_strcmpi(name, G_LED_MODE_NAMES[1]))
	{
		*Mode = DsLEDModeBatteryIndicatorBarGraph;
		return TRUE;
	}

	if (!_strcmpi(name, G_LED_MODE_NAMES[2]))
	{
		*Mode = DsLEDModeCustomPattern;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown LED Mode '%s', ignoring",
		name
	);
	return FALSE;
}

static BOOLEAN
ConfigTryGetLedAuthority(
	_In_opt_ const cJSON* Object,
	_Out_ DS_LED_AUTHORITY* Authority
)
{
	const CHAR* name = NULL;

	*Authority = DsLEDAuthorityAutomatic;

	if (!ConfigTryGetString(Object, "Authority", &name))
	{
		return FALSE;
	}

	if (!_strcmpi(name, G_DS_LED_AUTHORITY_NAMES[0]))
	{
		*Authority = DsLEDAuthorityAutomatic;
		return TRUE;
	}

	if (!_strcmpi(name, G_DS_LED_AUTHORITY_NAMES[1]))
	{
		*Authority = DsLEDAuthorityDriver;
		return TRUE;
	}

	if (!_strcmpi(name, G_DS_LED_AUTHORITY_NAMES[2]))
	{
		*Authority = DsLEDAuthorityApplication;
		return TRUE;
	}

	TraceWarning(
		TRACE_CONFIG,
		"Unknown LED Authority '%s', ignoring",
		name
	);
	return FALSE;
}

static void
ConfigParseButtonComboSettings(
	_In_ const cJSON* ComboSettings,
	_Inout_ PDS_BUTTON_COMBO Combo
)
{
	BOOLEAN isEnabled = FALSE;
	ULONG holdTime = 0;
	UCHAR offset = 0;

	if (ConfigTryGetBool(ComboSettings, "IsEnabled", &isEnabled))
	{
		Combo->IsEnabled = isEnabled;
		EventWriteOverrideSettingUInt(ComboSettings->string, "IsEnabled", Combo->IsEnabled);
	}

	if (ConfigTryGetULong(ComboSettings, "HoldTime", &holdTime))
	{
		Combo->HoldTime = holdTime;
		EventWriteOverrideSettingUInt(ComboSettings->string, "HoldTime", Combo->HoldTime);
	}

	for (ULONGLONG buttonIndex = 0; buttonIndex < _countof(Combo->Buttons); buttonIndex++)
	{
		if (!ConfigTryGetUChar(ComboSettings, G_DS_BUTTON_COMBO_NAMES[buttonIndex], &offset))
		{
			continue;
		}

		if (offset <= DS_BUTTON_COMBO_MAX_OFFSET)
		{
			Combo->Buttons[buttonIndex] = offset;
			EventWriteOverrideSettingUInt(
				ComboSettings->string,
				G_DS_BUTTON_COMBO_NAMES[buttonIndex],
				Combo->Buttons[buttonIndex]
			);
		}
		else
		{
			TraceWarning(
				TRACE_CONFIG,
				"Provided button offset %d for %s out of range, ignoring",
				offset,
				G_DS_BUTTON_COMBO_NAMES[buttonIndex]
			);
		}
	}
}

static void
ConfigParseRumbleSettings(
	_In_ const cJSON* RumbleSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	BOOLEAN flag = FALSE;
	UCHAR range = 0;
	const cJSON* heavyRescale = NULL;
	const cJSON* alternativeMode = NULL;
	const cJSON* forced = NULL;
	const cJSON* toggleCombo = NULL;

	if (ConfigTryGetBool(RumbleSettings, "DisableLeft", &flag))
	{
		Config->RumbleSettings.DisableLeft = flag;
		EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.DisableLeft", Config->RumbleSettings.DisableLeft);
	}

	if (ConfigTryGetBool(RumbleSettings, "DisableRight", &flag))
	{
		Config->RumbleSettings.DisableRight = flag;
		EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.DisableRight", Config->RumbleSettings.DisableRight);
	}

	heavyRescale = ConfigGetObjectMember(RumbleSettings, "HeavyRescale");
	if (heavyRescale)
	{
		if (ConfigTryGetBool(heavyRescale, "IsEnabled", &flag))
		{
			Config->RumbleSettings.HeavyRescaling.IsEnabled = flag;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.IsEnabled", Config->RumbleSettings.HeavyRescaling.IsEnabled);
		}

		if (ConfigTryGetUChar(heavyRescale, "RescaleMinRange", &range))
		{
			Config->RumbleSettings.HeavyRescaling.MinRange = range;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.MinRange", Config->RumbleSettings.HeavyRescaling.MinRange);
		}

		if (ConfigTryGetUChar(heavyRescale, "RescaleMaxRange", &range))
		{
			Config->RumbleSettings.HeavyRescaling.MaxRange = range;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.HeavyRescaling.MaxRange", Config->RumbleSettings.HeavyRescaling.MaxRange);
		}
	}

	alternativeMode = ConfigGetObjectMember(RumbleSettings, "AlternativeMode");
	if (alternativeMode)
	{
		if (ConfigTryGetBool(alternativeMode, "IsEnabled", &flag))
		{
			Config->RumbleSettings.AlternativeMode.IsEnabled = flag;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.IsEnabled", Config->RumbleSettings.AlternativeMode.IsEnabled);
		}

		if (ConfigTryGetUChar(alternativeMode, "RescaleMinRange", &range))
		{
			Config->RumbleSettings.AlternativeMode.MinRange = range;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.MinRange", Config->RumbleSettings.AlternativeMode.MinRange);
		}

		if (ConfigTryGetUChar(alternativeMode, "RescaleMaxRange", &range))
		{
			Config->RumbleSettings.AlternativeMode.MaxRange = range;
			EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.MaxRange", Config->RumbleSettings.AlternativeMode.MaxRange);
		}

		toggleCombo = ConfigGetObjectMember(alternativeMode, "ToggleCombo");
		if (toggleCombo)
		{
			ConfigParseButtonComboSettings(toggleCombo, &Config->RumbleSettings.AlternativeMode.ToggleButtonCombo);
		}

		forced = ConfigGetObjectMember(alternativeMode, "ForcedRight");
		if (forced)
		{
			if (ConfigTryGetBool(forced, "IsHeavyThresholdEnabled", &flag))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled = flag;
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled", Config->RumbleSettings.AlternativeMode.ForcedRight.IsHeavyThresholdEnabled);
			}

			if (ConfigTryGetBool(forced, "IsLightThresholdEnabled", &flag))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled = flag;
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled", Config->RumbleSettings.AlternativeMode.ForcedRight.IsLightThresholdEnabled);
			}

			if (ConfigTryGetUChar(forced, "HeavyThreshold", &range))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold = range;
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold", Config->RumbleSettings.AlternativeMode.ForcedRight.HeavyThreshold);
			}

			if (ConfigTryGetUChar(forced, "LightThreshold", &range))
			{
				Config->RumbleSettings.AlternativeMode.ForcedRight.LightThreshold = range;
				EventWriteOverrideSettingUInt(RumbleSettings->string, "RumbleSettings.AlternativeMode.ForcedRight.LightThreshold", Config->RumbleSettings.AlternativeMode.ForcedRight.LightThreshold);
			}
		}
	}
}

static void
ConfigParseLEDSettings(
	_In_ const cJSON* LEDSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	DS_LED_MODE mode = DsLEDModeUnknown;
	DS_LED_AUTHORITY authority = DsLEDAuthorityAutomatic;
	UCHAR flags = 0;
	const cJSON* customPatterns = NULL;

	if (ConfigTryGetLedMode(LEDSettings, &mode))
	{
		Config->LEDSettings.Mode = mode;
		EventWriteOverrideSettingUInt(LEDSettings->string, "Mode", Config->LEDSettings.Mode);
	}

	if (ConfigTryGetLedAuthority(LEDSettings, &authority))
	{
		Config->LEDSettings.Authority = authority;
		EventWriteOverrideSettingUInt(LEDSettings->string, "Authority", Config->LEDSettings.Authority);
	}

	if (Config->LEDSettings.Mode != DsLEDModeCustomPattern)
	{
		return;
	}

	customPatterns = ConfigGetObjectMember(LEDSettings, "CustomPatterns");
	if (customPatterns == NULL)
	{
		return;
	}

	if (ConfigTryGetUChar(customPatterns, "LEDFlags", &flags))
	{
		Config->LEDSettings.CustomPatterns.LEDFlags = flags;
	}

	const PSTR playerSlotNames[] =
	{
		"Player1",
		"Player2",
		"Player3",
		"Player4",
	};

	PDS_LED playerSlots[] =
	{
		&Config->LEDSettings.CustomPatterns.Player1,
		&Config->LEDSettings.CustomPatterns.Player2,
		&Config->LEDSettings.CustomPatterns.Player3,
		&Config->LEDSettings.CustomPatterns.Player4,
	};

	for (ULONGLONG playerIndex = 0; playerIndex < _countof(playerSlotNames); playerIndex++)
	{
		const cJSON* player = ConfigGetObjectMember(customPatterns, playerSlotNames[playerIndex]);
		UCHAR byteValue = 0;
		USHORT shortValue = 0;

		if (player == NULL)
		{
			continue;
		}

		if (ConfigTryGetUChar(player, "TotalDuration", &byteValue))
		{
			playerSlots[playerIndex]->TotalDuration = byteValue;
			EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "TotalDuration", playerSlots[playerIndex]->TotalDuration);
		}

		if (ConfigTryGetUShort(player, "BasePortionDuration", &shortValue))
		{
			playerSlots[playerIndex]->BasePortionDuration = shortValue;
			EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "BasePortionDuration", playerSlots[playerIndex]->BasePortionDuration);
		}

		if (ConfigTryGetUChar(player, "OffPortionMultiplier", &byteValue))
		{
			playerSlots[playerIndex]->OffPortionMultiplier = byteValue;
			EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "OffPortionMultiplier", playerSlots[playerIndex]->OffPortionMultiplier);
		}

		if (ConfigTryGetUChar(player, "OnPortionMultiplier", &byteValue))
		{
			playerSlots[playerIndex]->OnPortionMultiplier = byteValue;
			EventWriteOverrideSettingUInt(playerSlotNames[playerIndex], "OnPortionMultiplier", playerSlots[playerIndex]->OnPortionMultiplier);
		}
	}
}

static void
ConfigParseHidDeviceModeSpecificSettings(
	_In_ const cJSON* NodeSettings,
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	DS_PRESSURE_EXPOSURE_MODE pressureMode = DsPressureExposureModeDefault;
	DS_DPAD_EXPOSURE_MODE dpadMode = DsDPadExposureModeDefault;

	switch (Config->HidDeviceMode)
	{
	case DsHidMiniDeviceModeSDF:
		if (ConfigTryGetPressureMode(NodeSettings, &pressureMode))
		{
			Config->SDF.PressureExposureMode = pressureMode;
			EventWriteOverrideSettingUInt(NodeSettings->string, "SDF.PressureExposureMode", Config->SDF.PressureExposureMode);
		}

		if (ConfigTryGetDPadMode(NodeSettings, &dpadMode))
		{
			Config->SDF.DPadExposureMode = dpadMode;
			EventWriteOverrideSettingUInt(NodeSettings->string, "SDF.DPadExposureMode", Config->SDF.DPadExposureMode);
		}
		break;

	case DsHidMiniDeviceModeGPJ:
		if (ConfigTryGetPressureMode(NodeSettings, &pressureMode))
		{
			Config->GPJ.PressureExposureMode = pressureMode;
			EventWriteOverrideSettingUInt(NodeSettings->string, "GPJ.PressureExposureMode", Config->GPJ.PressureExposureMode);
		}

		if (ConfigTryGetDPadMode(NodeSettings, &dpadMode))
		{
			Config->GPJ.DPadExposureMode = dpadMode;
			EventWriteOverrideSettingUInt(NodeSettings->string, "GPJ.DPadExposureMode", Config->GPJ.DPadExposureMode);
		}
		break;

	default:
		break;
	}
}

static void
ConfigParseDeadZone(
	_In_opt_ const cJSON* DeadZone,
	_Inout_ PDS_AXIS_DEADZONE Target,
	_In_z_ const CHAR* ApplyEventName,
	_In_z_ const CHAR* PolarEventName
)
{
	BOOLEAN apply = FALSE;
	DOUBLE polarValue = 0.0;

	if (DeadZone == NULL)
	{
		return;
	}

	if (ConfigTryGetBool(DeadZone, "Apply", &apply))
	{
		Target->Apply = apply;
		EventWriteOverrideSettingUInt(DeadZone->string, ApplyEventName, Target->Apply);
	}

	if (ConfigTryGetNumber(DeadZone, "PolarValue", &polarValue))
	{
		if (polarValue < 0.0 || polarValue > 360.0)
		{
			TraceWarning(
				TRACE_CONFIG,
				"Configuration value PolarValue is outside the 0-360 range, ignoring"
			);
			return;
		}

		Target->PolarValue = polarValue;
		EventWriteOverrideSettingDouble(DeadZone->string, PolarEventName, Target->PolarValue);
	}
}

static void
ConfigNodeParse(
	_In_ const cJSON* ParentNode,
	_Inout_ PDS_DRIVER_CONFIGURATION Config,
	_In_ BOOLEAN IsHotReload
)
{
	BOOLEAN flag = FALSE;
	UCHAR period = 0;
	ULONG timeout = 0;
	DS_HID_DEVICE_MODE hidMode = DsHidMiniDeviceModeUnknown;
	DS_DEVICE_PAIRING_MODE pairingMode = DsDevicePairingModeDisabled;
	DS_USB_OUTPUT_REPORT_TRANSPORT usbTransport = DsUsbOutputReportTransportAuto;
	DS_BLUETOOTH_OUTPUT_REPORT_TRANSPORT bluetoothTransport = DsBluetoothOutputReportTransportControl;
	UCHAR customAddress[6];
	const cJSON* combo = NULL;
	const cJSON* modeSpecific = NULL;

	FuncEntry(TRACE_CONFIG);

	if (!IsHotReload)
	{
		if (ConfigTryGetHidDeviceMode(ParentNode, &hidMode))
		{
			Config->HidDeviceMode = hidMode;
			EventWriteOverrideSettingUInt(ParentNode->string, "HidDeviceMode", Config->HidDeviceMode);
		}

		if (ConfigTryGetBool(ParentNode, "AutoRestartOnHidModeMismatch", &flag))
		{
			Config->AutoRestartOnHidModeMismatch = flag;
			EventWriteOverrideSettingUInt(ParentNode->string, "AutoRestartOnHidModeMismatch", Config->AutoRestartOnHidModeMismatch);
		}

		if (ConfigTryGetUsbTransport(ParentNode, &usbTransport))
		{
			Config->UsbOutputReportTransport = usbTransport;
			EventWriteOverrideSettingUInt(ParentNode->string, "UsbOutputReportTransport", Config->UsbOutputReportTransport);
		}
	}

	if (ConfigTryGetBluetoothTransport(ParentNode, &bluetoothTransport))
	{
		Config->BluetoothOutputReportTransport = bluetoothTransport;
		EventWriteOverrideSettingUInt(ParentNode->string, "BluetoothOutputReportTransport", Config->BluetoothOutputReportTransport);
	}

	if (ConfigTryGetPairingMode(ParentNode, &pairingMode))
	{
		Config->DevicePairingMode = pairingMode;
		EventWriteOverrideSettingUInt(ParentNode->string, "DevicePairingMode", Config->DevicePairingMode);
	}

	if (ConfigTryGetPairingAddress(ParentNode, "CustomPairingAddress", customAddress))
	{
		RtlCopyMemory(Config->CustomHostAddress, customAddress, sizeof(customAddress));
		TraceVerbose(
			TRACE_DS3,
			"Configuration custom address: %02X:%02X:%02X:%02X:%02X:%02X",
			Config->CustomHostAddress[0],
			Config->CustomHostAddress[1],
			Config->CustomHostAddress[2],
			Config->CustomHostAddress[3],
			Config->CustomHostAddress[4],
			Config->CustomHostAddress[5]
		);
	}

	if (ConfigTryGetBool(ParentNode, "IsOutputRateControlEnabled", &flag))
	{
		Config->IsOutputRateControlEnabled = flag;
		EventWriteOverrideSettingUInt(ParentNode->string, "IsOutputRateControlEnabled", Config->IsOutputRateControlEnabled);
	}

	if (ConfigTryGetUChar(ParentNode, "OutputRateControlPeriodMs", &period))
	{
		Config->OutputRateControlPeriodMs = period;
		EventWriteOverrideSettingUInt(ParentNode->string, "OutputRateControlPeriodMs", Config->OutputRateControlPeriodMs);
	}

	if (ConfigTryGetULong(ParentNode, "WirelessIdleTimeoutPeriodMs", &timeout))
	{
		Config->WirelessIdleTimeoutPeriodMs = timeout;
		EventWriteOverrideSettingUInt(ParentNode->string, "WirelessIdleTimeoutPeriodMs", Config->WirelessIdleTimeoutPeriodMs);
	}

	if (ConfigTryGetBool(ParentNode, "DisableWirelessIdleTimeout", &flag))
	{
		Config->DisableWirelessIdleTimeout = flag;
		EventWriteOverrideSettingUInt(ParentNode->string, "DisableWirelessIdleTimeout", Config->DisableWirelessIdleTimeout);
	}

	combo = ConfigGetObjectMember(ParentNode, "QuickDisconnectCombo");
	if (combo)
	{
		ConfigParseButtonComboSettings(combo, &Config->WirelessDisconnectButtonCombo);
	}

	if (Config->HidDeviceMode > 0 &&
		Config->HidDeviceMode < (DS_HID_DEVICE_MODE)_countof(G_HID_DEVICE_MODE_NAMES))
	{
		modeSpecific = ConfigGetObjectMember(ParentNode, G_HID_DEVICE_MODE_NAMES[Config->HidDeviceMode]);
		if (modeSpecific)
		{
			BOOLEAN flip = FALSE;
			const cJSON* rumbleSettings = NULL;
			const cJSON* ledSettings = NULL;
			const cJSON* flipAxis = NULL;

			ConfigParseHidDeviceModeSpecificSettings(modeSpecific, Config);
			ConfigParseDeadZone(
				ConfigGetObjectMember(modeSpecific, "DeadZoneLeft"),
				&Config->ThumbSettings.DeadZoneLeft,
				"ThumbSettings.DeadZoneLeft.Apply",
				"ThumbSettings.DeadZoneLeft.PolarValue"
			);
			ConfigParseDeadZone(
				ConfigGetObjectMember(modeSpecific, "DeadZoneRight"),
				&Config->ThumbSettings.DeadZoneRight,
				"ThumbSettings.DeadZoneRight.Apply",
				"ThumbSettings.DeadZoneRight.PolarValue"
			);

			rumbleSettings = ConfigGetObjectMember(modeSpecific, "RumbleSettings");
			if (rumbleSettings)
			{
				ConfigParseRumbleSettings(rumbleSettings, Config);
			}

			ledSettings = ConfigGetObjectMember(modeSpecific, "LEDSettings");
			if (ledSettings)
			{
				ConfigParseLEDSettings(ledSettings, Config);
			}

			flipAxis = ConfigGetObjectMember(modeSpecific, "FlipAxis");
			if (flipAxis)
			{
				if (ConfigTryGetBool(flipAxis, "LeftX", &flip))
				{
					Config->FlipAxis.LeftX = flip;
				}

				if (ConfigTryGetBool(flipAxis, "LeftY", &flip))
				{
					Config->FlipAxis.LeftY = flip;
				}

				if (ConfigTryGetBool(flipAxis, "RightX", &flip))
				{
					Config->FlipAxis.RightX = flip;
				}

				if (ConfigTryGetBool(flipAxis, "RightY", &flip))
				{
					Config->FlipAxis.RightY = flip;
				}
			}
		}
	}

	FuncExitNoReturn(TRACE_CONFIG);
}

void
ConfigSetDefaults(
	_Inout_ PDS_DRIVER_CONFIGURATION Config
)
{
	FuncEntry(TRACE_CONFIG);

	RtlZeroMemory(Config, sizeof(*Config));

	Config->HidDeviceMode = DsHidMiniDeviceModeXInputHIDCompatible;
	Config->DevicePairingMode = DsDevicePairingModeAuto;
	Config->AutoRestartOnHidModeMismatch = TRUE;
	Config->UsbOutputReportTransport = DsUsbOutputReportTransportAuto;
	Config->BluetoothOutputReportTransport = DsBluetoothOutputReportTransportControl;
	Config->IsOutputRateControlEnabled = TRUE;
	Config->OutputRateControlPeriodMs = 150;
	Config->WirelessIdleTimeoutPeriodMs = 300000;
	Config->DisableWirelessIdleTimeout = FALSE;

	Config->WirelessDisconnectButtonCombo.IsEnabled = TRUE;
	Config->WirelessDisconnectButtonCombo.HoldTime = 1000;
	Config->WirelessDisconnectButtonCombo.Buttons[0] = DS3_BUTTON_COMBO_OFFSET_L1;
	Config->WirelessDisconnectButtonCombo.Buttons[1] = DS3_BUTTON_COMBO_OFFSET_R1;
	Config->WirelessDisconnectButtonCombo.Buttons[2] = DS3_BUTTON_COMBO_OFFSET_PS;

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
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[0] = DS3_BUTTON_COMBO_OFFSET_SELECT;
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[1] = DS3_BUTTON_COMBO_OFFSET_SELECT;
	Config->RumbleSettings.AlternativeMode.ToggleButtonCombo.Buttons[2] = DS3_BUTTON_COMBO_OFFSET_PS;

	Config->LEDSettings.Mode = DsLEDModeBatteryIndicatorPlayerIndex;
	Config->LEDSettings.Authority = DsLEDAuthorityAutomatic;
	Config->LEDSettings.CustomPatterns.LEDFlags = DS3_LED_1;

	PDS_LED playerSlots[] =
	{
		&Config->LEDSettings.CustomPatterns.Player1,
		&Config->LEDSettings.CustomPatterns.Player2,
		&Config->LEDSettings.CustomPatterns.Player3,
		&Config->LEDSettings.CustomPatterns.Player4,
	};

	for (ULONGLONG playerIndex = 0; playerIndex < _countof(playerSlots); playerIndex++)
	{
		playerSlots[playerIndex]->TotalDuration = 0xFF;
		playerSlots[playerIndex]->BasePortionDuration = 0x01;
		playerSlots[playerIndex]->OffPortionMultiplier = 0x00;
		playerSlots[playerIndex]->OnPortionMultiplier = 0x01;
	}

	Config->SDF.PressureExposureMode = DsPressureExposureModeDefault;
	Config->SDF.DPadExposureMode = DsDPadExposureModeDefault;
	Config->GPJ.PressureExposureMode = DsPressureExposureModeDefault;
	Config->GPJ.DPadExposureMode = DsDPadExposureModeDefault;

	FuncExitNoReturn(TRACE_CONFIG);
}

void
ConfigDeriveRumbleState(
	_In_ const DS_DRIVER_CONFIGURATION* Config,
	_Out_ PDS_CONFIG_RUMBLE_DERIVED Derived
)
{
	const DS_RUMBLE_SETTINGS* rumble = &Config->RumbleSettings;

	RtlZeroMemory(Derived, sizeof(*Derived));

	if (rumble->AlternativeMode.MaxRange > rumble->AlternativeMode.MinRange &&
		rumble->AlternativeMode.MinRange > 0)
	{
		const DOUBLE lightConstA = (DOUBLE)(rumble->AlternativeMode.MaxRange - rumble->AlternativeMode.MinRange) / 254.0;
		const DOUBLE lightConstB = rumble->AlternativeMode.MaxRange - lightConstA * 255.0;

		Derived->AltModeIsEnabled = rumble->AlternativeMode.IsEnabled;
		Derived->LightRescaleConstA = lightConstA;
		Derived->LightRescaleConstB = lightConstB;
		Derived->LightRescaleIsAllowed = TRUE;
	}

	if (rumble->HeavyRescaling.MaxRange > rumble->HeavyRescaling.MinRange &&
		rumble->HeavyRescaling.MinRange > 0)
	{
		const DOUBLE heavyConstA = (DOUBLE)(rumble->HeavyRescaling.MaxRange - rumble->HeavyRescaling.MinRange) / 254.0;
		const DOUBLE heavyConstB = rumble->HeavyRescaling.MaxRange - heavyConstA * 255.0;

		Derived->HeavyRescaleEnabled = rumble->HeavyRescaling.IsEnabled;
		Derived->HeavyRescaleConstA = heavyConstA;
		Derived->HeavyRescaleConstB = heavyConstB;
		Derived->HeavyRescaleIsAllowed = TRUE;
	}
}

static NTSTATUS
ConfigParseRootObject(
	_In_reads_bytes_(Length) const CHAR* Json,
	_In_ SIZE_T Length,
	_Outptr_ cJSON** Root,
	_Out_opt_ PSIZE_T ErrorOffset
)
{
	const CHAR* parseEnd = NULL;
	cJSON* root = NULL;

	if (ErrorOffset)
	{
		*ErrorOffset = 0;
	}

	if (Root == NULL)
	{
		return STATUS_INVALID_PARAMETER;
	}

	*Root = NULL;

	if (Json == NULL || Length == 0 || Length > CONFIG_JSON_MAX_BYTES)
	{
		return STATUS_DATA_ERROR;
	}

	for (SIZE_T index = 0; index < Length; index++)
	{
		if (Json[index] == '\0')
		{
			if (ErrorOffset)
			{
				*ErrorOffset = index;
			}

			TraceError(
				TRACE_CONFIG,
				"JSON payload contains an embedded NUL at offset %Iu",
				index
			);
			return STATUS_DATA_ERROR;
		}
	}

	if (Json[Length] != '\0')
	{
		return STATUS_DATA_ERROR;
	}

	root = cJSON_ParseWithLengthOpts(Json, Length + 1, &parseEnd, TRUE);
	if (root == NULL)
	{
		SIZE_T offset = 0;

		if (parseEnd != NULL && parseEnd >= Json)
		{
			offset = (SIZE_T)(parseEnd - Json);
		}

		if (ErrorOffset)
		{
			*ErrorOffset = offset;
		}

		TraceError(
			TRACE_CONFIG,
			"JSON parsing failed at offset %Iu",
			offset
		);
		return STATUS_DATA_ERROR;
	}

	if (!cJSON_IsObject(root))
	{
		if (ErrorOffset)
		{
			*ErrorOffset = 0;
		}

		TraceError(
			TRACE_CONFIG,
			"JSON configuration root is not an object"
		);
		cJSON_Delete(root);
		return STATUS_DATA_ERROR;
	}

	*Root = root;
	return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
ConfigParseIpcEnabled(
	const CHAR* Json,
	SIZE_T Length,
	PBOOLEAN Enabled,
	PSIZE_T ErrorOffset
)
{
	NTSTATUS status;
	cJSON* root = NULL;
	const cJSON* ipcNode = NULL;
	BOOLEAN enabled = TRUE;

	if (Enabled == NULL)
	{
		return STATUS_INVALID_PARAMETER;
	}

	status = ConfigParseRootObject(Json, Length, &root, ErrorOffset);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	ipcNode = ConfigGetUniqueItem(root, "IPCEnabled");
	if (ipcNode != NULL)
	{
		if (!cJSON_IsBool(ipcNode))
		{
			TraceError(
				TRACE_CONFIG,
				"Configuration value IPCEnabled is not a boolean"
			);
			cJSON_Delete(root);
			return STATUS_DATA_ERROR;
		}

		enabled = (BOOLEAN)cJSON_IsTrue(ipcNode);
	}

	*Enabled = enabled;
	cJSON_Delete(root);
	return STATUS_SUCCESS;
}

_Use_decl_annotations_
NTSTATUS
ConfigParseJsonDocument(
	const CHAR* Json,
	SIZE_T Length,
	const CHAR* DeviceAddress,
	BOOLEAN IsHotReload,
	const DS_DRIVER_CONFIGURATION* Current,
	PDS_DRIVER_CONFIGURATION Parsed,
	PDS_CONFIG_RUMBLE_DERIVED RumbleDerived,
	PSIZE_T ErrorOffset
)
{
	NTSTATUS status;
	cJSON* root = NULL;
	DS_DRIVER_CONFIGURATION candidate;
	const cJSON* globalNode = NULL;
	const cJSON* devicesNode = NULL;

	if (Parsed == NULL || RumbleDerived == NULL)
	{
		return STATUS_INVALID_PARAMETER;
	}

	status = ConfigParseRootObject(Json, Length, &root, ErrorOffset);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	ConfigSetDefaults(&candidate);

	if (IsHotReload && Current != NULL)
	{
		candidate.HidDeviceMode = Current->HidDeviceMode;
		candidate.AutoRestartOnHidModeMismatch = Current->AutoRestartOnHidModeMismatch;
		candidate.UsbOutputReportTransport = Current->UsbOutputReportTransport;
	}

	globalNode = ConfigGetUniqueItem(root, "Global");
	if (globalNode != NULL)
	{
		if (cJSON_IsObject(globalNode))
		{
			TraceVerbose(TRACE_CONFIG, "Loading global configuration");
			ConfigNodeParse(globalNode, &candidate, IsHotReload);
		}
		else
		{
			TraceWarning(
				TRACE_CONFIG,
				"Configuration value Global is not an object, ignoring"
			);
		}
	}

	devicesNode = ConfigGetUniqueItem(root, "Devices");
	if (devicesNode != NULL)
	{
		if (!cJSON_IsObject(devicesNode))
		{
			TraceWarning(
				TRACE_CONFIG,
				"Configuration value Devices is not an object, ignoring"
			);
		}
		else if (DeviceAddress != NULL && DeviceAddress[0] != '\0')
		{
			const cJSON* deviceNode = ConfigGetUniqueItem(devicesNode, DeviceAddress);

			if (deviceNode != NULL)
			{
				if (cJSON_IsObject(deviceNode))
				{
					TraceVerbose(
						TRACE_CONFIG,
						"Found device-specific (%s) config, loading",
						DeviceAddress
					);
					EventWriteLoadingDeviceSpecificConfig(DeviceAddress);
					ConfigNodeParse(deviceNode, &candidate, IsHotReload);
				}
				else
				{
					TraceWarning(
						TRACE_CONFIG,
						"Device-specific (%s) config is not an object, ignoring",
						DeviceAddress
					);
				}
			}
			else
			{
				TraceVerbose(
					TRACE_CONFIG,
					"Device-specific (%s) config not found",
					DeviceAddress
				);
			}
		}
	}

	*Parsed = candidate;
	ConfigDeriveRumbleState(&candidate, RumbleDerived);
	status = STATUS_SUCCESS;

	cJSON_Delete(root);
	return status;
}
