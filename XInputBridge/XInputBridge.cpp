// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "XInputBridge.h"

#include <climits>

//
// Dead-Zone value to stop jittering
// 
#define DS3_AXIS_ANTI_JITTER_OFFSET		10

#define DS3_VID							0x054C
#define DS3_PID							0x0268
#define SXS_MODE_GET_FEATURE_REPORT_ID	0xF2
#define SXS_MODE_GET_FEATURE_BUFFER_LEN	0x40

//
// This is really an artificial limit so set high
// 
#define DS3_DEVICES_MAX					0xFF


//
// Device state information to improve performance
// 
struct device_state
{
	bool isConnected = false;

	hid_device* deviceHandle = nullptr;

	DWORD packetNumber;

	DS3_RAW_INPUT_REPORT lastReport;
};

//
// Keep track on device states for better lookup performance
// 
device_state g_deviceStates[DS3_DEVICES_MAX];

#pragma region Rumble helper types

/*
 * Source: https://github.com/RPCS3/rpcs3/blob/5e436984a2b5753ad340d2c97462bf3be6e86237/rpcs3/Input/ds3_pad_handler.cpp#L9-L40
 */

struct ds3_rumble
{
	UCHAR padding = 0x00;
	UCHAR small_motor_duration = 0xFF; // 0xff means forever
	UCHAR small_motor_on = 0x00; // 0 or 1 (off/on)
	UCHAR large_motor_duration = 0xFF; // 0xff means forever
	UCHAR large_motor_force = 0x00; // 0 to 255
};

struct ds3_led
{
	UCHAR duration = 0xFF; // total duration, 0xff means forever
	UCHAR interval_duration = 0xFF; // interval duration in deciseconds
	UCHAR enabled = 0x10;
	UCHAR interval_portion_off = 0x00; // in percent (100% = 0xFF)
	UCHAR interval_portion_on = 0xFF; // in percent (100% = 0xFF)
};

struct ds3_output_report
{
	UCHAR report_id = 0x00;
	UCHAR idk_what_this_is[3] = { 0x02, 0x00, 0x00 };
	ds3_rumble rumble;
	UCHAR padding[4] = { 0x00, 0x00, 0x00, 0x00 };
	UCHAR led_enabled = 0x00; // LED 1 = 0x02, LED 2 = 0x04, etc.
	ds3_led led[4];
	ds3_led led_5; // reserved for another LED
};

#pragma endregion

#pragma region Utility functions

SHORT ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto intValue = value - 0x80;
	if (intValue == -128) intValue = -127;

	auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

float ClampAxis(float value)
{
	if (value > 1.0f) return 1.0f;
	if (value < -1.0f) return -1.0f;
	return value;
}

float ToAxis(UCHAR value)
{
	return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
}

#pragma endregion

void SetDeviceDisconnected(DWORD UserIndex)
{
	if (UserIndex >= DS3_DEVICES_MAX)
		return;

	const auto state = &g_deviceStates[UserIndex];

	state->isConnected = false;
	state->packetNumber = 0;
	memset(&state->lastReport, 0, sizeof(DS3_RAW_INPUT_REPORT));

	if (state->deviceHandle)
	{
		hid_close(state->deviceHandle);
		state->deviceHandle = nullptr;
	}
}

bool GetDeviceHandle(DWORD UserIndex, hid_device** Handle)
{
	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	bool result = false;
	const auto state = &g_deviceStates[UserIndex];
	hid_device* device = nullptr;
	struct hid_device_info* devs = nullptr, * cur_dev;
	DWORD index = 0;

	do
	{
		if (state->isConnected)
		{
			if (Handle)
				*Handle = state->deviceHandle;
			result = true;
			break;
		}

		memset(&state->lastReport, 0, sizeof(DS3_RAW_INPUT_REPORT));
		state->packetNumber = 0;

		//
		// Look for device of interest
		// 
		devs = hid_enumerate(DS3_VID, DS3_PID);

		if (devs == nullptr)
			return false;

		cur_dev = devs;
		while (cur_dev)
		{
			if (index++ == UserIndex)
				break;

			cur_dev = cur_dev->next;
		}

		if (cur_dev == nullptr)
		{
			state->deviceHandle = nullptr;
			state->isConnected = false;
			break;
		}

		device = hid_open_path(cur_dev->path);

		if (device == nullptr)
		{
			state->deviceHandle = nullptr;
			state->isConnected = false;
			break;
		}

		if (Handle)
			*Handle = device;

		state->deviceHandle = device;
		state->isConnected = true;
		result = true;

	} while (FALSE);

	if (devs)
		hid_free_enumeration(devs);

	return result;
}

bool GetPacketNumber(DWORD UserIndex, PDS3_RAW_INPUT_REPORT Report, DWORD* PacketNumber)
{
	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	if (!PacketNumber || !Report)
		return false;

	const auto state = &g_deviceStates[UserIndex];

	//
	// Exclude noisy motion stuff from comparison
	// 
	constexpr size_t bytesToCompare = sizeof(DS3_RAW_INPUT_REPORT) - 18;

	//
	// Only increment when a change happened
	// 
	if (memcmp(
		&state->lastReport,
		Report,
		bytesToCompare
	) != 0)
	{
		state->packetNumber++;
		memcpy(&state->lastReport, Report, sizeof(DS3_RAW_INPUT_REPORT));
	}

	*PacketNumber = state->packetNumber;

	return true;
}


XINPUTBRIDGE_API DWORD WINAPI XInputGetExtended(
	_In_ DWORD dwUserIndex,
	_Out_ SCP_EXTN* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

	do
	{
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!GetDeviceHandle(dwUserIndex, &device))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res == 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		RtlZeroMemory(pState, sizeof(SCP_EXTN));

		//
		// D-Pad
		// 
		pState->SCP_UP = static_cast<float>(pReport->Pressure.Values.Up) / static_cast<float>(UCHAR_MAX);
		pState->SCP_RIGHT = static_cast<float>(pReport->Pressure.Values.Right) / static_cast<float>(UCHAR_MAX);
		pState->SCP_DOWN = static_cast<float>(pReport->Pressure.Values.Down) / static_cast<float>(UCHAR_MAX);
		pState->SCP_LEFT = static_cast<float>(pReport->Pressure.Values.Left) / static_cast<float>(UCHAR_MAX);

		//
		// Start/Select
		// 
		pState->SCP_START = pReport->Buttons.Individual.Start ? 1.0f : 0.0f;
		pState->SCP_SELECT = pReport->Buttons.Individual.Select ? 1.0f : 0.0f;

		//
		// Thumbs
		// 
		pState->SCP_L3 = pReport->Buttons.Individual.L3 ? 1.0f : 0.0f;
		pState->SCP_R3 = pReport->Buttons.Individual.R3 ? 1.0f : 0.0f;

		//
		// Shoulders
		// 
		pState->SCP_L1 = static_cast<float>(pReport->Pressure.Values.L1) / static_cast<float>(UCHAR_MAX);
		pState->SCP_R1 = static_cast<float>(pReport->Pressure.Values.R1) / static_cast<float>(UCHAR_MAX);

		//
		// Face buttons
		// 
		pState->SCP_T = static_cast<float>(pReport->Pressure.Values.Triangle) / static_cast<float>(UCHAR_MAX);
		pState->SCP_C = static_cast<float>(pReport->Pressure.Values.Circle) / static_cast<float>(UCHAR_MAX);
		pState->SCP_X = static_cast<float>(pReport->Pressure.Values.Cross) / static_cast<float>(UCHAR_MAX);
		pState->SCP_S = static_cast<float>(pReport->Pressure.Values.Square) / static_cast<float>(UCHAR_MAX);

		//
		// Triggers
		// 
		pState->SCP_L2 = static_cast<float>(pReport->Pressure.Values.L2) / static_cast<float>(UCHAR_MAX);
		pState->SCP_R2 = static_cast<float>(pReport->Pressure.Values.R2) / static_cast<float>(UCHAR_MAX);

		//
		// PS
		// 
		pState->SCP_PS = pReport->Buttons.Individual.PS ? 1.0f : 0.0f;

		//
		// Thumb axes
		//
		if (pReport->LeftThumbX < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->LeftThumbX >(UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_LX = ToAxis(pReport->LeftThumbX);
		if (pReport->LeftThumbY < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->LeftThumbY >(UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_LY = ToAxis(pReport->LeftThumbY) * -1.0f;
		if (pReport->RightThumbX < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->RightThumbX >(UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_RX = ToAxis(pReport->RightThumbX);
		if (pReport->RightThumbY < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->RightThumbY >(UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_RY = ToAxis(pReport->RightThumbY) * -1.0f;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetState(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

	do
	{
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!GetDeviceHandle(dwUserIndex, &device))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		GetPacketNumber(dwUserIndex, pReport, &pState->dwPacketNumber);

		RtlZeroMemory(&pState->Gamepad, sizeof(pState->Gamepad));

		//
		// D-Pad translation
		// 
		switch (pReport->Buttons.bButtons[0] & ~0xF)
		{
		case 0x10: // N
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			break;
		case 0x30: // NE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x20: // E
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x60: // SE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0x40: // S
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0xC0: // SW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x80: // W
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x90: // NW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		default: // Released
			break;
		}

		//
		// Start/Select
		// 
		if (pReport->Buttons.Individual.Start)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_START;
		if (pReport->Buttons.Individual.Select)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_BACK;

		//
		// Thumbs
		// 
		if (pReport->Buttons.Individual.L3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if (pReport->Buttons.Individual.R3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		//
		// Shoulders
		// 
		if (pReport->Buttons.Individual.L1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (pReport->Buttons.Individual.R1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;

		//
		// Face buttons
		// 
		if (pReport->Buttons.Individual.Triangle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_Y;
		if (pReport->Buttons.Individual.Circle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_B;
		if (pReport->Buttons.Individual.Cross)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_A;
		if (pReport->Buttons.Individual.Square)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_X;

		//
		// Triggers
		// 
		pState->Gamepad.bLeftTrigger = pReport->Pressure.Values.L2;
		pState->Gamepad.bRightTrigger = pReport->Pressure.Values.R2;

		//
		// Thumb axes
		// 
		pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		pState->Gamepad.sThumbRY = ScaleDsToXi(pReport->RightThumbY, TRUE);

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputSetState(
	_In_ DWORD dwUserIndex,
	_In_ XINPUT_VIBRATION* pVibration
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

	do
	{
		//
		// User might troll us
		// 
		if (pVibration == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!GetDeviceHandle(dwUserIndex, &device))
			break;

		ds3_output_report output_report;

		output_report.rumble.small_motor_on = pVibration->wRightMotorSpeed > 0 ? 1 : 0;
		output_report.rumble.large_motor_force = static_cast<float>(pVibration->wLeftMotorSpeed) / static_cast<float>(
			USHRT_MAX) * static_cast<float>(UCHAR_MAX);

		switch (dwUserIndex)
		{
		case 0: output_report.led_enabled = 0b00000010;
			break;
		case 1: output_report.led_enabled = 0b00000100;
			break;
		case 2: output_report.led_enabled = 0b00001000;
			break;
		case 3: output_report.led_enabled = 0b00010000;
			break;
		case 4: output_report.led_enabled = 0b00010010;
			break;
		case 5: output_report.led_enabled = 0b00010100;
			break;
		case 6: output_report.led_enabled = 0b00011000;
			break;
		default:
			break;
		}

		const int res = hid_write(device, &output_report.report_id, sizeof(output_report));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetCapabilities(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlags,
	_Out_ XINPUT_CAPABILITIES* pCapabilities
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;

	do
	{
		//
		// User might troll us
		// 
		if (pCapabilities == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!GetDeviceHandle(dwUserIndex, nullptr))
			break;

		RtlZeroMemory(pCapabilities, sizeof(XINPUT_CAPABILITIES));

		pCapabilities->Type = XINPUT_DEVTYPE_GAMEPAD;
		pCapabilities->SubType = XINPUT_DEVSUBTYPE_GAMEPAD;
		pCapabilities->Flags += XINPUT_CAPS_FFB_SUPPORTED;

		pCapabilities->Gamepad.wButtons = (
			XINPUT_GAMEPAD_DPAD_UP |
			XINPUT_GAMEPAD_DPAD_DOWN |
			XINPUT_GAMEPAD_DPAD_LEFT |
			XINPUT_GAMEPAD_DPAD_RIGHT |
			XINPUT_GAMEPAD_START |
			XINPUT_GAMEPAD_BACK |
			XINPUT_GAMEPAD_LEFT_THUMB |
			XINPUT_GAMEPAD_RIGHT_THUMB |
			XINPUT_GAMEPAD_LEFT_SHOULDER |
			XINPUT_GAMEPAD_RIGHT_SHOULDER |
			XINPUT_GAMEPAD_A |
			XINPUT_GAMEPAD_B |
			XINPUT_GAMEPAD_X |
			XINPUT_GAMEPAD_Y
			);
		pCapabilities->Gamepad.bLeftTrigger = UCHAR_MAX;
		pCapabilities->Gamepad.bRightTrigger = UCHAR_MAX;
		pCapabilities->Gamepad.sThumbLX = 0xFFC0;
		pCapabilities->Gamepad.sThumbLY = 0xFFC0;
		pCapabilities->Gamepad.sThumbRX = 0xFFC0;
		pCapabilities->Gamepad.sThumbRY = 0xFFC0;

		pCapabilities->Vibration.wLeftMotorSpeed = UCHAR_MAX;
		pCapabilities->Vibration.wRightMotorSpeed = UCHAR_MAX;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API void WINAPI XInputEnable(
	_In_ BOOL enable
)
{
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetDSoundAudioDeviceGuids(
	DWORD dwUserIndex,
	GUID* pDSoundRenderGuid,
	GUID* pDSoundCaptureGuid
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetBatteryInformation(
	_In_ DWORD dwUserIndex,
	_In_ BYTE devType,
	_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetKeystroke(
	DWORD dwUserIndex,
	DWORD dwReserved,
	PXINPUT_KEYSTROKE pKeystroke
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetStateEx(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

	do
	{
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!GetDeviceHandle(dwUserIndex, &device))
			break;

		UCHAR buf[64];
		buf[0] = 0xF2;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		GetPacketNumber(dwUserIndex, pReport, &pState->dwPacketNumber);

		RtlZeroMemory(&pState->Gamepad, sizeof(pState->Gamepad));

		//
		// D-Pad translation
		// 
		switch (pReport->Buttons.bButtons[0] & ~0xF)
		{
		case 0x10: // N
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			break;
		case 0x30: // NE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x20: // E
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x60: // SE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0x40: // S
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0xC0: // SW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x80: // W
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x90: // NW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		default: // Released
			break;
		}

		//
		// Start/Select
		// 
		if (pReport->Buttons.Individual.Start)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_START;
		if (pReport->Buttons.Individual.Select)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_BACK;

		//
		// Thumbs
		// 
		if (pReport->Buttons.Individual.L3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if (pReport->Buttons.Individual.R3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		//
		// Shoulders
		// 
		if (pReport->Buttons.Individual.L1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (pReport->Buttons.Individual.R1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;

		//
		// Face buttons
		// 
		if (pReport->Buttons.Individual.Triangle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_Y;
		if (pReport->Buttons.Individual.Circle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_B;
		if (pReport->Buttons.Individual.Cross)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_A;
		if (pReport->Buttons.Individual.Square)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_X;

		//
		// Triggers
		// 
		pState->Gamepad.bLeftTrigger = pReport->Pressure.Values.L2;
		pState->Gamepad.bRightTrigger = pReport->Pressure.Values.R2;

		//
		// Thumb axes
		// 
		pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		pState->Gamepad.sThumbRY = ScaleDsToXi(pReport->RightThumbY, TRUE);

		//
		// PS/Guide
		// 
		pState->Gamepad.wButtons |= XINPUT_GAMEPAD_GUIDE;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputWaitForGuideButton(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlag,
	_In_ LPVOID pVoid
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputCancelGuideButtonWait(
	_In_ DWORD dwUserIndex
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputPowerOffController(
	_In_ DWORD dwUserIndex
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}
