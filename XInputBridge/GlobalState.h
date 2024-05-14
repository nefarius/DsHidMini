#pragma once

#include <Windows.h>
#include <cfgmgr32.h>
#include <optional>

#include <vector>

#include "Macros.h"
#include "DeviceState.h"
#include <DsHidMini/ScpTypes.h>
#include "XInputBridge.h"

class GlobalState
{
public:
	void Initialize();
	void Destroy();

#pragma region XInput API proxies

	DWORD RealXInputGetState(
		_In_ DWORD dwUserIndex,
		_Out_ XINPUT_STATE* pState
	) const;

	DWORD RealXInputSetState(
		_In_ DWORD dwUserIndex,
		_In_ XINPUT_VIBRATION* pVibration
	) const;

	DWORD RealXInputGetCapabilities(
		_In_ DWORD dwUserIndex,
		_In_ DWORD dwFlags,
		_Out_ XINPUT_CAPABILITIES* pCapabilities
	) const;

	void RealXInputEnable(
		_In_ BOOL enable
	) const;

	DWORD RealXInputGetDSoundAudioDeviceGuids(
		DWORD dwUserIndex,
		GUID* pDSoundRenderGuid,
		GUID* pDSoundCaptureGuid
	) const;

	DWORD RealXInputGetBatteryInformation(
		_In_ DWORD dwUserIndex,
		_In_ BYTE devType,
		_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
	) const;

	DWORD RealXInputGetKeystroke(
		DWORD dwUserIndex,
		DWORD dwReserved,
		PXINPUT_KEYSTROKE pKeystroke
	) const;

	DWORD RealXInputGetStateEx(
		_In_ DWORD dwUserIndex,
		_Out_ XINPUT_STATE* pState
	) const;

	DWORD RealXInputWaitForGuideButton(
		_In_ DWORD dwUserIndex,
		_In_ DWORD dwFlag,
		_In_ LPVOID pVoid
	) const;

	DWORD RealXInputCancelGuideButtonWait(
		_In_ DWORD dwUserIndex
	) const;

	DWORD RealXInputPowerOffController(
		_In_ DWORD dwUserIndex
	) const;

#pragma endregion

private:
	std::vector<DeviceState> States{ DS3_DEVICES_MAX };
	SRWLOCK StatesLock{};
	HCMNOTIFICATION Ds3NotificationHandle{};
	HCMNOTIFICATION XusbNotificationHandle{};

	DeviceState* GetFreeState();

#pragma region XInput declarations

		decltype(XInputGetState)* FpnXInputGetState = nullptr;
	decltype(XInputSetState)* FpnXInputSetState = nullptr;
	decltype(XInputGetCapabilities)* FpnXInputGetCapabilities = nullptr;
	decltype(XInputEnable)* FpnXInputEnable = nullptr;
	decltype(XInputGetDSoundAudioDeviceGuids)* FpnXInputGetDSoundAudioDeviceGuids = nullptr;
	decltype(XInputGetBatteryInformation)* FpnXInputGetBatteryInformation = nullptr;
	decltype(XInputGetKeystroke)* FpnXInputGetKeystroke = nullptr;
	decltype(XInputGetStateEx)* FpnXInputGetStateEx = nullptr;
	decltype(XInputWaitForGuideButton)* FpnXInputWaitForGuideButton = nullptr;
	decltype(XInputCancelGuideButtonWait)* FpnXInputCancelGuideButtonWait = nullptr;
	decltype(XInputPowerOffController)* FpnXInputPowerOffController = nullptr;

#pragma endregion

	static DWORD CALLBACK DeviceNotificationCallback(
		_In_ HCMNOTIFICATION hNotify,
		_In_opt_ PVOID Context,
		_In_ CM_NOTIFY_ACTION Action,
		_In_reads_bytes_(EventDataSize) PCM_NOTIFY_EVENT_DATA EventData,
		_In_ DWORD EventDataSize
	);

	static bool SymlinkToUserIndex(_In_ PCWSTR Symlink, _Inout_ PDWORD UserIndex);

	static DWORD WINAPI InitAsync(_In_ LPVOID lpParameter);

	//friend class DeviceState;
};
