// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "Common.h"
#include "XInputBridge.h"
#include "GlobalState.h"

GlobalState G_State{};


#pragma region Public exports

//
// Public API exports
// 

XINPUTBRIDGE_API DWORD WINAPI XInputGetExtended(
	_In_ DWORD dwUserIndex,
	_Out_ SCP_EXTN* pState
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputGetExtended(dwUserIndex, pState);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetState(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputGetState(dwUserIndex, pState);
}

XINPUTBRIDGE_API DWORD WINAPI XInputSetState(
	_In_ DWORD dwUserIndex,
	_In_ XINPUT_VIBRATION* pVibration
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scoped_span = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputSetState(dwUserIndex, pVibration);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetCapabilities(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlags,
	_Out_ XINPUT_CAPABILITIES* pCapabilities
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
}

XINPUTBRIDGE_API void WINAPI XInputEnable(
	_In_ BOOL enable
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__));
#endif

	G_State.ProxyXInputEnable(enable);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetDSoundAudioDeviceGuids(
	DWORD dwUserIndex,
	GUID* pDSoundRenderGuid,
	GUID* pDSoundCaptureGuid
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetDSoundAudioDeviceGuids(dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetBatteryInformation(
	_In_ DWORD dwUserIndex,
	_In_ BYTE devType,
	_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetBatteryInformation(dwUserIndex, devType, pBatteryInformation);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetKeystroke(
	DWORD dwUserIndex,
	DWORD dwReserved,
	PXINPUT_KEYSTROKE pKeystroke
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetKeystroke(dwUserIndex, dwReserved, pKeystroke);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetStateEx(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputGetStateEx(dwUserIndex, pState);
}

XINPUTBRIDGE_API DWORD WINAPI XInputWaitForGuideButton(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlag,
	_In_ LPVOID pVoid
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputWaitForGuideButton(dwUserIndex, dwFlag, pVoid);
}

XINPUTBRIDGE_API DWORD WINAPI XInputCancelGuideButtonWait(
	_In_ DWORD dwUserIndex
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputCancelGuideButtonWait(dwUserIndex);
}

XINPUTBRIDGE_API DWORD WINAPI XInputPowerOffController(
	_In_ DWORD dwUserIndex
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GlobalState::GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputPowerOffController(dwUserIndex);
}

#pragma endregion
