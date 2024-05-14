#include "GlobalState.h"
#include <absl/cleanup/cleanup.h>


DWORD GlobalState::ProxyXInputGetExtended(DWORD dwUserIndex, SCP_EXTN* pState)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return -1;
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
