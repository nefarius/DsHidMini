// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the XINPUTBRIDGE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// XINPUTBRIDGE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef XINPUTBRIDGE_EXPORTS
#define XINPUTBRIDGE_API __declspec(dllexport)
#else
#define XINPUTBRIDGE_API __declspec(dllimport)
#endif


#pragma region XInput types

#define XINPUT_GAMEPAD_DPAD_UP          0x0001
#define XINPUT_GAMEPAD_DPAD_DOWN        0x0002
#define XINPUT_GAMEPAD_DPAD_LEFT        0x0004
#define XINPUT_GAMEPAD_DPAD_RIGHT       0x0008
#define XINPUT_GAMEPAD_START            0x0010
#define XINPUT_GAMEPAD_BACK             0x0020
#define XINPUT_GAMEPAD_LEFT_THUMB       0x0040
#define XINPUT_GAMEPAD_RIGHT_THUMB      0x0080
#define XINPUT_GAMEPAD_LEFT_SHOULDER    0x0100
#define XINPUT_GAMEPAD_RIGHT_SHOULDER   0x0200
#define XINPUT_GAMEPAD_A                0x1000
#define XINPUT_GAMEPAD_B                0x2000
#define XINPUT_GAMEPAD_X                0x4000
#define XINPUT_GAMEPAD_Y				0x8000

#define XINPUT_CAPS_FFB_SUPPORTED       0x0001
#define XINPUT_CAPS_WIRELESS            0x0002
#define XINPUT_CAPS_PMD_SUPPORTED       0x0008
#define XINPUT_CAPS_NO_NAVIGATION       0x0010

//
// Flags for battery status level
//
#define BATTERY_TYPE_DISCONNECTED       0x00    // This device is not connected
#define BATTERY_TYPE_WIRED              0x01    // Wired device, no battery
#define BATTERY_TYPE_ALKALINE           0x02    // Alkaline battery source
#define BATTERY_TYPE_NIMH               0x03    // Nickel Metal Hydride battery source
#define BATTERY_TYPE_UNKNOWN            0xFF    // Cannot determine the battery type

// These are only valid for wireless, connected devices, with known battery types
// The amount of use time remaining depends on the type of device.
#define BATTERY_LEVEL_EMPTY             0x00
#define BATTERY_LEVEL_LOW               0x01
#define BATTERY_LEVEL_MEDIUM            0x02
#define BATTERY_LEVEL_FULL              0x03


#define XINPUT_DEVTYPE_GAMEPAD          0x01
#define XINPUT_DEVSUBTYPE_GAMEPAD       0x01

#define BATTERY_TYPE_DISCONNECTED		0x00

#define XUSER_MAX_COUNT                 4
#define MAX_PLAYER_COUNT				8
#define XUSER_INDEX_ANY					0x000000FF

//
// Structures used by XInput APIs
//
typedef struct _XINPUT_GAMEPAD
{
	WORD                                wButtons;
	BYTE                                bLeftTrigger;
	BYTE                                bRightTrigger;
	SHORT                               sThumbLX;
	SHORT                               sThumbLY;
	SHORT                               sThumbRX;
	SHORT                               sThumbRY;
} XINPUT_GAMEPAD, * PXINPUT_GAMEPAD;

typedef struct _XINPUT_STATE
{
	DWORD                               dwPacketNumber;
	XINPUT_GAMEPAD                      Gamepad;
} XINPUT_STATE, * PXINPUT_STATE;

typedef struct _XINPUT_VIBRATION
{
	WORD                                wLeftMotorSpeed;
	WORD                                wRightMotorSpeed;
} XINPUT_VIBRATION, * PXINPUT_VIBRATION;

typedef struct _XINPUT_CAPABILITIES
{
	BYTE                                Type;
	BYTE                                SubType;
	WORD                                Flags;
	XINPUT_GAMEPAD                      Gamepad;
	XINPUT_VIBRATION                    Vibration;
} XINPUT_CAPABILITIES, * PXINPUT_CAPABILITIES;

typedef struct _XINPUT_BATTERY_INFORMATION
{
	BYTE BatteryType;
	BYTE BatteryLevel;
} XINPUT_BATTERY_INFORMATION, * PXINPUT_BATTERY_INFORMATION;

typedef struct _XINPUT_KEYSTROKE
{
	WORD    VirtualKey;
	WCHAR   Unicode;
	WORD    Flags;
	BYTE    UserIndex;
	BYTE    HidCode;
} XINPUT_KEYSTROKE, * PXINPUT_KEYSTROKE;

#pragma endregion



XINPUTBRIDGE_API DWORD WINAPI XInputGetExtended(
	_In_ DWORD dwUserIndex,
	_Out_ SCP_EXTN* pState
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetState(
	_In_ DWORD dwUserIndex, 
	_Out_ XINPUT_STATE* pState
);

XINPUTBRIDGE_API DWORD WINAPI XInputSetState(
	_In_ DWORD dwUserIndex,
	_In_ XINPUT_VIBRATION* pVibration
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetCapabilities(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlags,
	_Out_ XINPUT_CAPABILITIES* pCapabilities
);

XINPUTBRIDGE_API void WINAPI XInputEnable(
	_In_ BOOL enable
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetDSoundAudioDeviceGuids(
	DWORD dwUserIndex,
	GUID* pDSoundRenderGuid,
	GUID* pDSoundCaptureGuid
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetBatteryInformation(
	_In_ DWORD dwUserIndex,
	_In_ BYTE devType,
	_Out_ XINPUT_BATTERY_INFORMATION* pBatteryInformation
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetKeystroke(
	DWORD dwUserIndex,
	DWORD dwReserved,
	PXINPUT_KEYSTROKE pKeystroke
);

XINPUTBRIDGE_API DWORD WINAPI XInputGetStateEx(
	_In_ DWORD dwUserIndex, 
	_Out_ XINPUT_STATE* pState
);

XINPUTBRIDGE_API DWORD WINAPI XInputWaitForGuideButton(
	_In_ DWORD dwUserIndex,
	_In_ DWORD dwFlag,
	_In_ LPVOID pVoid
);

XINPUTBRIDGE_API DWORD WINAPI XInputCancelGuideButtonWait(
	_In_ DWORD dwUserIndex
);

XINPUTBRIDGE_API DWORD WINAPI XInputPowerOffController(
	_In_ DWORD dwUserIndex
);
