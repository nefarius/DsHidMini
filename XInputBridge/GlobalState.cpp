#include "GlobalState.h"

#include <memory>
#include <spdlog/logger.h>
#include <spdlog/spdlog.h>
#include <scope_guard.hpp>
#include <Shlwapi.h>
#include <hidapi/hidapi.h>
#include <spdlog/sinks/basic_file_sink.h>

#include "Types.h"
#include "UniUtil.h"
#include "DsHidMini/dshmguid.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

void GlobalState::Initialize()
{
	InitializeCriticalSection(&StatesLock);

	CHAR dllPath[MAX_PATH];

	GetModuleFileNameA((HINSTANCE)&__ImageBase, dllPath, MAX_PATH);
	PathRemoveFileSpecA(dllPath);
	const auto dllDir = std::string(dllPath);

	auto logger = spdlog::basic_logger_mt(
		LOGGER_NAME,
		dllDir + "\\XInputBridge.log"
	);

#if _DEBUG
	spdlog::set_level(spdlog::level::debug);
	logger->flush_on(spdlog::level::debug);
#else
	logger->flush_on(spdlog::level::info);
#endif

	set_default_logger(logger);

	/*for (auto& state : G_DEVICE_STATES)
	{
		InitializeCriticalSection(&state.lock);
		state.isInitialized = true;
	}*/

	//
	// Call stuff that must not be done in DllMain in the background
	// 
	const HANDLE hThread = CreateThread(nullptr, 0, InitAsync, this, 0, nullptr);

	if (hThread == nullptr)
	{
		logger->error("Failed to create thread");
	}
}

void GlobalState::Destroy()
{
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	logger->info("Library getting unloaded");

	(void)CM_Unregister_Notification(Ds3NotificationHandle);
	(void)CM_Unregister_Notification(XusbNotificationHandle);

	(void)hid_exit();

	DeleteCriticalSection(&StatesLock);
}

DWORD GlobalState::DeviceNotificationCallback(
	HCMNOTIFICATION hNotify,
	PVOID Context,
	CM_NOTIFY_ACTION Action,
	PCM_NOTIFY_EVENT_DATA EventData,
	DWORD EventDataSize
)
{
	UNREFERENCED_PARAMETER(hNotify);
	UNREFERENCED_PARAMETER(EventDataSize);

	const GlobalState* _this = static_cast<GlobalState*>(Context);
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	switch (Action)
	{
	case CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL:
		if (IsEqualGUID(GUID_DEVINTERFACE_DSHIDMINI, EventData->u.DeviceInterface.ClassGuid))
		{
			logger->info("New DS3 device arrived: {}", ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink));
		}

		if (IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const auto symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);

			logger->info("New XUSB device arrived: {}", symlink);

			DWORD userIndex = INVALID_X_INPUT_USER_ID;
			if (SymlinkToUserIndex(EventData->u.DeviceInterface.SymbolicLink, &userIndex))
			{
				logger->info("User index: {}", userIndex);

				/*if (const auto slot = GetFreeSlot())
				{
					slot->SetOnlineAsXusb(symlink, userIndex);
				}
				else
				{
					logger->warn("No free slot for {}", symlink);
				}*/
			}
			else
			{
				logger->error("User index lookup failed");
			}
		}
		break;
	case CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL:
		if (IsEqualGUID(GUID_DEVINTERFACE_DSHIDMINI, EventData->u.DeviceInterface.ClassGuid))
		{
			const std::string symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);
			logger->info("DS3 device got removed: {}", symlink);

			/*const auto item = std::ranges::find_if(G_DEVICE_STATES, [symlink](const XI_DEVICE_STATE& element)
			{
				return absl::EqualsIgnoreCase(element.SymbolicLink, symlink);
			});

			if (item != G_DEVICE_STATES.end())
			{
				item->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
			}*/
		}

		if (IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const std::string symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);
			logger->info("XUSB device got removed: {}", symlink);

			/*const auto item = std::ranges::find_if(G_DEVICE_STATES, [symlink](const XI_DEVICE_STATE& element)
			{
				return absl::EqualsIgnoreCase(element.SymbolicLink, symlink);
			});

			if (item != G_DEVICE_STATES.end())
			{
				item->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
				item->Backend.UserIndex = K_INVALID_X_INPUT_USER_ID;
			}*/
		}
		break;
	default:
		return ERROR_SUCCESS;
	}

	return ERROR_SUCCESS;
}

//
// https://github.com/DJm00n/RawInputDemo/blob/master/RawInputLib/RawInputDeviceHid.cpp#L275-L339
// 
bool GlobalState::SymlinkToUserIndex(PCWSTR Symlink, PDWORD UserIndex)
{
	constexpr DWORD desiredAccess = (GENERIC_WRITE | GENERIC_READ);
	constexpr DWORD shareMode = FILE_SHARE_READ | FILE_SHARE_WRITE;

	HANDLE handle = CreateFileW(
		Symlink,
		desiredAccess,
		shareMode,
		nullptr,
		OPEN_EXISTING,
		0,
		nullptr
	);

	const auto guard = sg::make_scope_guard([handle]() noexcept
	{
		if (handle != INVALID_HANDLE_VALUE)
			CloseHandle(handle);
	});

	std::array<uint8_t, 3> gamepadStateRequest0101{ 0x01, 0x01, 0x00 };
	std::array<uint8_t, 3> ledStateData{};
	DWORD len = 0;

	// https://github.com/nefarius/XInputHooker/issues/1
	// https://gist.github.com/mmozeiko/b8ccc54037a5eaf35432396feabbe435
	constexpr DWORD IOCTL_XUSB_GET_LED_STATE = 0x8000E008;

	if (!DeviceIoControl(handle,
		IOCTL_XUSB_GET_LED_STATE,
		gamepadStateRequest0101.data(),
		// ReSharper disable once CppRedundantCastExpression
		(DWORD)gamepadStateRequest0101.size(),
		ledStateData.data(),
		// ReSharper disable once CppRedundantCastExpression
		(DWORD)ledStateData.size(),
		&len,
		nullptr
	))
	{
		// GetLastError()
		return false;
	}

	// https://www.partsnotincluded.com/xbox-360-controller-led-animations-info/
	// https://github.com/paroj/xpad/blob/5978d1020344c3288701ef70ea9a54dfc3312733/xpad.c#L1382-L1402
	constexpr uint8_t XINPUT_LED_TO_PORT_MAP[] =
	{
		INVALID_X_INPUT_USER_ID, // All off
		INVALID_X_INPUT_USER_ID, // All blinking, then previous setting
		0, // 1 flashes, then on
		1, // 2 flashes, then on
		2, // 3 flashes, then on
		3, // 4 flashes, then on
		0, // 1 on
		1, // 2 on
		2, // 3 on
		3, // 4 on
		INVALID_X_INPUT_USER_ID, // Rotate
		INVALID_X_INPUT_USER_ID, // Blink, based on previous setting
		INVALID_X_INPUT_USER_ID, // Slow blink, based on previous setting
		INVALID_X_INPUT_USER_ID, // Rotate with two lights
		INVALID_X_INPUT_USER_ID, // Persistent slow all blink
		INVALID_X_INPUT_USER_ID, // Blink once, then previous setting
	};

	const uint8_t ledState = ledStateData[2];

	*UserIndex = XINPUT_LED_TO_PORT_MAP[ledState];

	return true;
}

DWORD WINAPI GlobalState::InitAsync(LPVOID lpParameter)
{
	const auto _this = static_cast<GlobalState*>(lpParameter);
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	logger->info("Async library startup initialized");

	CHAR systemDir[MAX_PATH] = {};

	if (GetSystemDirectoryA(systemDir, MAX_PATH) == 0)
	{
		logger->error("GetSystemDirectoryA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	CHAR fullXiPath[MAX_PATH] = {};

	if (PathCombineA(fullXiPath, systemDir, XI_SYSTEM_LIB_NAME) == nullptr)
	{
		logger->error("PathCombineA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	const HMODULE xiLib = LoadLibraryA(fullXiPath);

	if (xiLib == nullptr)
	{
		logger->error("LoadLibraryA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	//
	// Grab the function pointers from the OS-provided exports
	// 

	_this->FpnXInputGetState = reinterpret_cast<decltype(XInputGetState)*>(GetProcAddress(xiLib,
		NAMEOF(XInputGetState)
	));
	_this->FpnXInputSetState = reinterpret_cast<decltype(XInputSetState)*>(GetProcAddress(xiLib,
		NAMEOF(XInputSetState)
	));
	_this->FpnXInputGetCapabilities = reinterpret_cast<decltype(XInputGetCapabilities)*>(GetProcAddress(xiLib,
		NAMEOF(XInputGetCapabilities)
	));
	_this->FpnXInputEnable = reinterpret_cast<decltype(XInputEnable)*>(GetProcAddress(xiLib,
		NAMEOF(XInputEnable)
	));
	_this->FpnXInputGetDSoundAudioDeviceGuids = reinterpret_cast<decltype(XInputGetDSoundAudioDeviceGuids)*>(GetProcAddress(xiLib,
		NAMEOF(XInputGetDSoundAudioDeviceGuids)
	));
	_this->FpnXInputGetBatteryInformation = reinterpret_cast<decltype(XInputGetBatteryInformation)*>(GetProcAddress(xiLib,
		NAMEOF(XInputGetBatteryInformation)
	));
	_this->FpnXInputGetKeystroke = reinterpret_cast<decltype(XInputGetKeystroke)*>(GetProcAddress(xiLib,
		NAMEOF(XInputGetKeystroke)
	));
	_this->FpnXInputGetStateEx = reinterpret_cast<decltype(XInputGetStateEx)*>(GetProcAddress(xiLib,
		MAKEINTRESOURCEA(100)
	));
	_this->FpnXInputWaitForGuideButton = reinterpret_cast<decltype(XInputWaitForGuideButton)*>(GetProcAddress(xiLib,
		MAKEINTRESOURCEA(101)
	));
	_this->FpnXInputCancelGuideButtonWait = reinterpret_cast<decltype(XInputCancelGuideButtonWait)*>(GetProcAddress(xiLib,
		MAKEINTRESOURCEA(102)
	));
	_this->FpnXInputPowerOffController = reinterpret_cast<decltype(XInputPowerOffController)*>(GetProcAddress(xiLib,
		MAKEINTRESOURCEA(103)
	));

	//
	// Register notifications for device arrival/removal
	// 

	CM_NOTIFY_FILTER ds3Filter = {};
	ds3Filter.cbSize = sizeof(CM_NOTIFY_FILTER);
	ds3Filter.FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
	ds3Filter.u.DeviceInterface.ClassGuid = GUID_DEVINTERFACE_DSHIDMINI;

	//
	// Register DsHidMini device interface
	// 
	CONFIGRET ret = CM_Register_Notification(&ds3Filter, nullptr, DeviceNotificationCallback, &_this->Ds3NotificationHandle);

	if (ret != CR_SUCCESS)
	{
		logger->error("CM_Register_Notification (DS3) failed: {:#x}", ret);
	}

	CM_NOTIFY_FILTER xusbFilter = {};
	xusbFilter.cbSize = sizeof(CM_NOTIFY_FILTER);
	xusbFilter.FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
	xusbFilter.u.DeviceInterface.ClassGuid = XUSB_INTERFACE_CLASS_GUID;

	//
	// Register X360/XBONE device interface
	// 
	ret = CM_Register_Notification(&xusbFilter, nullptr, DeviceNotificationCallback, &_this->XusbNotificationHandle);

	if (ret != CR_SUCCESS)
	{
		logger->error("CM_Register_Notification (XUSB) failed: {:#x}", ret);
	}

	if (const auto result = hid_init(); result != 0)
	{
		logger->error("hid_init failed: {}", ConvertWideToANSI(hid_error(nullptr)));
	}

	return ERROR_SUCCESS;
}
