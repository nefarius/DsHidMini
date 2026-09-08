#pragma once

#include <cstring>
#include <optional>

#include "Common.h"
#include "Macros.h"
#include "Types.h"
#include "XInputBridge.h"

//
// Pure DS3/XInput translation used by the bridge and by scpdlltester.
// Keep this header free of HID/PnP so mapping can be tested without hardware.
//
namespace ReportMapper
{
	inline constexpr int kMinDs3FeatureReportBytes =
		1 + static_cast<int>(sizeof(DS3_RAW_INPUT_REPORT));

	inline constexpr uint8_t kXinputLedToPortMap[] =
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

	inline SHORT ScaleDsToXi(UCHAR value, BOOLEAN invert)
	{
		auto intValue = value - 0x80;
		if (intValue == -128)
			intValue = -127;

		const auto scaled = intValue * 258.00787401574803149606299212599f;

		return static_cast<SHORT>(invert ? -scaled : scaled);
	}

	inline float ClampAxis(float value)
	{
		if (value > 1.0f)
			return 1.0f;
		if (value < -1.0f)
			return -1.0f;
		return value;
	}

	inline float ToAxis(UCHAR value)
	{
		return ClampAxis((((value & 0xFF) - 0x7F) * 2) / 254.0f);
	}

	inline WORD MapDs3DpadButtons(UCHAR hatBits)
	{
		switch (hatBits & ~0xF)
		{
		case 0x10: return XINPUT_GAMEPAD_DPAD_UP;
		case 0x30: return XINPUT_GAMEPAD_DPAD_UP | XINPUT_GAMEPAD_DPAD_RIGHT;
		case 0x20: return XINPUT_GAMEPAD_DPAD_RIGHT;
		case 0x60: return XINPUT_GAMEPAD_DPAD_RIGHT | XINPUT_GAMEPAD_DPAD_DOWN;
		case 0x40: return XINPUT_GAMEPAD_DPAD_DOWN;
		case 0xC0: return XINPUT_GAMEPAD_DPAD_DOWN | XINPUT_GAMEPAD_DPAD_LEFT;
		case 0x80: return XINPUT_GAMEPAD_DPAD_LEFT;
		case 0x90: return XINPUT_GAMEPAD_DPAD_UP | XINPUT_GAMEPAD_DPAD_LEFT;
		default: return 0;
		}
	}

	inline void MapDs3ToXInputGamepad(
		_In_ const DS3_RAW_INPUT_REPORT& report,
		_Out_ XINPUT_GAMEPAD& gamepad,
		_In_ bool includeGuide)
	{
		RtlZeroMemory(&gamepad, sizeof(gamepad));

		gamepad.wButtons = MapDs3DpadButtons(report.Buttons.bButtons[0]);

		if (report.Buttons.Individual.Start)
			gamepad.wButtons |= XINPUT_GAMEPAD_START;
		if (report.Buttons.Individual.Select)
			gamepad.wButtons |= XINPUT_GAMEPAD_BACK;
		if (report.Buttons.Individual.L3)
			gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if (report.Buttons.Individual.R3)
			gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;
		if (report.Buttons.Individual.L1)
			gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_SHOULDER;
		if (report.Buttons.Individual.R1)
			gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_SHOULDER;
		if (report.Buttons.Individual.Triangle)
			gamepad.wButtons |= XINPUT_GAMEPAD_Y;
		if (report.Buttons.Individual.Circle)
			gamepad.wButtons |= XINPUT_GAMEPAD_B;
		if (report.Buttons.Individual.Cross)
			gamepad.wButtons |= XINPUT_GAMEPAD_A;
		if (report.Buttons.Individual.Square)
			gamepad.wButtons |= XINPUT_GAMEPAD_X;
		if (includeGuide && report.Buttons.Individual.PS)
			gamepad.wButtons |= XINPUT_GAMEPAD_GUIDE;

		gamepad.bLeftTrigger = report.Pressure.Values.L2;
		gamepad.bRightTrigger = report.Pressure.Values.R2;

		if (IS_OUTSIDE_DZ(report.LeftThumbX))
			gamepad.sThumbLX = ScaleDsToXi(report.LeftThumbX, FALSE);
		if (IS_OUTSIDE_DZ(report.LeftThumbY))
			gamepad.sThumbLY = ScaleDsToXi(report.LeftThumbY, TRUE);
		if (IS_OUTSIDE_DZ(report.RightThumbX))
			gamepad.sThumbRX = ScaleDsToXi(report.RightThumbX, FALSE);
		if (IS_OUTSIDE_DZ(report.RightThumbY))
			gamepad.sThumbRY = ScaleDsToXi(report.RightThumbY, TRUE);
	}

	inline void MapDs3ToExtended(_In_ const DS3_RAW_INPUT_REPORT& report, _Out_ SCP_EXTN& state)
	{
		RtlZeroMemory(&state, sizeof(state));

		state.SCP_UP = static_cast<float>(report.Pressure.Values.Up) / static_cast<float>(UCHAR_MAX);
		state.SCP_RIGHT = static_cast<float>(report.Pressure.Values.Right) / static_cast<float>(UCHAR_MAX);
		state.SCP_DOWN = static_cast<float>(report.Pressure.Values.Down) / static_cast<float>(UCHAR_MAX);
		state.SCP_LEFT = static_cast<float>(report.Pressure.Values.Left) / static_cast<float>(UCHAR_MAX);

		state.SCP_START = report.Buttons.Individual.Start ? 1.0f : 0.0f;
		state.SCP_SELECT = report.Buttons.Individual.Select ? 1.0f : 0.0f;
		state.SCP_L3 = report.Buttons.Individual.L3 ? 1.0f : 0.0f;
		state.SCP_R3 = report.Buttons.Individual.R3 ? 1.0f : 0.0f;
		state.SCP_L1 = static_cast<float>(report.Pressure.Values.L1) / static_cast<float>(UCHAR_MAX);
		state.SCP_R1 = static_cast<float>(report.Pressure.Values.R1) / static_cast<float>(UCHAR_MAX);
		state.SCP_T = static_cast<float>(report.Pressure.Values.Triangle) / static_cast<float>(UCHAR_MAX);
		state.SCP_C = static_cast<float>(report.Pressure.Values.Circle) / static_cast<float>(UCHAR_MAX);
		state.SCP_X = static_cast<float>(report.Pressure.Values.Cross) / static_cast<float>(UCHAR_MAX);
		state.SCP_S = static_cast<float>(report.Pressure.Values.Square) / static_cast<float>(UCHAR_MAX);
		state.SCP_L2 = static_cast<float>(report.Pressure.Values.L2) / static_cast<float>(UCHAR_MAX);
		state.SCP_R2 = static_cast<float>(report.Pressure.Values.R2) / static_cast<float>(UCHAR_MAX);
		state.SCP_PS = report.Buttons.Individual.PS ? 1.0f : 0.0f;

		if (IS_OUTSIDE_DZ(report.LeftThumbX))
			state.SCP_LX = ToAxis(report.LeftThumbX);
		if (IS_OUTSIDE_DZ(report.LeftThumbY))
			state.SCP_LY = ToAxis(report.LeftThumbY) * -1.0f;
		if (IS_OUTSIDE_DZ(report.RightThumbX))
			state.SCP_RX = ToAxis(report.RightThumbX);
		if (IS_OUTSIDE_DZ(report.RightThumbY))
			state.SCP_RY = ToAxis(report.RightThumbY) * -1.0f;
	}

	inline void FillXboxGamepadCapabilities(_Out_ XINPUT_CAPABILITIES& capabilities)
	{
		RtlZeroMemory(&capabilities, sizeof(capabilities));

		capabilities.Type = XINPUT_DEVTYPE_GAMEPAD;
		capabilities.SubType = XINPUT_DEVSUBTYPE_GAMEPAD;
		capabilities.Flags = XINPUT_CAPS_FFB_SUPPORTED;
		capabilities.Gamepad.wButtons = (
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
		capabilities.Gamepad.bLeftTrigger = UCHAR_MAX;
		capabilities.Gamepad.bRightTrigger = UCHAR_MAX;
		capabilities.Gamepad.sThumbLX = static_cast<SHORT>(0xFFC0);
		capabilities.Gamepad.sThumbLY = static_cast<SHORT>(0xFFC0);
		capabilities.Gamepad.sThumbRX = static_cast<SHORT>(0xFFC0);
		capabilities.Gamepad.sThumbRY = static_cast<SHORT>(0xFFC0);
		capabilities.Vibration.wLeftMotorSpeed = UCHAR_MAX;
		capabilities.Vibration.wRightMotorSpeed = UCHAR_MAX;
	}

	inline UCHAR PlayerIndexToDs3LedMask(DWORD userIndex)
	{
		switch (userIndex)
		{
		case 0: return 0b00000010;
		case 1: return 0b00000100;
		case 2: return 0b00001000;
		case 3: return 0b00010000;
		case 4: return 0b00010010;
		case 5: return 0b00010100;
		case 6: return 0b00011000;
		default: return 0;
		}
	}

	_Success_(return)
	inline bool MapXusbLedStateToUserIndex(_In_ uint8_t ledState, _Out_ DWORD& userIndex)
	{
		if (ledState >= ARRAYSIZE(kXinputLedToPortMap))
			return false;

		const uint8_t mapped = kXinputLedToPortMap[ledState];
		if (mapped == INVALID_X_INPUT_USER_ID)
			return false;

		userIndex = mapped;
		return true;
	}

	inline bool UpdateSyntheticPacketNumber(
		_In_ const DS3_RAW_INPUT_REPORT& report,
		_Inout_ DS3_RAW_INPUT_REPORT& lastReport,
		_Inout_ DWORD& packetNumber)
	{
		if (memcmp(&report, &lastReport, sizeof(DS3_RAW_INPUT_REPORT)) != 0)
		{
			++packetNumber;
			lastReport = report;
		}

		return true;
	}

	inline bool IsValidDs3FeatureRead(int bytesRead)
	{
		return bytesRead >= kMinDs3FeatureReportBytes;
	}

	struct SlotView
	{
		XI_DEVICE_TYPE Type;
		DWORD RealUserIndex;
	};

	inline const SlotView* FindDs3ByUserIndex(const SlotView* slots, size_t count, DWORD userIndex)
	{
		if (slots == nullptr || userIndex >= count)
			return nullptr;

		return slots[userIndex].Type == XI_DEVICE_TYPE_DS3 ? &slots[userIndex] : nullptr;
	}

	inline const SlotView* FindXusbByUserIndex(const SlotView* slots, size_t count, DWORD userIndex)
	{
		if (slots == nullptr)
			return nullptr;

		for (size_t i = 0; i < count; ++i)
		{
			if (slots[i].Type == XI_DEVICE_TYPE_XUSB && slots[i].RealUserIndex == userIndex)
				return &slots[i];
		}

		return nullptr;
	}

	inline std::optional<size_t> FindFreeSlotIndex(const SlotView* slots, size_t count)
	{
		if (slots == nullptr)
			return std::nullopt;

		for (size_t i = 0; i < count; ++i)
		{
			if (slots[i].Type == XI_DEVICE_TYPE_NOT_CONNECTED)
				return i;
		}

		return std::nullopt;
	}
}
