#include "GlobalState.h"
#include <initguid.h>
#include <devpkey.h>
#include <cfgmgr32.h>

#include <absl/cleanup/cleanup.h>
#include <winreg/WinReg.hpp>

SHORT GlobalState::ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto intValue = value - 0x80;
	if (intValue == -128)
		intValue = -127;

	const auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

float GlobalState::ClampAxis(float value)
{
	if (value > 1.0f)
		return 1.0f;
	if (value < -1.0f)
		return -1.0f;
	return value;
}

float GlobalState::ToAxis(UCHAR value)
{
	return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
}

std::optional<std::wstring> GlobalState::InterfaceIdToInstanceId(const std::wstring& Symlink)
{
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

	absl::Cleanup lockRelease = [instanceIdBuf]
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

	absl::Cleanup lockRelease = [childrenIdBuf]
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
	const auto childIds = winreg::winreg_internal::ParseMultiString(multiString);

	return childIds;
}
