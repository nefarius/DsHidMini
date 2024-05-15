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
		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(state->HidDeviceHandle, buf, ARRAYSIZE(buf));

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
		if (IS_OUTSIDE_DZ(pReport->LeftThumbX))
			pState->SCP_LX = ToAxis(pReport->LeftThumbX);
		if (IS_OUTSIDE_DZ(pReport->LeftThumbY))
			pState->SCP_LY = ToAxis(pReport->LeftThumbY) * -1.0f;
		if (IS_OUTSIDE_DZ(pReport->RightThumbX))
			pState->SCP_RX = ToAxis(pReport->RightThumbX);
		if (IS_OUTSIDE_DZ(pReport->RightThumbY))
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
		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
		{
			if ((state = GetXusbByUserIndex(dwUserIndex)))
			{
				status = CALL_FPN_SAFE(FpnXInputGetState, state->RealUserIndex, pState);
			}

			break;
		}

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(state->HidDeviceHandle, buf, ARRAYSIZE(buf));

		if (res <= 0)
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		// invalid packet, discard
		if (pReport->BatteryStatus == 0)
			break;

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
		if (IS_OUTSIDE_DZ(pReport->LeftThumbX))
			pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		if (IS_OUTSIDE_DZ(pReport->LeftThumbY))
			pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		if (IS_OUTSIDE_DZ(pReport->RightThumbX))
			pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		if (IS_OUTSIDE_DZ(pReport->RightThumbY))
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
		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
		{
			if ((state = GetXusbByUserIndex(dwUserIndex)))
			{
				status = CALL_FPN_SAFE(FpnXInputSetState, state->RealUserIndex, pVibration);
			}

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

		const int res = hid_write(state->HidDeviceHandle, &outputReport.report_id, sizeof(outputReport));

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

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;

#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

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
		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, nullptr))
		{
			status = CALL_FPN_SAFE(FpnXInputGetCapabilities, dwUserIndex, dwFlags, pCapabilities);
			break;
		}

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
		pCapabilities->Gamepad.sThumbLX = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbLY = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbRX = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbRY = static_cast<SHORT>(0xFFC0);

		pCapabilities->Vibration.wLeftMotorSpeed = UCHAR_MAX;
		pCapabilities->Vibration.wRightMotorSpeed = UCHAR_MAX;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
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
		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
		{
			if ((state = GetXusbByUserIndex(dwUserIndex)))
			{
				status = CALL_FPN_SAFE(FpnXInputGetStateEx, state->RealUserIndex, pState);
			}

			break;
		}

		UCHAR buf[64];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(state->HidDeviceHandle, buf, ARRAYSIZE(buf));

		if (res <= 0)
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		// invalid packet, discard
		if (pReport->BatteryStatus == 0)
			break;

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
		if (IS_OUTSIDE_DZ(pReport->LeftThumbX))
			pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		if (IS_OUTSIDE_DZ(pReport->LeftThumbY))
			pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		if (IS_OUTSIDE_DZ(pReport->RightThumbX))
			pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		if (IS_OUTSIDE_DZ(pReport->RightThumbY))
			pState->Gamepad.sThumbRY = ScaleDsToXi(pReport->RightThumbY, TRUE);

		//
		// PS/Guide
		// 
		pState->Gamepad.wButtons |= XINPUT_GAMEPAD_GUIDE;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
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
