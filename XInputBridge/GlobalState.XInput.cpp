#include "GlobalState.h"

DWORD GlobalState::RealXInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState) const
{
	return CALL_FPN_SAFE(FpnXInputGetState, dwUserIndex, pState);
}

DWORD GlobalState::RealXInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration) const
{
	return CALL_FPN_SAFE(FpnXInputSetState, dwUserIndex, pVibration);
}

DWORD GlobalState::RealXInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities) const
{
	return CALL_FPN_SAFE(FpnXInputGetCapabilities, dwUserIndex, dwFlags, pCapabilities);
}

void GlobalState::RealXInputEnable(BOOL enable) const
{
	CALL_FPN_SAFE_NO_RETURN(FpnXInputEnable, enable);
}

DWORD GlobalState::RealXInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid) const
{
	return CALL_FPN_SAFE(FpnXInputGetDSoundAudioDeviceGuids, dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
}

DWORD GlobalState::RealXInputGetBatteryInformation(DWORD dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation) const
{
	return CALL_FPN_SAFE(FpnXInputGetBatteryInformation, dwUserIndex, devType, pBatteryInformation);
}

DWORD GlobalState::RealXInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke) const
{
	return CALL_FPN_SAFE(FpnXInputGetKeystroke, dwUserIndex, dwReserved, pKeystroke);
}

DWORD GlobalState::RealXInputGetStateEx(DWORD dwUserIndex, XINPUT_STATE* pState) const
{
	return CALL_FPN_SAFE(FpnXInputGetStateEx, dwUserIndex, pState);
}

DWORD GlobalState::RealXInputWaitForGuideButton(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid) const
{
	return CALL_FPN_SAFE(FpnXInputWaitForGuideButton, dwUserIndex, dwFlag, pVoid);
}

DWORD GlobalState::RealXInputCancelGuideButtonWait(DWORD dwUserIndex) const
{
	return CALL_FPN_SAFE(FpnXInputCancelGuideButtonWait, dwUserIndex);
}

DWORD GlobalState::RealXInputPowerOffController(DWORD dwUserIndex) const
{
	return CALL_FPN_SAFE(FpnXInputPowerOffController, dwUserIndex);
}
