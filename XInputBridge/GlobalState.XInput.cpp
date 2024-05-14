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
	hid_device* device = nullptr;

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
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!this->GetDs3DeviceByUserIndex(dwUserIndex, &device))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

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

	return CALL_FPN_SAFE(FpnXInputGetState, dwUserIndex, pState);
}

DWORD GlobalState::ProxyXInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputSetState, dwUserIndex, pVibration);
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
