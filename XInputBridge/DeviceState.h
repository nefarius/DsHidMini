#pragma once
#include <Windows.h>
#include <string>
#include <memory>

#include <hidapi/hidapi.h>

#include <DsHidMini/Ds3Types.h>
#include "Types.h"

class DeviceState
{
public:
	

private:
	std::atomic<XI_DEVICE_TYPE> Type{XI_DEVICE_TYPE_NOT_CONNECTED};
	std::string SymbolicLink{};
	DWORD RealUserIndex{INVALID_X_INPUT_USER_ID};
	hid_device* Ds3Device{};
	DWORD PacketNumber{};
	DS3_RAW_INPUT_REPORT LastReport{};

	bool InitializeAsXusb(const std::wstring& Symlink, const DWORD UserIndex);
	bool InitializeAsDs3(const std::wstring& Symlink);
	void Dispose();

	_Must_inspect_result_
	bool Ds3GetPacketNumber(_In_ PDS3_RAW_INPUT_REPORT Report, _Inout_ DWORD* PacketNumber);

	_Must_inspect_result_
	bool Ds3GetDeviceHandle(_Inout_opt_ hid_device** Handle) const;

	friend class GlobalState;
};
