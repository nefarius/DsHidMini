#include "GlobalState.h"

#include <Shlwapi.h>
#include <hidapi/hidapi.h>
#include <winreg/WinReg.hpp>

#include "ReportMapper.h"
#include "Types.h"
#include "UniUtil.h"


EXTERN_C IMAGE_DOS_HEADER __ImageBase; // NOLINT(bugprone-reserved-identifier)


void GlobalState::Initialize()
{
	InitializeSRWLock(&StatesLock);
	ShuttingDown.store(false);
	StartupReady.store(false);

	this->StartupFinishedEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);

	this->InitThread = CreateThread(nullptr, 0, InitAsync, this, 0, nullptr);
	if (this->InitThread == nullptr)
	{
		SignalStartupFinished();
	}
}

void GlobalState::Destroy()
{
	LOG_INFO("Library getting unloaded from PID {}", GetCurrentProcessId());

	ShuttingDown.store(true);
	SignalStartupFinished();

	if (this->Ds3NotificationHandle)
	{
		(void)CM_Unregister_Notification(this->Ds3NotificationHandle);
		this->Ds3NotificationHandle = nullptr;
	}

	if (this->XusbNotificationHandle)
	{
		(void)CM_Unregister_Notification(this->XusbNotificationHandle);
		this->XusbNotificationHandle = nullptr;
	}

	for (int i = 0; i < 50 && ArrivalWorkCount > 0; ++i)
	{
		Sleep(20);
	}

	AcquireSRWLockExclusive(&this->StatesLock);
	DisposeAllSlots();
	ReleaseSRWLockExclusive(&this->StatesLock);

	(void)hid_exit();

	if (this->SystemXInputModule)
	{
		FpnXInputGetState = nullptr;
		FpnXInputSetState = nullptr;
		FpnXInputGetCapabilities = nullptr;
		FpnXInputEnable = nullptr;
		FpnXInputGetDSoundAudioDeviceGuids = nullptr;
		FpnXInputGetBatteryInformation = nullptr;
		FpnXInputGetKeystroke = nullptr;
		FpnXInputGetStateEx = nullptr;
		FpnXInputWaitForGuideButton = nullptr;
		FpnXInputCancelGuideButtonWait = nullptr;
		FpnXInputPowerOffController = nullptr;
		(void)FreeLibrary(this->SystemXInputModule);
		this->SystemXInputModule = nullptr;
	}

	if (this->InitThread)
	{
		(void)WaitForSingleObject(this->InitThread, 250);
		(void)CloseHandle(this->InitThread);
		this->InitThread = nullptr;
	}

	if (this->StartupFinishedEvent && this->StartupFinishedEvent != INVALID_HANDLE_VALUE)
	{
		(void)CloseHandle(this->StartupFinishedEvent);
		this->StartupFinishedEvent = INVALID_HANDLE_VALUE;
	}
}

void GlobalState::WaitForStartup() const
{
	if (StartupReady.load(std::memory_order_acquire) || ShuttingDown.load(std::memory_order_acquire))
		return;

	if (StartupFinishedEvent && StartupFinishedEvent != INVALID_HANDLE_VALUE)
		WaitForSingleObject(StartupFinishedEvent, MAX_STARTUP_WAIT_MS);
}

void GlobalState::SignalStartupFinished()
{
	if (StartupFinishedEvent && StartupFinishedEvent != INVALID_HANDLE_VALUE)
		SetEvent(StartupFinishedEvent);

	StartupReady.store(true, std::memory_order_release);
}

void GlobalState::DisposeAllSlots()
{
	for (auto& slot : States)
	{
		slot.Dispose();
	}
}

_Success_(return)
_Must_inspect_result_
bool GlobalState::SymlinkToUserIndex(_In_ PCWSTR Symlink, _Out_ PDWORD UserIndex)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "global.symlink", ConvertWideToANSI(Symlink) });

	if (!UserIndex)
		return false;

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

	if (handle == INVALID_HANDLE_VALUE)
		return false;

	ScopeCleanup handleFree = [handle]
	{
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
		static_cast<DWORD>(gamepadStateRequest0101.size()),
		ledStateData.data(),
		static_cast<DWORD>(ledStateData.size()),
		&len,
		nullptr
	))
	{
		return false;
	}

	if (len < 3)
		return false;

	return ReportMapper::MapXusbLedStateToUserIndex(ledStateData[2], *UserIndex);
}

_Success_(return != NULL)
_Must_inspect_result_
DeviceState* GlobalState::GetNextFreeSlot(_Out_opt_ PULONG SlotIndex)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	const auto it = std::ranges::find_if(this->States,
		[](const DeviceState& element)
		{
			return element.Type == XI_DEVICE_TYPE_NOT_CONNECTED;
		});

	const auto state = (it != this->States.end()) ? &(*it) : nullptr;

	if (state && SlotIndex)
		*SlotIndex = static_cast<ULONG>(std::distance(this->States.begin(), it));

	return state;
}

DeviceState* GlobalState::FindBySymbolicLink(const std::wstring& Symlink)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "global.symlink", ConvertWideToANSI(Symlink) });

	const auto narrow = ConvertWideToANSI(Symlink);

	const auto item = std::ranges::find_if(this->States,
		[narrow](const DeviceState& element)
		{
			return EqualsIgnoreCase(element.SymbolicLink, narrow);
		});

	return (item != this->States.end()) ? &(*item) : nullptr;
}

DeviceState* GlobalState::GetXusbByUserIndex(const DWORD UserIndex)
{
	return GetXusbByRealUserIndex(UserIndex);
}

DeviceState* GlobalState::GetXusbByRealUserIndex(const DWORD UserIndex)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "xinput.userIndex", std::to_string(UserIndex) }
	);

	if (UserIndex >= DS3_DEVICES_MAX)
		return nullptr;

	const auto item = std::ranges::find_if(this->States,
		[UserIndex](const DeviceState& element)
		{
			return element.Type == XI_DEVICE_TYPE_XUSB && element.RealUserIndex == UserIndex;
		});

	return (item != this->States.end()) ? &(*item) : nullptr;
}

_Success_(return != NULL)
_Must_inspect_result_
bool GlobalState::GetConnectedDs3ByUserIndex(_In_ const DWORD UserIndex, _Out_opt_ DeviceState** Handle) const
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "xinput.userIndex", std::to_string(UserIndex) }
	);

	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	const auto state = const_cast<DeviceState*>(&this->States[UserIndex]);

	if (state->Type != XI_DEVICE_TYPE_DS3)
		return false;

	if (Handle)
		*Handle = state;

	return true;
}

void GlobalState::AssignPreparedDs3(const std::wstring& Symlink, DeviceState& Prepared)
{
	if (ShuttingDown.load())
	{
		Prepared.Dispose();
		return;
	}

	AcquireSRWLockExclusive(&this->StatesLock);
	{
		if (FindBySymbolicLink(Symlink))
		{
			LOG_INFO("DS3 {} already assigned, ignoring duplicate", ConvertWideToANSI(Symlink));
			Prepared.Dispose();
		}
		else if (const auto slot = GetNextFreeSlot())
		{
			slot->AdoptFrom(Prepared);
		}
		else
		{
			LOG_WARN("No free slot to assign {} to", ConvertWideToANSI(Symlink));
			Prepared.Dispose();
		}
	}
	ReleaseSRWLockExclusive(&this->StatesLock);
}

void GlobalState::AssignXusbDevice(const std::wstring& Symlink, const DWORD UserIndex)
{
	if (ShuttingDown.load())
		return;

	AcquireSRWLockExclusive(&this->StatesLock);
	{
		if (FindBySymbolicLink(Symlink) || GetXusbByRealUserIndex(UserIndex))
		{
			LOG_INFO("XUSB {} already assigned, ignoring duplicate", ConvertWideToANSI(Symlink));
		}
		else if (const auto slot = GetNextFreeSlot())
		{
			if (!slot->InitializeAsXusb(Symlink, UserIndex))
			{
				LOG_ERROR("Failed to initialize {} as a XUSB device", ConvertWideToANSI(Symlink));
			}
			else
			{
				LOG_INFO("Assigned {} to real user index {}", ConvertWideToANSI(Symlink), UserIndex);
			}
		}
		else
		{
			LOG_WARN("No free slot to assign {} to", ConvertWideToANSI(Symlink));
		}
	}
	ReleaseSRWLockExclusive(&this->StatesLock);
}

void GlobalState::EnumerateDs3Devices()
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	LOG_INFO("Running DS3 enumeration");

	const auto symlinks = GetSymbolicLinksForDeviceInterfaceClass(&GUID_DEVINTERFACE_DSHIDMINI);

	if (!symlinks.has_value())
	{
		LOG_INFO("No DS3 interface devices found");
		LOG_INFO("DS3 enumeration finished");
		return;
	}

	LOG_INFO("Found {} device(s)", symlinks.value().size());

	for (const auto& symlink : symlinks.value())
	{
		if (ShuttingDown.load())
			break;

		DeviceState prepared;
		if (!prepared.InitializeAsDs3(symlink))
			continue;

		AssignPreparedDs3(symlink, prepared);
	}

	LOG_INFO("DS3 enumeration finished");
}

void GlobalState::EnumerateXusbDevices()
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	LOG_INFO("Running XUSB enumeration");

	const auto symlinks = GetSymbolicLinksForDeviceInterfaceClass(&XUSB_INTERFACE_CLASS_GUID);

	if (!symlinks.has_value())
	{
		LOG_INFO("No XUSB interface devices found");
		LOG_INFO("XUSB enumeration finished");
		return;
	}

	LOG_INFO("Found {} device(s)", symlinks.value().size());

	for (const auto& symlink : symlinks.value())
	{
		if (ShuttingDown.load())
			break;

		DWORD userIndex = INVALID_X_INPUT_USER_ID;
		if (SymlinkToUserIndex(symlink.c_str(), &userIndex))
		{
			LOG_INFO("User index: {}", userIndex);
			AssignXusbDevice(symlink, userIndex);
		}
		else
		{
			LOG_ERROR("User index lookup failed");
		}
	}

	LOG_INFO("XUSB enumeration finished");
}
