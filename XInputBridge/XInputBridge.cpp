// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "XInputBridge.h"




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
	return ERROR_DEVICE_NOT_CONNECTED;
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
