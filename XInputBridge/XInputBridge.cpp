// XInputBridge.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "XInputBridge.h"
#include "UniUtil.h"
#include "Macros.h"
#include "Types.h"
#include "GlobalState.h"

GlobalState G_State{};

//
// Device state information of each user index to improve performance
// 
struct XI_DEVICE_STATE
{
	bool isInitialized = false;

	bool isConnected = false;

	hid_device* oldDeviceHandle = nullptr;

	DWORD PacketNumber;

	DS3_RAW_INPUT_REPORT LastReport;

	CRITICAL_SECTION lock;

	//private:
	//
	// Path of backing DS3 or XUSB device
	// 
	std::string SymbolicLink;

	std::atomic<XI_DEVICE_TYPE> Type = XI_DEVICE_TYPE_NOT_CONNECTED;

	union
	{
		hid_device* DeviceHandle = nullptr;
		DWORD UserIndex;
	} Backend;

public:
	void SetOnlineAsXusb(const std::string& Symlink, DWORD UserIndex)
	{
		SymbolicLink = Symlink;
		Backend.UserIndex = UserIndex;
		Type = XI_DEVICE_TYPE_XUSB;
	}
};

//
// Keep track on device states for better lookup performance
// 
static std::vector<XI_DEVICE_STATE> G_DEVICE_STATES(DS3_DEVICES_MAX);

static XI_DEVICE_STATE* GetFreeSlot()
{
	const auto item = std::ranges::find_if(G_DEVICE_STATES,
		[](const XI_DEVICE_STATE& element)
		{
			return element.Type == XI_DEVICE_TYPE_NOT_CONNECTED;
		});

	return (item != G_DEVICE_STATES.end()) ? &(*item) : nullptr;
}

#pragma region Rumble helper types

/*
 * Source: https://github.com/RPCS3/rpcs3/blob/5e436984a2b5753ad340d2c97462bf3be6e86237/rpcs3/Input/ds3_pad_handler.cpp#L9-L40
 */

struct ds3_rumble
{
	UCHAR padding = 0x00;
	UCHAR small_motor_duration = 0xFF; // 0xff means forever
	UCHAR small_motor_on = 0x00; // 0 or 1 (off/on)
	UCHAR large_motor_duration = 0xFF; // 0xff means forever
	UCHAR large_motor_force = 0x00; // 0 to 255
};

struct ds3_led
{
	UCHAR duration = 0xFF; // total duration, 0xff means forever
	UCHAR interval_duration = 0xFF; // interval duration in deciseconds
	UCHAR enabled = 0x10;
	UCHAR interval_portion_off = 0x00; // in percent (100% = 0xFF)
	UCHAR interval_portion_on = 0xFF; // in percent (100% = 0xFF)
};

struct ds3_output_report
{
	UCHAR report_id = 0x00;
	UCHAR idk_what_this_is[3] = { 0x02, 0x00, 0x00 };
	ds3_rumble rumble;
	UCHAR padding[4] = { 0x00, 0x00, 0x00, 0x00 };
	UCHAR led_enabled = 0x00; // LED 1 = 0x02, LED 2 = 0x04, etc.
	ds3_led led[4];
	ds3_led led_5; // reserved for another LED
};

#pragma endregion

#pragma region Utility functions

static SHORT ScaleDsToXi(UCHAR value, BOOLEAN invert)
{
	auto intValue = value - 0x80;
	if (intValue == -128)
		intValue = -127;

	const auto wtfValue = intValue * 258.00787401574803149606299212599f; // what the fuck?

	return static_cast<short>(invert ? -wtfValue : wtfValue);
}

static float ClampAxis(float value)
{
	if (value > 1.0f)
		return 1.0f;
	if (value < -1.0f)
		return -1.0f;
	return value;
}

static float ToAxis(UCHAR value)
{
	return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
}

#pragma endregion

#pragma region Device state handling

#if defined(SCPLIB_ENABLE_TELEMETRY)
static nostd::shared_ptr<trace::Tracer> GetTracer()
{
	const auto provider = trace::Provider::GetTracerProvider();
	return provider->GetTracer("XInputBridge", OPENTELEMETRY_SDK_VERSION);
}
#endif

//
// Marks a given user index HID device as not available
// 
static void SetDeviceDisconnected(DWORD UserIndex)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(UserIndex) }
		}));
#endif

	if (UserIndex >= DS3_DEVICES_MAX)
		return;

	const auto state = &G_DEVICE_STATES[UserIndex];

	EnterCriticalSection(&state->lock);

	state->isConnected = false;
	state->PacketNumber = 0;
	RtlZeroMemory(&state->LastReport, sizeof(DS3_RAW_INPUT_REPORT));

	if (state->oldDeviceHandle)
	{
		hid_close(state->oldDeviceHandle);
		state->oldDeviceHandle = nullptr;
	}

	LeaveCriticalSection(&state->lock);
}

//
// Gets a device handle for a given user index, if it is connected
// 
static bool TryGetDs3DeviceHandle(DWORD UserIndex, hid_device** Handle)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(UserIndex) }
		}));
#endif

	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	bool result = false;
	const auto state = &G_DEVICE_STATES[UserIndex];
	hid_device* device = nullptr;
	struct hid_device_info* devices = nullptr;
	DWORD index = 0;

	do
	{
		if (!state->isInitialized)
			break;

		EnterCriticalSection(&state->lock);

		if (state->isConnected)
		{
			if (Handle)
				*Handle = state->oldDeviceHandle;
			result = true;
			break;
		}

		RtlZeroMemory(&state->LastReport, sizeof(DS3_RAW_INPUT_REPORT));
		state->PacketNumber = 0;

		//
		// Look for device of interest
		// 
		devices = hid_enumerate(DS3_VID, DS3_PID);

		if (devices == nullptr)
			return false;

		const struct hid_device_info* currentDevice = devices;

		while (currentDevice)
		{
			if (index++ == UserIndex)
				break;

			currentDevice = currentDevice->next;
		}

		if (currentDevice == nullptr)
		{
			state->oldDeviceHandle = nullptr;
			state->isConnected = false;
			break;
		}

		device = hid_open_path(currentDevice->path);

		if (device == nullptr)
		{
			state->oldDeviceHandle = nullptr;
			state->isConnected = false;
			break;
		}

		if (Handle)
			*Handle = device;

		state->oldDeviceHandle = device;
		state->isConnected = true;
		result = true;

	} while (FALSE);

	if (devices)
		hid_free_enumeration(devices);

	LeaveCriticalSection(&state->lock);

	return result;
}

//
// Returns a packet number for a given user index that increments only if the input report has changed
// 
static bool GetPacketNumber(DWORD UserIndex, PDS3_RAW_INPUT_REPORT Report, DWORD* PacketNumber)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(UserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	if (UserIndex >= DS3_DEVICES_MAX)
		return false;

	if (!PacketNumber || !Report)
		return false;

	const auto state = &G_DEVICE_STATES[UserIndex];

	//
	// Exclude noisy motion stuff from comparison
	// 
	constexpr size_t bytesToCompare = sizeof(DS3_RAW_INPUT_REPORT) - 18;

	//
	// Only increment when a change happened
	// 
	if (memcmp(
		&state->LastReport,
		Report,
		bytesToCompare
	) != 0)
	{
		state->PacketNumber++;
		memcpy(&state->LastReport, Report, sizeof(DS3_RAW_INPUT_REPORT));
	}

#if defined(SCPLIB_ENABLE_TELEMETRY)
	span->SetAttribute("xinput.packetNumber", std::to_string(state->packetNumber));
#endif

	*PacketNumber = state->PacketNumber;

	return true;
}

#pragma endregion

#pragma region Public exports

//
// Public API exports
// 

XINPUTBRIDGE_API DWORD WINAPI XInputGetExtended(
	_In_ DWORD dwUserIndex,
	_Out_ SCP_EXTN* pState
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	return G_State.ProxyXInputGetExtended(dwUserIndex, pState);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetState(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	do
	{
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!TryGetDs3DeviceHandle(dwUserIndex, &device))
		{
			status = G_State.ProxyXInputGetState(dwUserIndex, pState);
			break;
		}

		UCHAR buf[SXS_MODE_GET_FEATURE_BUFFER_LEN];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		if (!GetPacketNumber(dwUserIndex, pReport, &pState->dwPacketNumber))
			break;

		RtlZeroMemory(&pState->Gamepad, sizeof(pState->Gamepad));

		//
		// D-Pad translation
		// 
		switch (pReport->Buttons.bButtons[0] & ~0xF)
		{
		case 0x10: // N
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			break;
		case 0x30: // NE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x20: // E
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x60: // SE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0x40: // S
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0xC0: // SW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x80: // W
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x90: // NW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		default: // Released
			break;
		}

		//
		// Start/Select
		// 
		if (pReport->Buttons.Individual.Start)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_START;
		if (pReport->Buttons.Individual.Select)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_BACK;

		//
		// Thumbs
		// 
		if (pReport->Buttons.Individual.L3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if (pReport->Buttons.Individual.R3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		//
		// Shoulders
		// 
		if (pReport->Buttons.Individual.L1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (pReport->Buttons.Individual.R1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;

		//
		// Face buttons
		// 
		if (pReport->Buttons.Individual.Triangle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_Y;
		if (pReport->Buttons.Individual.Circle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_B;
		if (pReport->Buttons.Individual.Cross)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_A;
		if (pReport->Buttons.Individual.Square)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_X;

		//
		// Triggers
		// 
		pState->Gamepad.bLeftTrigger = pReport->Pressure.Values.L2;
		pState->Gamepad.bRightTrigger = pReport->Pressure.Values.R2;

		//
		// Thumb axes
		// 
		pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		pState->Gamepad.sThumbRY = ScaleDsToXi(pReport->RightThumbY, TRUE);

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputSetState(
	_In_ DWORD dwUserIndex,
	_In_ XINPUT_VIBRATION* pVibration
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scoped_span = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	do
	{
		//
		// User might troll us
		// 
		if (pVibration == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!TryGetDs3DeviceHandle(dwUserIndex, &device))
		{
			status = G_State.ProxyXInputSetState(dwUserIndex, pVibration);
			break;
		}

		ds3_output_report outputReport;

		// ReSharper disable CppAssignedValueIsNeverUsed
		outputReport.rumble.small_motor_on = pVibration->wRightMotorSpeed > 0 ? 1 : 0;
		outputReport.rumble.large_motor_force = static_cast<float>(pVibration->wLeftMotorSpeed) / static_cast<float>(
			USHRT_MAX) * static_cast<float>(UCHAR_MAX);

		switch (dwUserIndex)
		{
		case 0:
			outputReport.led_enabled = 0b00000010;
			break;
		case 1:
			outputReport.led_enabled = 0b00000100;
			break;
		case 2:
			outputReport.led_enabled = 0b00001000;
			break;
		case 3:
			outputReport.led_enabled = 0b00010000;
			break;
		case 4:
			outputReport.led_enabled = 0b00010010;
			break;
		case 5:
			outputReport.led_enabled = 0b00010100;
			break;
		case 6:
			outputReport.led_enabled = 0b00011000;
			break;
		default:
			break;
		}
		// ReSharper restore CppAssignedValueIsNeverUsed

		const int res = hid_write(device, &outputReport.report_id, sizeof(outputReport));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetCapabilities(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlags,
	_Out_ XINPUT_CAPABILITIES* pCapabilities
)
{
	UNREFERENCED_PARAMETER(dwFlags);
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;

#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	do
	{
		//
		// User might troll us
		// 
		if (pCapabilities == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!TryGetDs3DeviceHandle(dwUserIndex, nullptr))
		{
			status = G_State.ProxyXInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
			break;
		}

		RtlZeroMemory(pCapabilities, sizeof(XINPUT_CAPABILITIES));

		pCapabilities->Type = XINPUT_DEVTYPE_GAMEPAD;
		pCapabilities->SubType = XINPUT_DEVSUBTYPE_GAMEPAD;
		pCapabilities->Flags += XINPUT_CAPS_FFB_SUPPORTED;

		pCapabilities->Gamepad.wButtons = (
			XINPUT_GAMEPAD_DPAD_UP |
			XINPUT_GAMEPAD_DPAD_DOWN |
			XINPUT_GAMEPAD_DPAD_LEFT |
			XINPUT_GAMEPAD_DPAD_RIGHT |
			XINPUT_GAMEPAD_START |
			XINPUT_GAMEPAD_BACK |
			XINPUT_GAMEPAD_LEFT_THUMB |
			XINPUT_GAMEPAD_RIGHT_THUMB |
			XINPUT_GAMEPAD_LEFT_SHOULDER |
			XINPUT_GAMEPAD_RIGHT_SHOULDER |
			XINPUT_GAMEPAD_A |
			XINPUT_GAMEPAD_B |
			XINPUT_GAMEPAD_X |
			XINPUT_GAMEPAD_Y
		);
		pCapabilities->Gamepad.bLeftTrigger = UCHAR_MAX;
		pCapabilities->Gamepad.bRightTrigger = UCHAR_MAX;
		pCapabilities->Gamepad.sThumbLX = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbLY = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbRX = static_cast<SHORT>(0xFFC0);
		pCapabilities->Gamepad.sThumbRY = static_cast<SHORT>(0xFFC0);

		pCapabilities->Vibration.wLeftMotorSpeed = UCHAR_MAX;
		pCapabilities->Vibration.wRightMotorSpeed = UCHAR_MAX;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API void WINAPI XInputEnable(
	_In_ BOOL enable
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__));
#endif

	G_State.ProxyXInputEnable(enable);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetDSoundAudioDeviceGuids(
	DWORD dwUserIndex,
	GUID* pDSoundRenderGuid,
	GUID* pDSoundCaptureGuid
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetDSoundAudioDeviceGuids(dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetBatteryInformation(
	_In_ DWORD dwUserIndex,
	_In_ BYTE devType,
	_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetBatteryInformation(dwUserIndex, devType, pBatteryInformation);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetKeystroke(
	DWORD dwUserIndex,
	DWORD dwReserved,
	PXINPUT_KEYSTROKE pKeystroke
)
{
	UNREFERENCED_PARAMETER(dwUserIndex);
	UNREFERENCED_PARAMETER(dwReserved);
	UNREFERENCED_PARAMETER(pKeystroke);

#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputGetKeystroke(dwUserIndex, dwReserved, pKeystroke);
}

XINPUTBRIDGE_API DWORD WINAPI XInputGetStateEx(
	_In_ DWORD dwUserIndex,
	_Out_ XINPUT_STATE* pState
)
{
	DWORD status = ERROR_DEVICE_NOT_CONNECTED;
	hid_device* device = nullptr;

#if defined(SCPLIB_ENABLE_TELEMETRY)
	auto scopedSpan = trace::Scope(GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		}));
#endif

	do
	{
		//
		// User might troll us
		// 
		if (pState == nullptr)
			break;

		//
		// Look for device of interest
		// 
		if (!TryGetDs3DeviceHandle(dwUserIndex, &device))
		{
			status = G_State.ProxyXInputGetStateEx(dwUserIndex, pState);
			break;
		}

		UCHAR buf[64];
		buf[0] = SXS_MODE_GET_FEATURE_REPORT_ID;

		const int res = hid_get_feature_report(device, buf, ARRAYSIZE(buf));

		if (res <= 0)
		{
			SetDeviceDisconnected(dwUserIndex);
			break;
		}

		const auto pReport = reinterpret_cast<PDS3_RAW_INPUT_REPORT>(&buf[1]);

		if (!GetPacketNumber(dwUserIndex, pReport, &pState->dwPacketNumber))
			break;

		RtlZeroMemory(&pState->Gamepad, sizeof(pState->Gamepad));

		//
		// D-Pad translation
		// 
		switch (pReport->Buttons.bButtons[0] & ~0xF)
		{
		case 0x10: // N
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			break;
		case 0x30: // NE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x20: // E
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			break;
		case 0x60: // SE
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0x40: // S
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			break;
		case 0xC0: // SW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x80: // W
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		case 0x90: // NW
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
			break;
		default: // Released
			break;
		}

		//
		// Start/Select
		// 
		if (pReport->Buttons.Individual.Start)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_START;
		if (pReport->Buttons.Individual.Select)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_BACK;

		//
		// Thumbs
		// 
		if (pReport->Buttons.Individual.L3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if (pReport->Buttons.Individual.R3)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		//
		// Shoulders
		// 
		if (pReport->Buttons.Individual.L1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (pReport->Buttons.Individual.R1)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;

		//
		// Face buttons
		// 
		if (pReport->Buttons.Individual.Triangle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_Y;
		if (pReport->Buttons.Individual.Circle)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_B;
		if (pReport->Buttons.Individual.Cross)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_A;
		if (pReport->Buttons.Individual.Square)
			pState->Gamepad.wButtons |= XINPUT_GAMEPAD_X;

		//
		// Triggers
		// 
		pState->Gamepad.bLeftTrigger = pReport->Pressure.Values.L2;
		pState->Gamepad.bRightTrigger = pReport->Pressure.Values.R2;

		//
		// Thumb axes
		// 
		pState->Gamepad.sThumbLX = ScaleDsToXi(pReport->LeftThumbX, FALSE);
		pState->Gamepad.sThumbLY = ScaleDsToXi(pReport->LeftThumbY, TRUE);
		pState->Gamepad.sThumbRX = ScaleDsToXi(pReport->RightThumbX, FALSE);
		pState->Gamepad.sThumbRY = ScaleDsToXi(pReport->RightThumbY, TRUE);

		//
		// PS/Guide
		// 
		pState->Gamepad.wButtons |= XINPUT_GAMEPAD_GUIDE;

		status = ERROR_SUCCESS;
	} while (FALSE);

	return status;
}

XINPUTBRIDGE_API DWORD WINAPI XInputWaitForGuideButton(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlag,
	_In_ LPVOID pVoid
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputWaitForGuideButton(dwUserIndex, dwFlag, pVoid);
}

XINPUTBRIDGE_API DWORD WINAPI XInputCancelGuideButtonWait(
	_In_ DWORD dwUserIndex
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputCancelGuideButtonWait(dwUserIndex);
}

XINPUTBRIDGE_API DWORD WINAPI XInputPowerOffController(
	_In_ DWORD dwUserIndex
)
{
#if defined(SCPLIB_ENABLE_TELEMETRY)
	const auto span = GetTracer()->StartSpan(__FUNCTION__, {
		{ "xinput.userIndex", std::to_string(dwUserIndex) }
		});
	auto scopedSpan = trace::Scope(span);
#endif

	return G_State.ProxyXInputPowerOffController(dwUserIndex);
}

#pragma endregion
