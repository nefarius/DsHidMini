#include "GlobalState.h"
#include <absl/cleanup/cleanup.h>


DWORD GlobalState::ProxyXInputGetExtended(DWORD dwUserIndex, SCP_EXTN* pState)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

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
		if (!this->GetDs3ByUserIndex(dwUserIndex, &state))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(state->Ds3Device, buf, ARRAYSIZE(buf));

		if (res == 0)
			break;

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
			|| pReport->LeftThumbX > (UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_LX = ToAxis(pReport->LeftThumbX);
		if (pReport->LeftThumbY < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->LeftThumbY > (UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_LY = ToAxis(pReport->LeftThumbY) * -1.0f;
		if (pReport->RightThumbX < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->RightThumbX > (UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_RX = ToAxis(pReport->RightThumbX);
		if (pReport->RightThumbY < (UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET
			|| pReport->RightThumbY > (UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET)
			pState->SCP_RY = ToAxis(pReport->RightThumbY) * -1.0f;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

DWORD GlobalState::ProxyXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

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
		if (!this->GetDs3ByUserIndex(dwUserIndex, &state))
		{
			status = CALL_FPN_SAFE(FpnXInputGetState, dwUserIndex, pState);
			break;
		}

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(state->Ds3Device, buf, ARRAYSIZE(buf));

		if (res <= 0)
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		if (!state->Ds3GetPacketNumber(pReport, &pState->dwPacketNumber))
			break;

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

DWORD GlobalState::ProxyXInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

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
		if (!this->GetDs3ByUserIndex(dwUserIndex, &state))
		{
			status = CALL_FPN_SAFE(FpnXInputSetState, dwUserIndex, pVibration);
			break;
		}

		ds3_output_report outputReport;

		// ReSharper disable CppAssignedValueIsNeverUsed
		outputReport.rumble.small_motor_on = pVibration->wRightMotorSpeed > 0 ? 1 : 0;
		outputReport.rumble.large_motor_force = static_cast<float>(pVibration->wLeftMotorSpeed) / static_cast<float>(
			USHRT_MAX) * static_cast<float>(UCHAR_MAX);

		switch (dwUserIndex)
		{
		case 0:
			outputReport.led_enabled = 0b00000010;
			break;
		case 1:
			outputReport.led_enabled = 0b00000100;
			break;
		case 2:
			outputReport.led_enabled = 0b00001000;
			break;
		case 3:
			outputReport.led_enabled = 0b00010000;
			break;
		case 4:
			outputReport.led_enabled = 0b00010010;
			break;
		case 5:
			outputReport.led_enabled = 0b00010100;
			break;
		case 6:
			outputReport.led_enabled = 0b00011000;
			break;
		default:
			break;
		}
		// ReSharper restore CppAssignedValueIsNeverUsed

		const int res = hid_write(state->Ds3Device, &outputReport.report_id, sizeof(outputReport));

		if (res <= 0)
			break;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

DWORD GlobalState::ProxyXInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputGetCapabilities, dwUserIndex, dwFlags, pCapabilities);
}

void GlobalState::ProxyXInputEnable(BOOL enable) const
{
	CALL_FPN_SAFE_NO_RETURN(FpnXInputEnable, enable);
}

DWORD GlobalState::ProxyXInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetDSoundAudioDeviceGuids, state->RealUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetBatteryInformation(DWORD dwUserIndex,
                                                    BYTE devType,
                                                    XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetBatteryInformation, state->RealUserIndex, devType, pBatteryInformation);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetKeystroke, state->RealUserIndex, dwReserved, pKeystroke);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetStateEx(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputGetStateEx, dwUserIndex, pState);
}

DWORD GlobalState::ProxyXInputWaitForGuideButton(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputWaitForGuideButton, state->RealUserIndex, dwFlag, pVoid);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputCancelGuideButtonWait(DWORD dwUserIndex)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputCancelGuideButtonWait, state->RealUserIndex);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputPowerOffController(DWORD dwUserIndex)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputPowerOffController, state->RealUserIndex);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}
