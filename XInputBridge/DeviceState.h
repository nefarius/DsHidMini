#pragma once
#include <Windows.h>
#include <string>
#include <memory>

#include <hidapi/hidapi.h>

#include <DsHidMini/Ds3Types.h>
#include "Types.h"

class DeviceState
{
private:
	/** The device and connection type */
	std::atomic<XI_DEVICE_TYPE> Type{XI_DEVICE_TYPE_NOT_CONNECTED};
	/** The symbolic link of the USB/BTH device (_NOT_ the HID device) */
	std::string SymbolicLink{};
	/** When in XUSB mode, the real underlying user index */
	DWORD RealUserIndex{INVALID_X_INPUT_USER_ID};
	/** When in DS3 mode, the open handle to the HID device */
	hid_device* HidDeviceHandle{};
	/** The synthetic packet number */
	DWORD SyntheticPacketNumber{};
	/** The previous cached report copy */
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
