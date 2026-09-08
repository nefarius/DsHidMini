#include <hidapi/hidapi.h>
#include "GlobalState.h"
#include "ReportMapper.h"


DWORD GlobalState::ReadDs3XInputState(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState, _In_ bool includeGuide)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

	do
	{
		if (pState == nullptr)
			break;

		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
		{
			if ((state = GetXusbByUserIndex(dwUserIndex)))
			{
				status = includeGuide
					? CALL_FPN_SAFE(FpnXInputGetStateEx, state->RealUserIndex, pState)
					: CALL_FPN_SAFE(FpnXInputGetState, state->RealUserIndex, pState);
			}

			break;
		}

		hid_device* hidDevice = nullptr;
		if (!state->Ds3GetDeviceHandle(&hidDevice))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN]{};
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(hidDevice, buf, ARRAYSIZE(buf));
		if (!ReportMapper::IsValidDs3FeatureRead(res))
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		if (pReport->BatteryStatus == 0)
			break;

		if (!state->Ds3GetPacketNumber(pReport, &pState->dwPacketNumber))
			break;

		ReportMapper::MapDs3ToXInputGamepad(*pReport, pState->Gamepad, includeGuide);
		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

DWORD GlobalState::ProxyXInputGetExtended(_In_ DWORD dwUserIndex, _Out_ SCP_EXTN* pState)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
	);

	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

	do
	{
		if (pState == nullptr)
			break;

		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
			break;

		hid_device* hidDevice = nullptr;
		if (!state->Ds3GetDeviceHandle(&hidDevice))
			break;

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN]{};
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(hidDevice, buf, ARRAYSIZE(buf));
		if (!ReportMapper::IsValidDs3FeatureRead(res))
			break;

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);
		if (pReport->BatteryStatus == 0)
			break;

		ReportMapper::MapDs3ToExtended(*pReport, *pState);
		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

DWORD GlobalState::ProxyXInputGetState(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
	);

	return ReadDs3XInputState(dwUserIndex, pState, false);
}

DWORD GlobalState::ProxyXInputSetState(_In_ DWORD dwUserIndex, _In_ XINPUT_VIBRATION* pVibration)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	DeviceState* state = nullptr;

	do
	{
		if (pVibration == nullptr)
			break;

		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, &state))
		{
			if ((state = GetXusbByUserIndex(dwUserIndex)))
			{
				status = CALL_FPN_SAFE(FpnXInputSetState, state->RealUserIndex, pVibration);
			}

			break;
		}

		hid_device* hidDevice = nullptr;
		if (!state->Ds3GetDeviceHandle(&hidDevice))
			break;

		ds3_output_report outputReport;
		outputReport.rumble.small_motor_on = pVibration->wRightMotorSpeed > 0 ? 1 : 0;
		outputReport.rumble.large_motor_force = static_cast<UCHAR>(
			(static_cast<float>(pVibration->wLeftMotorSpeed) / static_cast<float>(USHRT_MAX)) *
			static_cast<float>(UCHAR_MAX));
		outputReport.led_enabled = ReportMapper::PlayerIndexToDs3LedMask(dwUserIndex);

		const int res = hid_write(hidDevice, &outputReport.report_id, sizeof(outputReport));
		if (res <= 0)
			break;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

DWORD GlobalState::ProxyXInputGetCapabilities(_In_ DWORD dwUserIndex, _In_ DWORD dwFlags, _Out_ XINPUT_CAPABILITIES* pCapabilities)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	DWORD status = ERROR_DEVICE_NOT_CONNECTED;

	do
	{
		if (pCapabilities == nullptr)
			break;

		if (dwFlags != 0 && dwFlags != XINPUT_FLAG_GAMEPAD)
		{
			status = ERROR_BAD_ARGUMENTS;
			break;
		}

		if (!this->GetConnectedDs3ByUserIndex(dwUserIndex, nullptr))
		{
			if (const DeviceState* state = GetXusbByUserIndex(dwUserIndex))
			{
				status = CALL_FPN_SAFE(FpnXInputGetCapabilities, state->RealUserIndex, dwFlags, pCapabilities);
			}

			break;
		}

		ReportMapper::FillXboxGamepadCapabilities(*pCapabilities);
		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

void GlobalState::ProxyXInputEnable(_In_ BOOL enable)
{
	WaitForStartup();

	CALL_FPN_SAFE_NO_RETURN(FpnXInputEnable, enable);
}

DWORD GlobalState::ProxyXInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetDSoundAudioDeviceGuids, state->RealUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetBatteryInformation(_In_ DWORD dwUserIndex,
                                                    _In_ BYTE devType,
                                                    _Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetBatteryInformation, state->RealUserIndex, devType, pBatteryInformation);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputGetKeystroke, state->RealUserIndex, dwReserved, pKeystroke);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputGetStateEx(_In_ DWORD dwUserIndex, _Out_ XINPUT_STATE* pState)
{
	auto scopedSpan = TRACE_SCOPED_SPAN("",
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
	);

	return ReadDs3XInputState(dwUserIndex, pState, true);
}

DWORD GlobalState::ProxyXInputWaitForGuideButton(_In_ DWORD dwUserIndex, _In_ DWORD dwFlag, _In_ LPVOID pVoid)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputWaitForGuideButton, state->RealUserIndex, dwFlag, pVoid);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputCancelGuideButtonWait(_In_ DWORD dwUserIndex)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputCancelGuideButtonWait, state->RealUserIndex);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}

DWORD GlobalState::ProxyXInputPowerOffController(_In_ DWORD dwUserIndex)
{
	WaitForStartup();

	AcquireSRWLockShared(&this->StatesLock);
	ScopeCleanup lockRelease = [this]
	{
		ReleaseSRWLockShared(&this->StatesLock);
	};

	if (const auto state = GetXusbByUserIndex(dwUserIndex))
	{
		return CALL_FPN_SAFE(FpnXInputPowerOffController, state->RealUserIndex);
	}

	return ERROR_DEVICE_NOT_CONNECTED;
}
