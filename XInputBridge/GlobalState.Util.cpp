﻿#include "GlobalState.h"
#include <devpkey.h>
#include <hidclass.h>
#include "UniUtil.h"

#include <winreg/WinReg.hpp>

#include "DsHidMini/dshmguid.h"

SHORT GlobalState::ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	auto intValue = value - 0x80;
	if (intValue == -128)
		intValue = -127;

	const auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

float GlobalState::ClampAxis(float value)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	if (value > 1.0f)
		return 1.0f;
	if (value < -1.0f)
		return -1.0f;
	return value;
}

float GlobalState::ToAxis(UCHAR value)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
}

std::optional<std::vector<std::wstring>> GlobalState::GetSymbolicLinksForDeviceInterfaceClass(const GUID* InterfaceGuid)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("");

	ULONG requiredNumChars = 0;

	CONFIGRET ret = CM_Get_Device_Interface_List_SizeW(
		&requiredNumChars,
		const_cast<LPGUID>(InterfaceGuid),
		nullptr,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	if (requiredNumChars <= 1)
		// single NULL character means list is empty
		return std::nullopt;

	auto szListBuffer = static_cast<PZZWSTR>(calloc(requiredNumChars, sizeof(WCHAR)));

	if (szListBuffer == nullptr)
		return std::nullopt;

	absl::Cleanup freeBuffer = [szListBuffer]
	{
		free(szListBuffer);
	};

	ret = CM_Get_Device_Interface_ListW(
		const_cast<LPGUID>(InterfaceGuid),
		nullptr,
		szListBuffer,
		requiredNumChars,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	const std::vector<wchar_t> multiString{ &szListBuffer[0], szListBuffer + requiredNumChars };
	const auto symlinks = winreg::details::ParseMultiString(multiString);

	return symlinks;
}

std::optional<std::wstring> GlobalState::InterfaceIdToInstanceId(const std::wstring& Symlink)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "util.symlink", ConvertWideToANSI(Symlink) });

	ULONG instanceIdBytes = 0;
	DEVPROPTYPE type{ 0 };

	CONFIGRET ret = CM_Get_Device_Interface_PropertyW(
		Symlink.c_str(),
		&DEVPKEY_Device_InstanceId,
		&type,
		nullptr,
		&instanceIdBytes,
		0
	);

	if (ret != CR_BUFFER_SMALL)
		return std::nullopt;

	const auto instanceIdBuf = static_cast<PBYTE>(calloc(instanceIdBytes, 1));

	if (instanceIdBuf == nullptr)
		return std::nullopt;

	absl::Cleanup freeBuffer = [instanceIdBuf]
	{
		free(instanceIdBuf);
	};

	ret = CM_Get_Device_Interface_PropertyW(
		Symlink.c_str(),
		&DEVPKEY_Device_InstanceId,
		&type,
		instanceIdBuf,
		&instanceIdBytes,
		0
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	std::wstring instanceId{ reinterpret_cast<PWSTR>(instanceIdBuf), reinterpret_cast<PWSTR>(instanceIdBuf + instanceIdBytes) };

	return instanceId;
}

std::optional<std::vector<std::wstring>> GlobalState::GetDeviceChildren(const std::wstring& ParentDeviceId)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "util.parentDeviceId", ConvertWideToANSI(ParentDeviceId) });

	DEVINST instance{ 0 };
	DEVPROPTYPE type{ 0 };
	ULONG childrenIdBytes = 0;

	CONFIGRET ret = CM_Locate_DevNodeW(
		&instance,
		const_cast<DEVINSTID_W>(ParentDeviceId.c_str()),
		CM_LOCATE_DEVNODE_NORMAL
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	ret = CM_Get_DevNode_PropertyW(
		instance,
		&DEVPKEY_Device_Children,
		&type,
		nullptr,
		&childrenIdBytes,
		0
	);

	if (ret != CR_BUFFER_SMALL)
		return std::nullopt;

	const auto childrenIdBuf = static_cast<PBYTE>(calloc(childrenIdBytes, 1));

	absl::Cleanup freeBuffer = [childrenIdBuf]
	{
		free(childrenIdBuf);
	};

	ret = CM_Get_DevNode_PropertyW(
		instance,
		&DEVPKEY_Device_Children,
		&type,
		childrenIdBuf,
		&childrenIdBytes,
		0
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	const std::vector<wchar_t> multiString{
		reinterpret_cast<PWSTR>(childrenIdBuf), reinterpret_cast<PWSTR>(childrenIdBuf + childrenIdBytes)
	};
	const auto childIds = winreg::details::ParseMultiString(multiString);

	return childIds;
}

std::optional<std::vector<std::wstring>> GlobalState::InstanceIdToHidPaths(const std::wstring& InstanceId)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "util.instanceId", ConvertWideToANSI(InstanceId) });

	ULONG requiredNumChars = 0;

	CONFIGRET ret = CM_Get_Device_Interface_List_SizeW(
		&requiredNumChars,
		const_cast<LPGUID>(&GUID_DEVINTERFACE_HID),
		const_cast<DEVINSTID_W>(InstanceId.c_str()),
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	if (requiredNumChars <= 1)
		// single NULL character means list is empty
		return std::nullopt;

	auto buffer = static_cast<PZZWSTR>(calloc(requiredNumChars, sizeof(WCHAR)));

	if (buffer == nullptr)
		return std::nullopt;

	absl::Cleanup freeBuffer = [buffer]
	{
		free(buffer);
	};

	ret = CM_Get_Device_Interface_ListW(
		const_cast<LPGUID>(&GUID_DEVINTERFACE_HID),
		const_cast<DEVINSTID_W>(InstanceId.c_str()),
		buffer,
		requiredNumChars,
		CM_GET_DEVICE_INTERFACE_LIST_PRESENT
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	const std::vector<wchar_t> multiString{ &buffer[0], buffer + requiredNumChars };
	const auto symlinks = winreg::details::ParseMultiString(multiString);

	return symlinks;
}

std::optional<uint8_t> GlobalState::GetDs3HidDeviceModeProperty(const std::wstring& Ds3InstanceId)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("", { "util.instanceId", ConvertWideToANSI(Ds3InstanceId) });

	DEVINST instance{ 0 };
	DEVPROPTYPE type{ 0 };

	CONFIGRET ret = CM_Locate_DevNodeW(
		&instance,
		const_cast<DEVINSTID_W>(Ds3InstanceId.c_str()),
		CM_LOCATE_DEVNODE_NORMAL
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	uint8_t hidDeviceMode = 0;
	ULONG size = sizeof(uint8_t);

	ret = CM_Get_DevNode_PropertyW(
		instance,
		&DEVPKEY_DsHidMini_RW_HidDeviceMode,
		&type,
		&hidDeviceMode,
		&size,
		0
	);

	if (ret != CR_SUCCESS)
		return std::nullopt;

	return hidDeviceMode;
}
