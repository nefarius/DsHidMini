#include "GlobalState.h"

#include <Shlwapi.h>
#include <hidapi/hidapi.h>
#include <winreg/WinReg.hpp>

#include "Types.h"
#include "UniUtil.h"


EXTERN_C IMAGE_DOS_HEADER __ImageBase; // NOLINT(bugprone-reserved-identifier)


//
// Initialization safe to perform in DllMain
// 
void GlobalState::Initialize()
{
	InitializeSRWLock(&StatesLock);

	this->StartupFinishedEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);

#if defined(SCPLIB_ENABLE_TELEMETRY)
	//
	// Set up tracing
	// 

	const auto resourceAttributes = sdkresource::ResourceAttributes{
		{ opentelemetry::sdk::resource::SemanticConventions::kServiceName, TRACER_NAME }
	};

	const auto resource = sdkresource::Resource::Create(resourceAttributes);
	auto traceExporter = otlp::OtlpGrpcExporterFactory::Create();
	auto traceProcessor = sdktrace::SimpleSpanProcessorFactory::Create(std::move(traceExporter));
	const std::shared_ptr traceProvider = sdktrace::TracerProviderFactory::Create(std::move(traceProcessor), resource);

	trace::Provider::SetTracerProvider(traceProvider);

	//
	// Set up logger
	// 

	auto loggerExporter = otlp::OtlpGrpcLogRecordExporterFactory::Create();
	auto loggerProcessor = sdklogs::SimpleLogRecordProcessorFactory::Create(std::move(loggerExporter));
	const std::shared_ptr loggerProvider = sdklogs::LoggerProviderFactory::Create(std::move(loggerProcessor), resource);

	logs::Provider::SetLoggerProvider(loggerProvider);

	const auto logger = GlobalState::GetLogger(__FUNCTION__);
	LOG_INFO("Library got loaded into PID {}", GetCurrentProcessId());
#endif

	//
	// Call stuff that must not be done in DllMain in the background
	// 
	const HANDLE hThread = CreateThread(nullptr, 0, InitAsync, this, 0, nullptr);

	if (hThread == nullptr)
	{
		LOG_ERROR("Failed to create thread");
	}
}

void GlobalState::Destroy() const
{
	LOG_INFO("Library getting unloaded from PID {}", GetCurrentProcessId());

	(void)CloseHandle(this->StartupFinishedEvent);
	(void)CM_Unregister_Notification(this->Ds3NotificationHandle);
	(void)CM_Unregister_Notification(this->XusbNotificationHandle);

	(void)hid_exit();
}

//
// https://github.com/DJm00n/RawInputDemo/blob/master/RawInputLib/RawInputDeviceHid.cpp#L275-L339
// 
_Must_inspect_result_
bool GlobalState::SymlinkToUserIndex(_In_ PCWSTR Symlink, _Inout_ PDWORD UserIndex)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

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

_Success_(return != NULL)
_Must_inspect_result_
DeviceState* GlobalState::GetNextFreeSlot(_Out_opt_ PULONG SlotIndex)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

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
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

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
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(UserIndex) }
	}));
#endif

	if (UserIndex >= DS3_DEVICES_MAX)
		return nullptr;

	const auto state = &this->States[UserIndex];

	return state->Type == XI_DEVICE_TYPE_XUSB ? state : nullptr;
}

_Success_(return != NULL)
_Must_inspect_result_
bool GlobalState::GetConnectedDs3ByUserIndex(_In_ const DWORD UserIndex, _Out_opt_ DeviceState** Handle) const
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(UserIndex) }
	}));
#endif

	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	const auto state = const_cast<DeviceState*>(&this->States[UserIndex]);

	if (state->Type != XI_DEVICE_TYPE_DS3)
		return false;

	if (Handle)
		*Handle = state;

	return true;
}

void GlobalState::EnumerateDs3Devices()
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

	constexpr uint8_t DsHidMiniDeviceModeSixaxisCompatible = 0x03;

	LOG_INFO("Running DS3 enumeration");

	const auto symlinks = GetSymbolicLinksForDeviceInterfaceClass(&GUID_DEVINTERFACE_DSHIDMINI);

	if (!symlinks.has_value())
	{
		LOG_INFO("No DS3 interface devices found");
		goto exit;
	}

	LOG_INFO("Found {} device(s)", symlinks.value().size());

	for (const auto& symlink : symlinks.value())
	{
		const auto instanceId = InterfaceIdToInstanceId(symlink);

		if (!instanceId.has_value())
		{
			LOG_WARN("Instance ID lookup failed for {}", ConvertWideToANSI(symlink));
			continue;
		}

		const auto hidDeviceMode = GetDs3HidDeviceModeProperty(instanceId.value());

		if (hidDeviceMode != DsHidMiniDeviceModeSixaxisCompatible)
		{
			LOG_WARN("DS3 device {} not in SXS mode, skipping", ConvertWideToANSI(symlink));
			continue;
		}

		AcquireSRWLockExclusive(&this->StatesLock);
		{
			DWORD slotIndex = 0;
			if (const auto state = this->GetNextFreeSlot(&slotIndex))
			{
				state->Dispose();
				if (!state->InitializeAsDs3(symlink))
				{
					LOG_ERROR("Failed to initialize {} as a DS3 device", ConvertWideToANSI(symlink));
				}
				else
				{
					LOG_INFO("Assigned {} to index {}", ConvertWideToANSI(symlink), slotIndex);
				}
			}
			else
			{
				LOG_WARN("No free slot to assign {} to", ConvertWideToANSI(symlink));
			}
		}
		ReleaseSRWLockExclusive(&this->StatesLock);
	}

exit:
	LOG_INFO("DS3 enumeration finished");
}

void GlobalState::EnumerateXusbDevices()
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

	LOG_INFO("Running XUSB enumeration");

	const auto symlinks = GetSymbolicLinksForDeviceInterfaceClass(&XUSB_INTERFACE_CLASS_GUID);

	if (!symlinks.has_value())
	{
		LOG_INFO("No XUSB interface devices found");
		goto exit;
	}

	LOG_INFO("Found {} device(s)", symlinks.value().size());

	for (const auto& symlink : symlinks.value())
	{
		DWORD userIndex = INVALID_X_INPUT_USER_ID;
		if (SymlinkToUserIndex(symlink.c_str(), &userIndex))
		{
			LOG_INFO("User index: {}", userIndex);

			AcquireSRWLockExclusive(&this->StatesLock);
			{
				DWORD slotIndex = 0;
				if (const auto slot = this->GetNextFreeSlot(&slotIndex))
				{
					slot->Dispose();
					if (!slot->InitializeAsXusb(symlink, slotIndex))
					{
						LOG_ERROR("Failed to initialize {} as a XUSB device", ConvertWideToANSI(symlink));
					}
					else
					{
						LOG_INFO("Assigned {} to index {}", ConvertWideToANSI(symlink), slotIndex);
					}
				}
			}
			ReleaseSRWLockExclusive(&this->StatesLock);
		}
		else
		{
			LOG_ERROR("User index lookup failed");
		}
	}

exit:
	LOG_INFO("XUSB enumeration finished");
}
