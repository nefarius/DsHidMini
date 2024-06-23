#pragma once

#include <pshpack1.h>

/**
 * Native DualShock 3 Input Report as sent by the device. Starts at and includes Report ID.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	18.04.2021
 */
typedef struct _DS3_RAW_INPUT_REPORT
{
	//
	// Report ID (always 0x01)
	// 
	UCHAR ReportId;

	UCHAR Reserved0;

	//
	// Button breakouts in various formats
	// 
	union
	{
		ULONG lButtons;

		UCHAR bButtons[4];

		struct
		{
			UCHAR Select : 1;
			UCHAR L3 : 1;
			UCHAR R3 : 1;
			UCHAR Start : 1;
			UCHAR Up : 1;
			UCHAR Right : 1;
			UCHAR Down : 1;
			UCHAR Left : 1;
			UCHAR L2 : 1;
			UCHAR R2 : 1;
			UCHAR L1 : 1;
			UCHAR R1 : 1;
			UCHAR Triangle : 1;
			UCHAR Circle : 1;
			UCHAR Cross : 1;
			UCHAR Square : 1;
			UCHAR PS : 1;
			
		} Individual;
	} Buttons;

	//
	// Left Thumb Axes (0x00 = left/bottom, 0x80 = centered, 0xFF = right/top)
	// 
	UCHAR LeftThumbX;
	UCHAR LeftThumbY;

	//
	// Right Thumb Axes (0x00 = left/bottom, 0x80 = centered, 0xFF = right/top)
	// 
	UCHAR RightThumbX;
	UCHAR RightThumbY;

	UCHAR Reserved1[4];

	//
	// Pressure value breakouts in various formats
	// 
	union
	{
		UCHAR bValues[12];

		//
		// Individual sliders (0x00 = disengaged, 0xFF = fully engaged)
		// 
		struct
		{
			UCHAR Up;
			UCHAR Right;
			UCHAR Down;
			UCHAR Left;

			UCHAR L2;
			UCHAR R2;
			
			UCHAR L1;
			UCHAR R1;

			UCHAR Triangle;
			UCHAR Circle;
			UCHAR Cross;
			UCHAR Square;
			
		} Values;
	} Pressure;

	UCHAR Reserved2[4];

	//
	// Battery charge status
	// 
	UCHAR BatteryStatus;

	UCHAR Reserved3[10];

	//
	// Motion information
	// 
	USHORT AccelerometerX;
	USHORT AccelerometerY;
	USHORT AccelerometerZ;
	USHORT Gyroscope;

} DS3_RAW_INPUT_REPORT, * PDS3_RAW_INPUT_REPORT;

#define DS3_RAW_SLIDER_IDLE_THRESHOLD			0x7F // 127 ( (256 * 0,5 ) -1 )
#define DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER		0x3F // 63 ( ( 128 * 0,5 ) - 1 )
#define DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER		0xC0 // 192 ( ( 128 * 1,5 ) - 1 )

/**
 * Checks if the controller state is "idle" (no button pressed, no axis engaged). Jitter
 * compensation is applied to avoid false-positives.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	25.02.2021
 *
 * @param 	Input	The input.
 *
 * @returns	TRUE if idle, FALSE otherwise.
 */
FORCEINLINE BOOLEAN DS3_RAW_IS_IDLE(
	_In_ PDS3_RAW_INPUT_REPORT Input
)
{
	//
	// Button states
	// 

	if (Input->Buttons.lButtons)
	{
		return FALSE;
	}

	//
	// Axes
	// 

	if (
		Input->LeftThumbX < DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER
		|| Input->LeftThumbX > DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER
		|| Input->LeftThumbY < DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER
		|| Input->LeftThumbY > DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER
		|| Input->RightThumbX < DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER
		|| Input->RightThumbX > DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER
		|| Input->RightThumbY < DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER
		|| Input->RightThumbY > DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER
		)
	{
		return FALSE;
	}

	//
	// Sliders
	// 

	if (
		Input->Pressure.Values.L2 > DS3_RAW_SLIDER_IDLE_THRESHOLD
		|| Input->Pressure.Values.R2 > DS3_RAW_SLIDER_IDLE_THRESHOLD
		)
	{
		return FALSE;
	}

	//
	// If we end up here, no movement is going on
	// 

	return TRUE;
}


#include <poppack.h>

/*
 * Source: https://gist.github.com/DJm00n/07e1b7bb21643725e53b16f45e0e7022#file-giphidgamepaddescriptor-txt
 */
#include <pshpack1.h>
typedef struct _XINPUT_HID_INPUT_REPORT
 {
	 // No REPORT ID byte
	 // Collection: CA:GamePad CP:
	 USHORT GD_GamePadX;                              // Usage 0x00010030: X, Value = 0 to 65535
	 USHORT GD_GamePadY;                              // Usage 0x00010031: Y, Value = 0 to 65535
	 USHORT GD_GamePadRx;                             // Usage 0x00010033: Rx, Value = 0 to 65535
	 USHORT GD_GamePadRy;                             // Usage 0x00010034: Ry, Value = 0 to 65535
														// Collection: CA:GamePad
	 USHORT GD_GamePadZ : 10;                         // Usage 0x00010032: Z, Value = 0 to 1023
	 USHORT GD_GamePadRz : 10;                        // Usage 0x00010035: Rz, Value = 0 to 1023
	 UCHAR  BTN_GamePadButton1 : 1;                   // Usage 0x00090001: Button 1 Primary/trigger, Value = 0 to 0
	 UCHAR  BTN_GamePadButton2 : 1;                   // Usage 0x00090002: Button 2 Secondary, Value = 0 to 0
	 UCHAR  BTN_GamePadButton3 : 1;                   // Usage 0x00090003: Button 3 Tertiary, Value = 0 to 0
	 UCHAR  BTN_GamePadButton4 : 1;                   // Usage 0x00090004: Button 4, Value = 0 to 0
	 UCHAR  BTN_GamePadButton5 : 1;                   // Usage 0x00090005: Button 5, Value = 0 to 0
	 UCHAR  BTN_GamePadButton6 : 1;                   // Usage 0x00090006: Button 6, Value = 0 to 0
	 UCHAR  BTN_GamePadButton7 : 1;                   // Usage 0x00090007: Button 7, Value = 0 to 0
	 UCHAR  BTN_GamePadButton8 : 1;                   // Usage 0x00090008: Button 8, Value = 0 to 0
	 UCHAR  BTN_GamePadButton9 : 1;                   // Usage 0x00090009: Button 9, Value = 0 to 0
	 UCHAR  BTN_GamePadButton10 : 1;                  // Usage 0x0009000A: Button 10, Value = 0 to 0
	 UCHAR : 6;                                      // Pad
	 UCHAR  GD_GamePadHatSwitch : 4;                  // Usage 0x00010039: Hat switch, Value = 1 to 8, Physical = (Value - 1) x 45 in degrees
	 UCHAR : 4;                                      // Pad
														// Collection: CA:GamePad CP:SystemControl
	 UCHAR  GD_GamePadSystemControlSystemMainMenu : 1; // Usage 0x00010085: System Main Menu, Value = 0 to 1
	 UCHAR : 7;                                      // Pad
														// Collection: CA:GamePad
	 UCHAR  GEN_GamePadBatteryStrength;               // Usage 0x00060020: Battery Strength, Value = 0 to 255
 } XINPUT_HID_INPUT_REPORT, * PXINPUT_HID_INPUT_REPORT;
#include <poppack.h>
