#include "DeviceState.h"

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

	this->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
	this->SymbolicLink = ConvertWideToANSI(Symlink);

	const auto device = hid_open_path(this->SymbolicLink.c_str());

	if (device == nullptr)
	{
		logger->error("Failed to open hid device {} with error {}",
			this->SymbolicLink,
			ConvertWideToANSI(hid_error(nullptr))
		);
		return false;
	}

	this->Ds3Device = device;
	this->Type = XI_DEVICE_TYPE_DS3;

	return true;
}

void DeviceState::Dispose()
{
	switch (this->Type)
	{
	case XI_DEVICE_TYPE_DS3:
		if (this->Ds3Device != nullptr)
		{
			hid_close(this->Ds3Device);
			this->Ds3Device = nullptr;
		}
		break;
	case XI_DEVICE_TYPE_XUSB:
		this->RealUserIndex = INVALID_X_INPUT_USER_ID;
		break;
	}

	RtlZeroMemory(&this->LastReport, sizeof(DS3_RAW_INPUT_REPORT));
	this->PacketNumber = 0;
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
		this->PacketNumber++;
		memcpy(&this->LastReport, Report, sizeof(DS3_RAW_INPUT_REPORT));
	}

#if defined(SCPLIB_ENABLE_TELEMETRY)
	span->SetAttribute("xinput.packetNumber", std::to_string(state->packetNumber));
#endif

	*PacketNumber = this->PacketNumber;

	return true;
}
bool DeviceState::Ds3GetDeviceHandle(hid_device** Handle) const
{
	if (this->Type != XI_DEVICE_TYPE_DS3)
		return false;

	if (!this->Ds3Device)
		return false;
	
	if (Handle)
		*Handle = this->Ds3Device;

	return true;
}
