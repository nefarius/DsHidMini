#include "GlobalState.h"

#include <memory>
#include <spdlog/logger.h>
#include <spdlog/spdlog.h>
#include <Shlwapi.h>
#include <initguid.h>
#include <devpkey.h>
#include <hidapi/hidapi.h>
#include <spdlog/sinks/basic_file_sink.h>
#include <absl/strings/match.h>
#include <absl/cleanup/cleanup.h>
#include <winreg/WinReg.hpp>

#include "DsHidMini/dshmguid.h"

#include "Types.h"
#include "UniUtil.h"


EXTERN_C IMAGE_DOS_HEADER __ImageBase;


//
// Initialization safe to perform in DllMain
// 
void GlobalState::Initialize()
{
	InitializeSRWLock(&StatesLock);

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

	absl::Cleanup handleFree = [handle]
	{
		if (handle != INVALID_HANDLE_VALUE)
			CloseHandle(handle);
	};

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

DeviceState* GlobalState::GetNextFreeSlot()
{
	const auto item = std::ranges::find_if(this->States,
		[](const DeviceState& element)
		{
			return element.Type == XI_DEVICE_TYPE_NOT_CONNECTED;
		});

	return (item != this->States.end()) ? &(*item) : nullptr;
}

DeviceState* GlobalState::FindBySymbolicLink(const std::wstring& Symlink)
{
	const auto narrow = ConvertWideToANSI(Symlink);

	const auto item = std::ranges::find_if(this->States,
		[narrow](const DeviceState& element)
		{
			return absl::EqualsIgnoreCase(element.SymbolicLink, narrow);
		});

	return (item != this->States.end()) ? &(*item) : nullptr;
}

DeviceState* GlobalState::GetXusbByUserIndex(const DWORD UserIndex)
{
	const auto item = std::ranges::find_if(this->States,
		[UserIndex](const DeviceState& element)
		{
			return element.Type == XI_DEVICE_TYPE_XUSB && element.RealUserIndex == UserIndex;
		});

	return (item != this->States.end()) ? &(*item) : nullptr;
}

bool GlobalState::GetDs3ByUserIndex(const DWORD UserIndex, DeviceState** Handle) const
{
	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	if (Handle)
		*Handle = const_cast<DeviceState*>(&this->States[UserIndex]);

	return true;
}

bool GlobalState::IsConnectedDs3(const DWORD UserIndex) const
{
	DeviceState* state = nullptr;

	if (GetDs3ByUserIndex(UserIndex, &state))
	{
		return state->Type == XI_DEVICE_TYPE_DS3;
	}

	return false;
}

void GlobalState::EnumerateDs3Devices()
{
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	ULONG requiredNumChars = 0;

	CONFIGRET ret = CM_Get_Device_Interface_List_SizeW(
		&requiredNumChars,
		const_cast<LPGUID>(&GUID_DEVINTERFACE_DSHIDMINI),
		nullptr,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
	{
		logger->error("CM_Get_Device_Interface_List_SizeW failed with {:#x}", ret);
		return;
	}

	if (requiredNumChars <= 1)
		// single NULL character means list is empty
		return;

	auto buffer = static_cast<PZZWSTR>(calloc(requiredNumChars, sizeof(WCHAR)));

	absl::Cleanup lockRelease = [buffer]
	{
		free(buffer);
	};

	ret = CM_Get_Device_Interface_ListW(
		const_cast<LPGUID>(&GUID_DEVINTERFACE_DSHIDMINI),
		nullptr,
		buffer,
		requiredNumChars,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
	{
		logger->error("CM_Get_Device_Interface_ListW failed with {:#x}", ret);
		return;
	}

	const std::vector<wchar_t> multiString{ &buffer[0], buffer + requiredNumChars };
	const auto symlinks = winreg::winreg_internal::ParseMultiString(multiString);

	for (const auto& symlink : symlinks)
	{
		auto instanceId = InterfaceIdToInstanceId(symlink);

		if (!instanceId.has_value())
			continue;

		auto children = GetDeviceChildren(instanceId.value());

		if (!children.has_value())
			continue;

		const auto hidDeviceId = children.value()[0];

		if (const auto state = this->GetNextFreeSlot())
		{
			state->Dispose();
			state->InitializeAsDs3(symlink);
		}
	}
}
