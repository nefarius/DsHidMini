#include <Shlwapi.h>
#include <spdlog/logger.h>
#include <spdlog/spdlog.h>

#include "GlobalState.h"
#include "UniUtil.h"
#include "DsHidMini/dshmguid.h"


//
// Invoked on device arrival or removal
// 
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

	const auto _this = static_cast<GlobalState*>(Context);
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	switch (Action)
	{
	case CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL:
		if (IsEqualGUID(GUID_DEVINTERFACE_DSHIDMINI, EventData->u.DeviceInterface.ClassGuid))
		{
			const auto symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);
			logger->info("New DS3 device arrived: {}", symlink);

			AcquireSRWLockExclusive(&_this->StatesLock);
			{
				if (const auto slot = _this->GetNextFreeSlot())
				{
					slot->Dispose();
					if (!slot->InitializeAsDs3(EventData->u.DeviceInterface.SymbolicLink))
					{
						logger->error("Failed to initialize {} as a DS3 HID device", symlink);
					}
				}
			}
			ReleaseSRWLockExclusive(&_this->StatesLock);
		}

		if (IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const auto symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);

			logger->info("New XUSB device arrived: {}", symlink);

			DWORD userIndex = INVALID_X_INPUT_USER_ID;
			if (SymlinkToUserIndex(EventData->u.DeviceInterface.SymbolicLink, &userIndex))
			{
				logger->info("User index: {}", userIndex);

				AcquireSRWLockExclusive(&_this->StatesLock);
				{
					if (const auto slot = _this->GetNextFreeSlot())
					{
						slot->Dispose();
						if (!slot->InitializeAsXusb(EventData->u.DeviceInterface.SymbolicLink, userIndex))
						{
							logger->error("Failed to initialize {} as a XUSB device", symlink);
						}
					}
				}
				ReleaseSRWLockExclusive(&_this->StatesLock);
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

			AcquireSRWLockExclusive(&_this->StatesLock);
			{
				if (const auto slot = _this->FindBySymbolicLink(EventData->u.DeviceInterface.SymbolicLink))
				{
					slot->Dispose();
				}
				else
				{
					logger->warn("No state found for {}", symlink);
				}
			}
			ReleaseSRWLockExclusive(&_this->StatesLock);
		}

		if (IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const std::string symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);
			logger->info("XUSB device got removed: {}", symlink);

			AcquireSRWLockExclusive(&_this->StatesLock);
			{
				if (const auto slot = _this->FindBySymbolicLink(EventData->u.DeviceInterface.SymbolicLink))
				{
					slot->Dispose();
				}
				else
				{
					logger->warn("No state found for {}", symlink);
				}
			}
			ReleaseSRWLockExclusive(&_this->StatesLock);
		}
		break;
	default:
		return ERROR_SUCCESS;
	}

	return ERROR_SUCCESS;
}

//
// Initialization tasks on a background thread
// 
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
	CONFIGRET ret = CM_Register_Notification(&ds3Filter, _this, DeviceNotificationCallback, &_this->Ds3NotificationHandle);

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
	ret = CM_Register_Notification(&xusbFilter, _this, DeviceNotificationCallback, &_this->XusbNotificationHandle);

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
