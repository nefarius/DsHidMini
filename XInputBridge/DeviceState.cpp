#include "DeviceState.h"
#include "GlobalState.h"
#include "Macros.h"
#include "ReportMapper.h"
#include "UniUtil.h"

bool DeviceState::InitializeAsXusb(const std::wstring& Symlink, const DWORD UserIndex)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "device.symlink", ConvertWideToANSI(Symlink) },
		{ "device.userIndex", std::to_string(UserIndex) }
	);

	Dispose();

	this->SymbolicLink = ConvertWideToANSI(Symlink);
	this->RealUserIndex = UserIndex;
	this->Type = XI_DEVICE_TYPE_XUSB;

	return true;
}

bool DeviceState::InitializeAsDs3(const std::wstring& Symlink)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "device.symlink", ConvertWideToANSI(Symlink) }
	);

	Dispose();

	this->SymbolicLink = ConvertWideToANSI(Symlink);

	const auto instanceId = GlobalState::InterfaceIdToInstanceId(Symlink);

	if (!instanceId.has_value())
	{
		LOG_ERROR("Failed to get Instance ID for instance {}", this->SymbolicLink);
		return false;
	}

	if (GlobalState::GetDs3HidDeviceModeProperty(instanceId.value()) != DSHM_HID_DEVICE_MODE_SXS)
	{
		LOG_WARN("DS3 device {} not in SXS mode, skipping", this->SymbolicLink);
		return false;
	}

	int retries = 5;
	auto children = GlobalState::GetDeviceChildren(instanceId.value());

	// PnP manager needs a bit of time to bring the device online
	while (!children.has_value() || children->empty())
	{
		if (--retries == 0)
		{
			LOG_ERROR("Failed to get child devices for instance {}", this->SymbolicLink);
			return false;
		}

		Sleep(100);

		children = GlobalState::GetDeviceChildren(instanceId.value());
	}

	retries = 5;

	const auto hidDeviceId = children.value()[0];
	auto hidPaths = GlobalState::InstanceIdToHidPaths(hidDeviceId);

	// HID driver needs a bit of time to bring the interface online
	while (!hidPaths.has_value() || hidPaths->empty())
	{
		if (--retries == 0)
		{
			LOG_ERROR("Failed to get HID instance path for instance {}", this->SymbolicLink);
			return false;
		}

		Sleep(100);

		hidPaths = GlobalState::InstanceIdToHidPaths(hidDeviceId);
	}

	const auto hidSymlink = hidPaths.value()[0];

	const auto device = hid_open_path(ConvertWideToANSI(hidSymlink).c_str());

	if (device == nullptr)
	{
		LOG_ERROR("Failed to open hid device {} with error {}",
			ConvertWideToANSI(hidSymlink),
			ConvertWideToANSI(hid_error(nullptr))
		);
		return false;
	}

	this->HidDeviceHandle = device;
	this->Type = XI_DEVICE_TYPE_DS3;

	return true;
}

void DeviceState::AdoptFrom(DeviceState& Other)
{
	Dispose();

	SymbolicLink = std::move(Other.SymbolicLink);
	RealUserIndex = Other.RealUserIndex;
	HidDeviceHandle = Other.HidDeviceHandle;
	SyntheticPacketNumber = Other.SyntheticPacketNumber;
	LastReport = Other.LastReport;
	Type.store(Other.Type.load());

	Other.HidDeviceHandle = nullptr;
	Other.RealUserIndex = INVALID_X_INPUT_USER_ID;
	Other.SyntheticPacketNumber = 0;
	RtlZeroMemory(&Other.LastReport, sizeof(Other.LastReport));
	Other.Type = XI_DEVICE_TYPE_NOT_CONNECTED;
	Other.SymbolicLink.clear();
}

void DeviceState::Dispose()
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

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
	default:
		break;
	}

	RtlZeroMemory(&this->LastReport, sizeof(DS3_RAW_INPUT_REPORT));
	this->SyntheticPacketNumber = 0;
	this->SymbolicLink.clear();
	this->Type = XI_DEVICE_TYPE_NOT_CONNECTED;
}

_Must_inspect_result_
bool DeviceState::Ds3GetPacketNumber(_In_ PDS3_RAW_INPUT_REPORT Report, _Inout_ DWORD* PacketNumber)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	if (!Report || !PacketNumber)
		return false;

	if (this->Type != XI_DEVICE_TYPE_DS3)
		return false;

	ReportMapper::UpdateSyntheticPacketNumber(*Report, this->LastReport, this->SyntheticPacketNumber);
	*PacketNumber = this->SyntheticPacketNumber;

	return true;
}

_Must_inspect_result_
bool DeviceState::Ds3GetDeviceHandle(_Inout_opt_ hid_device** Handle) const
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	if (this->Type != XI_DEVICE_TYPE_DS3)
		return false;

	if (!this->HidDeviceHandle)
		return false;

	if (Handle)
		*Handle = this->HidDeviceHandle;

	return true;
}
