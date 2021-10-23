// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "XInputBridge.h"


SHORT ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto intValue = value - 0x80;
	if (intValue == -128) intValue = -127;

	auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetExtended(
	_In_ DWORD dwUserIndex,
	_Out_ SCP_EXTN* pState
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetState(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;
	struct hid_device_info* devs = nullptr, * cur_dev;
	DWORD index = 0;

	do {
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		devs = hid_enumerate(0x054C, 0x0268);
		cur_dev = devs;
		while (cur_dev)
		{
			if (index++ == dwUserIndex)
				break;

			cur_dev = cur_dev->next;
		}

		if (cur_dev == nullptr)
			break;

		device = hid_open_path(cur_dev->path);

		if (device == nullptr)
			break;

		UCHAR buf[64];
		buf[0] = 0xF2;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res == 0)
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		pState->dwPacketNumber++;
		RtlZeroMemory(&pState->Gamepad, sizeof(pState->Gamepad));
		//pState->Gamepad.wButtons &= ~0xF; // Clear lower 4 bits

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

	if (devs)
		hid_free_enumeration(devs);

	if (device)
		hid_close(device);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputSetState(
	_In_ DWORD dwUserIndex,
	_In_ XINPUT_VIBRATION* pVibration
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetCapabilities(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlags,
	_Out_ XINPUT_CAPABILITIES* pCapabilities
)
{
	return ERROR_DEVICE_NOT_CONNECTED;
}

XINPUTBRIDGE_API void WINAPI XInputEnable(
	_In_ BOOL enable
)
{
	return;
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
	return ERROR_DEVICE_NOT_CONNECTED;
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
