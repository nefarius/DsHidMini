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

	return CALL_FPN_SAFE(FpnXInputGetDSoundAudioDeviceGuids, dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
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

	return CALL_FPN_SAFE(FpnXInputGetBatteryInformation, dwUserIndex, devType, pBatteryInformation);
}

DWORD GlobalState::ProxyXInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputGetKeystroke, dwUserIndex, dwReserved, pKeystroke);
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

	return CALL_FPN_SAFE(FpnXInputWaitForGuideButton, dwUserIndex, dwFlag, pVoid);
}

DWORD GlobalState::ProxyXInputCancelGuideButtonWait(DWORD dwUserIndex)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputCancelGuideButtonWait, dwUserIndex);
}

DWORD GlobalState::ProxyXInputPowerOffController(DWORD dwUserIndex)
{
	AcquireSRWLockShared(&this->StatesLock);
	absl::Cleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	return CALL_FPN_SAFE(FpnXInputPowerOffController, dwUserIndex);
}
