#include <Shlwapi.h>
#include <hidapi/hidapi.h>
#include "GlobalState.h"
#include "UniUtil.h"
#include "DsHidMini/dshmguid.h"


DWORD CALLBACK GlobalState::DeviceNotificationCallback(
	_In_ HCMNOTIFICATION hNotify,
	_In_opt_ PVOID Context,
	_In_ CM_NOTIFY_ACTION Action,
	_In_reads_bytes_(EventDataSize) PCM_NOTIFY_EVENT_DATA EventData,
	_In_ DWORD EventDataSize
)
{
	UNREFERENCED_PARAMETER(hNotify);
	UNREFERENCED_PARAMETER(EventDataSize);

	const auto _this = static_cast<GlobalState*>(Context);

	if (_this == nullptr)
	{
		LOG_ERROR("Missing state pointer");
		return ERROR_INVALID_PARAMETER;
	}

	if (_this->ShuttingDown.load())
		return ERROR_SUCCESS;

	auto scopedSpan = TRACE_SCOPED_SPAN("");

	switch (Action)
	{
	case CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL:
		if (IsEqualGUID(GUID_DEVINTERFACE_DSHIDMINI, EventData->u.DeviceInterface.ClassGuid))
		{
			const auto symlink = std::wstring(EventData->u.DeviceInterface.SymbolicLink);
			LOG_INFO("New DS3 device arrived: {}", ConvertWideToANSI(symlink));

			InterlockedIncrement(&_this->ArrivalWorkCount);

			// child device boot is not instant so we need to do
			// this in the background to not block this callback
			try
			{
				std::thread asyncArrival{
					[_this, symlink]
					{
						ScopeCleanup workDone = [_this]
						{
							InterlockedDecrement(&_this->ArrivalWorkCount);
						};

						if (_this->ShuttingDown.load())
							return;

						DeviceState prepared;
						if (!prepared.InitializeAsDs3(symlink))
							return;

						_this->AssignPreparedDs3(symlink, prepared);
					}
				};

				asyncArrival.detach();
			}
			catch (...)
			{
				InterlockedDecrement(&_this->ArrivalWorkCount);
				LOG_ERROR("Failed to start DS3 arrival worker for {}", ConvertWideToANSI(symlink));
			}
		}

		if (IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const auto symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);

			LOG_INFO("New XUSB device arrived: {}", symlink);

			DWORD userIndex = INVALID_X_INPUT_USER_ID;
			if (SymlinkToUserIndex(EventData->u.DeviceInterface.SymbolicLink, &userIndex))
			{
				LOG_INFO("User index: {}", userIndex);
				_this->AssignXusbDevice(EventData->u.DeviceInterface.SymbolicLink, userIndex);
			}
			else
			{
				LOG_ERROR("User index lookup failed");
			}
		}
		break;
	case CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL:
		if (IsEqualGUID(GUID_DEVINTERFACE_DSHIDMINI, EventData->u.DeviceInterface.ClassGuid) ||
			IsEqualGUID(XUSB_INTERFACE_CLASS_GUID, EventData->u.DeviceInterface.ClassGuid))
		{
			const std::string symlink = ConvertWideToANSI(EventData->u.DeviceInterface.SymbolicLink);
			LOG_INFO("Device got removed: {}", symlink);

			AcquireSRWLockExclusive(&_this->StatesLock);
			{
				if (const auto slot = _this->FindBySymbolicLink(EventData->u.DeviceInterface.SymbolicLink))
				{
					slot->Dispose();
				}
				else
				{
					LOG_WARN("No state found for {}", symlink);
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

DWORD WINAPI GlobalState::InitAsync(_In_ LPVOID lpParameter)
{
	const auto _this = static_cast<GlobalState*>(lpParameter);
	ScopeCleanup finished = [_this]
	{
		_this->SignalStartupFinished();
	};

	auto scopedSpan = TRACE_SCOPED_SPAN("");

	LOG_INFO("Async library startup initialized");

	if (_this->ShuttingDown.load())
		return ERROR_SUCCESS;

	CHAR systemDir[MAX_PATH] = {};

	if (GetSystemDirectoryA(systemDir, MAX_PATH) == 0)
	{
		LOG_ERROR("GetSystemDirectoryA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	CHAR fullXiPath[MAX_PATH] = {};

	if (PathCombineA(fullXiPath, systemDir, XI_SYSTEM_LIB_NAME) == nullptr)
	{
		LOG_ERROR("PathCombineA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	_this->SystemXInputModule = LoadLibraryA(fullXiPath);

	if (_this->SystemXInputModule == nullptr)
	{
		LOG_ERROR("LoadLibraryA failed: {:#x}", GetLastError());
		return GetLastError();
	}

	const HMODULE xiLib = _this->SystemXInputModule;

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

	if (_this->ShuttingDown.load())
		return ERROR_SUCCESS;

	CM_NOTIFY_FILTER ds3Filter = {};
	ds3Filter.cbSize = sizeof(CM_NOTIFY_FILTER);
	ds3Filter.FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
	ds3Filter.u.DeviceInterface.ClassGuid = GUID_DEVINTERFACE_DSHIDMINI;

	CONFIGRET ret = CM_Register_Notification(&ds3Filter, _this, DeviceNotificationCallback, &_this->Ds3NotificationHandle);

	if (ret != CR_SUCCESS)
	{
		LOG_ERROR("CM_Register_Notification (DS3) failed: {:#x}", ret);
	}

	CM_NOTIFY_FILTER xusbFilter = {};
	xusbFilter.cbSize = sizeof(CM_NOTIFY_FILTER);
	xusbFilter.FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE;
	xusbFilter.u.DeviceInterface.ClassGuid = XUSB_INTERFACE_CLASS_GUID;

	ret = CM_Register_Notification(&xusbFilter, _this, DeviceNotificationCallback, &_this->XusbNotificationHandle);

	if (ret != CR_SUCCESS)
	{
		LOG_ERROR("CM_Register_Notification (XUSB) failed: {:#x}", ret);
	}

	if (const auto result = hid_init(); result != 0)
	{
		LOG_ERROR("hid_init failed: {}", ConvertWideToANSI(hid_error(nullptr)));
	}

	if (!_this->ShuttingDown.load())
	{
		_this->EnumerateDs3Devices();
		_this->EnumerateXusbDevices();
	}

	return ERROR_SUCCESS;
}
