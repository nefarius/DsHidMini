#include "DeviceState.h"
#include "GlobalState.h"

#include <spdlog/spdlog.h>

#include "Macros.h"
#include "UniUtil.h"

bool DeviceState::InitializeAsXusb(const std::wstring& Symlink, const DWORD UserIndex)
{
	this->Type = XI_DEVICE_TYPE_NOT_CONNECTED;

	this->SymbolicLink = ConvertWideToANSI(Symlink);
	this->RealUserIndex = UserIndex;
	this->Type = XI_DEVICE_TYPE_XUSB;

	return true;
}

bool DeviceState::InitializeAsDs3(const std::wstring& Symlink)
{
	const std::shared_ptr<spdlog::logger> logger = spdlog::get(LOGGER_NAME)->clone(__FUNCTION__);

	int retries = 5;

	this->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
	this->SymbolicLink = ConvertWideToANSI(Symlink);

	const auto instanceId = GlobalState::InterfaceIdToInstanceId(Symlink);

	if (!instanceId.has_value())
	{
		logger->error("Failed to get Instance ID for instance {}", this->SymbolicLink);
		return false;
	}

	auto children = GlobalState::GetDeviceChildren(instanceId.value());

	while (!children.has_value())
	{
		if (--retries == 0)
		{
			logger->error("Failed to get child devices for instance {}", this->SymbolicLink);
			return false;
		}

		Sleep(100);

		children = GlobalState::GetDeviceChildren(instanceId.value());
	}

	retries = 5;

	const auto hidDeviceId = children.value()[0];
	auto hidPaths = GlobalState::InstanceIdToHidPaths(hidDeviceId);

	while (!hidPaths.has_value())
	{
		if (--retries == 0)
		{
			logger->error("Failed to get HID instance path for instance {}", this->SymbolicLink);
			return false;
		}

		Sleep(100);

		hidPaths = GlobalState::InstanceIdToHidPaths(hidDeviceId);
	}

	const auto hidSymlink = hidPaths.value()[0];

	const auto device = hid_open_path(ConvertWideToANSI(hidSymlink).c_str());

	if (device == nullptr)
	{
		logger->error("Failed to open hid device {} with error {}",
			ConvertWideToANSI(hidSymlink),
			ConvertWideToANSI(hid_error(nullptr))
		);
		return false;
	}

	this->HidDeviceHandle = device;
	this->Type = XI_DEVICE_TYPE_DS3;

	return true;
}

void DeviceState::Dispose()
{
	switch (this->Type)
	{
	case XI_DEVICE_TYPE_DS3:
		if (this->HidDeviceHandle != nullptr)
		{
			hid_close(this->HidDeviceHandle);
			this->HidDeviceHandle = nullptr;
		}
		break;
	case XI_DEVICE_TYPE_XUSB:
		this->RealUserIndex = INVALID_X_INPUT_USER_ID;
		break;
	}

	RtlZeroMemory(&this->LastReport, sizeof(DS3_RAW_INPUT_REPORT));
	this->SyntheticPacketNumber = 0;
	this->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
}

bool DeviceState::Ds3GetPacketNumber(PDS3_RAW_INPUT_REPORT Report, DWORD* PacketNumber)
{
	if (!Report || !PacketNumber)
		return false;

	if (this->Type != XI_DEVICE_TYPE_DS3)
		return false;

	//
	// Exclude noisy motion stuff from comparison
	// 
	constexpr size_t bytesToCompare = sizeof(DS3_RAW_INPUT_REPORT) - 18;

	//
	// Only increment when a change happened
	// 
	if (memcmp(
		&this->LastReport,
		Report,
		bytesToCompare
	) != 0)
	{
		this->SyntheticPacketNumber++;
		memcpy(&this->LastReport, Report, sizeof(DS3_RAW_INPUT_REPORT));
	}

#if defined(SCPLIB_ENABLE_TELEMETRY)
	span->SetAttribute("xinput.packetNumber", std::to_string(state->packetNumber));
#endif

	*PacketNumber = this->SyntheticPacketNumber;

	return true;
}

bool DeviceState::Ds3GetDeviceHandle(hid_device** Handle) const
{
	if (this->Type != XI_DEVICE_TYPE_DS3)
		return false;

	if (!this->HidDeviceHandle)
		return false;

	if (Handle)
		*Handle = this->HidDeviceHandle;

	return true;
}
