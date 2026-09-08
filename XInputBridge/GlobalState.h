#pragma once

#include "Common.h"
#include <atomic>
#include "Macros.h"
#include "DeviceState.h"
#include "XInputBridge.h"

class GlobalState
{
public:
	void Initialize();
	void Destroy();

#pragma region XInput API proxies

	DWORD ProxyXInputGetExtended(
		_In_ DWORD dwUserIndex,
		_Out_ SCP_EXTN* pState
	);

	DWORD ProxyXInputGetState(
		_In_ DWORD dwUserIndex,
		_Out_ XINPUT_STATE* pState
	);

	DWORD ProxyXInputSetState(
		_In_ DWORD dwUserIndex,
		_In_ XINPUT_VIBRATION* pVibration
	);

	DWORD ProxyXInputGetCapabilities(
		_In_ DWORD dwUserIndex,
		_In_ DWORD dwFlags,
		_Out_ XINPUT_CAPABILITIES* pCapabilities
	);

	void ProxyXInputEnable(
		_In_ BOOL enable
	);

	DWORD ProxyXInputGetDSoundAudioDeviceGuids(
		DWORD dwUserIndex,
		GUID* pDSoundRenderGuid,
		GUID* pDSoundCaptureGuid
	);

	DWORD ProxyXInputGetBatteryInformation(
		_In_ DWORD dwUserIndex,
		_In_ BYTE devType,
		_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
	);

	DWORD ProxyXInputGetKeystroke(
		DWORD dwUserIndex,
		DWORD dwReserved,
		PXINPUT_KEYSTROKE pKeystroke
	);

	DWORD ProxyXInputGetStateEx(
		_In_ DWORD dwUserIndex,
		_Out_ XINPUT_STATE* pState
	);

	DWORD ProxyXInputWaitForGuideButton(
		_In_ DWORD dwUserIndex,
		_In_ DWORD dwFlag,
		_In_ LPVOID pVoid
	);

	DWORD ProxyXInputCancelGuideButtonWait(
		_In_ DWORD dwUserIndex
	);

	DWORD ProxyXInputPowerOffController(
		_In_ DWORD dwUserIndex
	);

#pragma endregion

	static std::optional<std::vector<std::wstring>> GetSymbolicLinksForDeviceInterfaceClass(const GUID* InterfaceGuid);
	static std::optional<std::wstring> InterfaceIdToInstanceId(const std::wstring& Symlink);
	static std::optional<std::vector<std::wstring>> GetDeviceChildren(const std::wstring& ParentDeviceId);
	static std::optional<std::vector<std::wstring>> InstanceIdToHidPaths(const std::wstring& InstanceId);
	static std::optional<uint8_t> GetDs3HidDeviceModeProperty(const std::wstring& Ds3InstanceId);

private:
	/** The states of each user index slot */
	std::vector<DeviceState> States{ DS3_DEVICES_MAX };
	/** The lock protecting modifying access to the states */
	SRWLOCK StatesLock{};
	/** Handle of the DS3 device notification */
	HCMNOTIFICATION Ds3NotificationHandle{};
	/** Handle of the XUSB device notification */
	HCMNOTIFICATION XusbNotificationHandle{};
	/** Handle of the startup finished event */
	HANDLE StartupFinishedEvent{ INVALID_HANDLE_VALUE };
	/** Background initialization thread */
	HANDLE InitThread{ nullptr };
	/** System XInput1_3.dll module */
	HMODULE SystemXInputModule{ nullptr };
	/** In-flight DS3 arrival workers */
	volatile LONG ArrivalWorkCount{ 0 };
	/** Set when async startup has signaled completion */
	std::atomic<bool> StartupReady{ false };
	/** Set when the DLL is shutting down */
	std::atomic<bool> ShuttingDown{ false };

	void WaitForStartup() const;
	void SignalStartupFinished();

	_Success_(return != NULL)
	_Must_inspect_result_
	DeviceState* GetNextFreeSlot(_Out_opt_ PULONG SlotIndex = nullptr);

	DeviceState* FindBySymbolicLink(const std::wstring& Symlink);
	DeviceState* GetXusbByUserIndex(DWORD UserIndex);
	DeviceState* GetXusbByRealUserIndex(DWORD UserIndex);

	_Success_(return != NULL)
	_Must_inspect_result_
	bool GetConnectedDs3ByUserIndex(_In_ DWORD UserIndex, _Out_opt_ DeviceState** Handle) const;

	void AssignPreparedDs3(const std::wstring& Symlink, DeviceState& Prepared);
	void AssignXusbDevice(const std::wstring& Symlink, DWORD UserIndex);

	void EnumerateDs3Devices();
	void EnumerateXusbDevices();
	void DisposeAllSlots();

	DWORD ReadDs3XInputState(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState, _In_ bool includeGuide);

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

	_Success_(return)
	_Must_inspect_result_
	static bool SymlinkToUserIndex(_In_ PCWSTR Symlink, _Out_ PDWORD UserIndex);

	static DWORD WINAPI InitAsync(_In_ LPVOID lpParameter);
};
