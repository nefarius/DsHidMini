#include "Driver.h"
#include "DsHid.tmh"
#ifdef DSHM_FEATURE_FFB
#include "PID/PIDTypes.h"
#endif
#include <math.h>

#pragma region DS3 HID Report Descriptor (Split Device Mode)

CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Split_Mode[] =
{
	/************************************************************************/
	/* Gamepad definition (for regular DS3 buttons, axes & features)        */
	/************************************************************************/
#include "HID/02_GPJ_Col1_Gamepad.h"
#ifdef DSHM_FEATURE_FFB
#include "PID/01_PIDStateReport.h"
#include "PID/02_SetEffectReport.h"
#include "PID/03_SetEnvelopeReport.h"
#include "PID/04_SetConditionReport.h"
#include "PID/05_SetPeriodicReport.h"
#include "PID/06_SetConstantForceReport.h"
#include "PID/07_SetRampForceReport.h"
#include "PID/08_CustomForceDataReport.h"
#include "PID/09_DownloadForceSample.h"
#include "PID/10_EffectOperationReport.h"
#include "PID/11_PIDBlockFreeReport.h"
#include "PID/12_PIDDeviceControl.h"
#include "PID/13_DeviceGainReport.h"
#include "PID/14_SetCustomForceReport.h"
#include "PID/15_CreateNewEffectReport.h"
#include "PID/16_PIDBlockLoadReport.h"
#include "PID/17_PIDPoolReport.h"
#endif
	0xC0,              // End Collection
	/************************************************************************/
	/* Joystick definition (for exposing pressure values as axes)           */
	/************************************************************************/
#include "HID/02_GPJ_Col2_Joystick.h"
};

CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Split_Mode = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_Ds3HidReportDescriptor_Split_Mode) }  // total length of report descriptor
};

#pragma endregion

#pragma region DS3 HID Report Descriptor (Single Device Mode)

CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Single_Mode[] =
{
	/************************************************************************/
	/* Gamepad definition with pressure axes in one report                  */
	/************************************************************************/
#include "HID/01_SDF_Col1_GamePad.h"
#ifdef DSHM_FEATURE_FFB
#include "PID/01_PIDStateReport.h"
#include "PID/02_SetEffectReport.h"
#include "PID/03_SetEnvelopeReport.h"
#include "PID/04_SetConditionReport.h"
#include "PID/05_SetPeriodicReport.h"
#include "PID/06_SetConstantForceReport.h"
#include "PID/07_SetRampForceReport.h"
#include "PID/08_CustomForceDataReport.h"
#include "PID/09_DownloadForceSample.h"
#include "PID/10_EffectOperationReport.h"
#include "PID/11_PIDBlockFreeReport.h"
#include "PID/12_PIDDeviceControl.h"
#include "PID/13_DeviceGainReport.h"
#include "PID/14_SetCustomForceReport.h"
#include "PID/15_CreateNewEffectReport.h"
#include "PID/16_PIDBlockLoadReport.h"
#include "PID/17_PIDPoolReport.h"
#endif
	0xC0,              // End Collection
};

CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Single_Mode = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_Ds3HidReportDescriptor_Single_Mode) }  // total length of report descriptor
};

#pragma endregion

#pragma region DS3 HID Report Descriptor (SIXAXIS.SYS compatible)

CONST HID_REPORT_DESCRIPTOR G_SixaxisHidReportDescriptor[] =
{
	/************************************************************************/
	/* SIXAXIS.SYS compatible report descriptor                             */
	/************************************************************************/
#include "HID/03_SXS_Col1_Joystick.h"
};

CONST HID_DESCRIPTOR G_SixaxisHidDescriptor = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_SixaxisHidReportDescriptor) }  // total length of report descriptor
};

#pragma endregion

#pragma region DS3 HID Report Descriptor (Vendor Defined DS4 Rev1 USB emulation)

CONST HID_REPORT_DESCRIPTOR G_VendorDefinedUSBDS4HidReportDescriptor[] =
{
	/************************************************************************/
	/* Vendor Defined DualShock 4 Rev1 USB compatible report descriptor     */
	/************************************************************************/
#include "HID/04_DS4_Col1_VendorDefined.h"
};

CONST HID_DESCRIPTOR G_VendorDefinedUSBDS4HidDescriptor = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_VendorDefinedUSBDS4HidReportDescriptor) }  // total length of report descriptor
};

#pragma endregion

#pragma region DS3 HID Report Descriptor (XINPUT compatible HID device)

/*
 * Source: https://gist.github.com/DJm00n/07e1b7bb21643725e53b16f45e0e7022#file-giphidgamepaddescriptor-txt
 */
CONST HID_REPORT_DESCRIPTOR G_XInputHIDCompatible_HidReportDescriptor[] =
{
#include "HID/05_XIH_Col1_XInputHID.h"
};

CONST HID_DESCRIPTOR G_XInputHIDCompatible_HidDescriptor = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_XInputHIDCompatible_HidReportDescriptor) }  // total length of report descriptor
};

#pragma endregion


//
// Applies transformations on a thumb axis pair
// 
void DS3_RAW_AXIS_TRANSFORM(
	_In_ const UCHAR InputX,
	_In_ const UCHAR InputY,
	_Inout_ PUCHAR OutputX,
	_Inout_ PUCHAR OutputY,
	_In_ const BOOLEAN ApplyDeadZone,
	_In_ const DOUBLE DeadZonePolarValue,
	_In_ const BOOLEAN FlipX,
	_In_ const BOOLEAN FlipY
)
{
	UCHAR modifiedX = InputX;
	UCHAR modifiedY = InputY;

	if (FlipX)
	{
		modifiedX = (UCHAR)abs(InputX - 0xFF);
	}
	if (FlipY)
	{
		modifiedY = (UCHAR)abs(InputY - 0xFF);
	}

	if (!ApplyDeadZone)
	{
		*OutputX = modifiedX;
		*OutputY = modifiedY;
		return;
	}

	//
	// 0x80 is centered, but working from 0 to positive
	// values makes the following calculations easier
	// 
	const int x = abs((int)modifiedX - 0x80);
	const int y = abs((int)modifiedY - 0x80);

	//
	// Calculate dead zone circle area
	// 
	const double r = sqrt(x * x + y * y);

	//
	// If we're outside of the dead zone, report non-default values
	// 
	if (r > DeadZonePolarValue)
	{
		*OutputX = modifiedX;
		*OutputY = modifiedY;
	}
	else
	{
		*OutputX = 0x80;
		*OutputY = 0x80;
	}
}

VOID DS3_RAW_TO_GPJ_HID_INPUT_REPORT_01(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PUCHAR Output,
	_In_ const DS_PRESSURE_EXPOSURE_MODE PressureMode,
	_In_ const DS_DPAD_EXPOSURE_MODE DPadExposureMode,
	_In_ const PDS_THUMB_SETTINGS ThumbSettings,
	_In_ const PDS_FLIP_AXIS_SETTINGS FlipAxis
)
{
	// Report ID
	Output[0] = 0x01;

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Prepare buttons: L2, R2, L1, R1, L3, R3, Select and Start
	Output[6] &= ~0xFF; // Clear all 8 bits

	// Prepare PS and D-Pad buttons
	Output[7] &= ~0xFF; // Clear all 8 bits

	if ((PressureMode & DsPressureExposureModeDigital) != 0)
	{
		// Translate D-Pad to HAT format
		if ((DPadExposureMode & DsDPadExposureModeHAT) != 0)
		{
			switch (Input->Buttons.bButtons[0] & ~0xF)
			{
			case 0x10: // N
				Output[5] |= 0 & 0xF;
				break;
			case 0x30: // NE
				Output[5] |= 1 & 0xF;
				break;
			case 0x20: // E
				Output[5] |= 2 & 0xF;
				break;
			case 0x60: // SE
				Output[5] |= 3 & 0xF;
				break;
			case 0x40: // S
				Output[5] |= 4 & 0xF;
				break;
			case 0xC0: // SW
				Output[5] |= 5 & 0xF;
				break;
			case 0x80: // W
				Output[5] |= 6 & 0xF;
				break;
			case 0x90: // NW
				Output[5] |= 7 & 0xF;
				break;
			default: // Released
				Output[5] |= 8 & 0xF;
				break;
			}
		}
		else {
			// Clear HAT position
			Output[5] |= 8 & 0xF;
		}

		// Set face buttons
		Output[5] |= Input->Buttons.bButtons[1] & 0xF0; // OUTPUT: SQUARE [7], CROSS [6], CIRCLE [5], TRIANGLE [4]

		// Remaining buttons
		Output[6] |= (Input->Buttons.bButtons[0] & 0xF); // OUTPUT: START [3], RSB [2], LSB [1], SELECT [0]
		Output[6] |= (Input->Buttons.bButtons[1] & 0xF) << 4; // OUTPUT: R1 [7], L1 [6], R2 [5], L2 [4]

		// D-Pad (Buttons)
		if ((DPadExposureMode & DsDPadExposureModeIndividualButtons) != 0)
		{
			Output[7] |= (Input->Buttons.bButtons[0] & ~0xF) >> 3; // OUTPUT: LEFT [4], DOWN [3], RIGHT [2], UP [1]
		}
	}
	else
	{
		// Clear HAT position
		Output[5] |= 8 & 0xF;
	}

	// PS button
	Output[7] |= Input->Buttons.Individual.PS; // OUTPUT: PS BUTTON [0]

	// Thumb axes
	DS3_RAW_AXIS_TRANSFORM(
		Input->LeftThumbX,
		Input->LeftThumbY,
		&Output[1],
		&Output[2],
		ThumbSettings->DeadZoneLeft.Apply,
		ThumbSettings->DeadZoneLeft.PolarValue,
		FlipAxis->LeftX,
		FlipAxis->LeftY
	);
	DS3_RAW_AXIS_TRANSFORM(
		Input->RightThumbX,
		Input->RightThumbY,
		&Output[3],
		&Output[4],
		ThumbSettings->DeadZoneRight.Apply,
		ThumbSettings->DeadZoneRight.PolarValue,
		FlipAxis->RightX,
		FlipAxis->RightY
	);

	// Trigger axes
	Output[8] = Input->Pressure.Values.L2;
	Output[9] = Input->Pressure.Values.R2;

	// Shoulders (pressure)
	Output[10] = Input->Pressure.Values.L1;
	Output[11] = Input->Pressure.Values.R1;

}

VOID DS3_RAW_TO_GPJ_HID_INPUT_REPORT_02(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PUCHAR Output
)
{
	// Report ID
	Output[0] = 0x02;

	// D-Pad (pressure)
	Output[1] = Input->Pressure.Values.Up;
	Output[2] = Input->Pressure.Values.Right;
	Output[3] = Input->Pressure.Values.Down;
	Output[4] = Input->Pressure.Values.Left;

	// Face buttons (pressure)
	Output[5] = Input->Pressure.Values.Triangle;
	Output[6] = Input->Pressure.Values.Circle;
	Output[7] = Input->Pressure.Values.Cross;
	Output[8] = Input->Pressure.Values.Square;
}

VOID DS3_RAW_TO_SDF_HID_INPUT_REPORT(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PUCHAR Output,
	_In_ const DS_PRESSURE_EXPOSURE_MODE PressureMode,
	_In_ const DS_DPAD_EXPOSURE_MODE DPadExposureMode,
	_In_ const PDS_THUMB_SETTINGS ThumbSettings,
	_In_ const PDS_FLIP_AXIS_SETTINGS FlipAxis
)
{
	// Report ID
	Output[0] = Input->ReportId;

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Prepare buttons: L2, R2, L1, R1, L3, R3, Select and Start
	Output[6] &= ~0xFF; // Clear all 8 bits

	// Prepare PS and D-Pad buttons
	Output[7] &= ~0xFF; // Clear all 8 bits

	if ((PressureMode & DsPressureExposureModeDigital) != 0)
	{
		// Translate D-Pad to HAT format
		if ((DPadExposureMode & DsDPadExposureModeHAT) != 0)
		{
			switch (Input->Buttons.bButtons[0] & ~0xF)
			{
			case 0x10: // N
				Output[5] |= 0 & 0xF;
				break;
			case 0x30: // NE
				Output[5] |= 1 & 0xF;
				break;
			case 0x20: // E
				Output[5] |= 2 & 0xF;
				break;
			case 0x60: // SE
				Output[5] |= 3 & 0xF;
				break;
			case 0x40: // S
				Output[5] |= 4 & 0xF;
				break;
			case 0xC0: // SW
				Output[5] |= 5 & 0xF;
				break;
			case 0x80: // W
				Output[5] |= 6 & 0xF;
				break;
			case 0x90: // NW
				Output[5] |= 7 & 0xF;
				break;
			default: // Released
				Output[5] |= 8 & 0xF;
				break;
			}
		}
		else {
			// Clear HAT position
			Output[5] |= 8 & 0xF;
		}

		// Set face buttons
		Output[5] |= Input->Buttons.bButtons[1] & 0xF0; // OUTPUT: SQUARE[7], CROSS[6], CIRCLE[5], TRIANGLE[4]

		// Remaining buttons
		Output[6] |= (Input->Buttons.bButtons[0] & 0xF);  // OUTPUT: START [3], RSB [2], LSB [1], SELECT [0]
		Output[6] |= (Input->Buttons.bButtons[1] & 0xF) << 4; // OUTPUT: R1 [7], L1 [6], R2 [5], L2 [4]

		// D-Pad (Buttons)
		if ((DPadExposureMode & DsDPadExposureModeIndividualButtons) != 0)
		{
			Output[7] |= (Input->Buttons.bButtons[0] & ~0xF) >> 3; // OUTPUT: LEFT [4], DOWN [3], RIGHT [2], UP [1]
		}
	}
	else {
		// Clear HAT position
		Output[5] |= 8 & 0xF;
	}
	
	// Thumb axes
	DS3_RAW_AXIS_TRANSFORM(
		Input->LeftThumbX,
		Input->LeftThumbY,
		&Output[1],
		&Output[2],
		ThumbSettings->DeadZoneLeft.Apply,
		ThumbSettings->DeadZoneLeft.PolarValue,
		FlipAxis->LeftX,
		FlipAxis->LeftY
	);
	DS3_RAW_AXIS_TRANSFORM(
		Input->RightThumbX,
		Input->RightThumbY,
		&Output[3],
		&Output[4],
		ThumbSettings->DeadZoneRight.Apply,
		ThumbSettings->DeadZoneRight.PolarValue,
		FlipAxis->RightX,
		FlipAxis->RightY
	);

	// Trigger axes
	Output[8] = Input->Pressure.Values.L2;
	Output[9] = Input->Pressure.Values.R2;

	// PS button
	Output[7] |= Input->Buttons.Individual.PS;

	if ((PressureMode & DsPressureExposureModeAnalogue) != 0)
	{
		// D-Pad (pressure)
		Output[10] = Input->Pressure.Values.Up;
		Output[11] = Input->Pressure.Values.Right;
		Output[12] = Input->Pressure.Values.Down;
		Output[13] = Input->Pressure.Values.Left;

		// Shoulders (pressure)
		Output[14] = Input->Pressure.Values.L1;
		Output[15] = Input->Pressure.Values.R1;

		// Face buttons (pressure)
		Output[16] = Input->Pressure.Values.Triangle;
		Output[17] = Input->Pressure.Values.Circle;
		Output[18] = Input->Pressure.Values.Cross;
		Output[19] = Input->Pressure.Values.Square;
	}
}

VOID DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PUCHAR Output,
	_In_ const PDS_THUMB_SETTINGS ThumbSettings,
	_In_ const PDS_FLIP_AXIS_SETTINGS FlipAxis
)
{
	// Prepare D-Pad
	Output[3] &= ~0xF; // Clear lower 4 bits

	// Translate D-Pad to HAT format
	switch (Input->Buttons.bButtons[0] & ~0xF)
	{
	case 0x10: // N
		Output[3] |= 0 & 0xF;
		break;
	case 0x30: // NE
		Output[3] |= 1 & 0xF;
		break;
	case 0x20: // E
		Output[3] |= 2 & 0xF;
		break;
	case 0x60: // SE
		Output[3] |= 3 & 0xF;
		break;
	case 0x40: // S
		Output[3] |= 4 & 0xF;
		break;
	case 0xC0: // SW
		Output[3] |= 5 & 0xF;
		break;
	case 0x80: // W
		Output[3] |= 6 & 0xF;
		break;
	case 0x90: // NW
		Output[3] |= 7 & 0xF;
		break;
	default: // Released
		Output[3] |= 8 & 0xF;
		break;
	}

	// Thumb axes
	DS3_RAW_AXIS_TRANSFORM(
		Input->LeftThumbX,
		Input->LeftThumbY,
		&Output[4],
		&Output[5],
		ThumbSettings->DeadZoneLeft.Apply,
		ThumbSettings->DeadZoneLeft.PolarValue,
		FlipAxis->LeftX,
		FlipAxis->LeftY
	);
	DS3_RAW_AXIS_TRANSFORM(
		Input->RightThumbX,
		Input->RightThumbY,
		&Output[6],
		&Output[7],
		ThumbSettings->DeadZoneRight.Apply,
		ThumbSettings->DeadZoneRight.PolarValue,
		FlipAxis->RightX,
		FlipAxis->RightY
	);

	// Buttons
	Output[0] &= ~0xFF; // Clear all 8 bits
	Output[1] &= ~0xFF; // Clear all 8 bits

	// Face buttons
	Output[0] |= ((Input->Buttons.bButtons[1] & 0xF0) >> 4);
	// L2, R2, L1, R1
	Output[0] |= ((Input->Buttons.bButtons[1] & 0x0F) << 4);

	// Select
	Output[1] |= ((Input->Buttons.bButtons[0] & 0x01) << 1);
	// Start
	Output[1] |= ((Input->Buttons.bButtons[0] & 0x08) >> 3);
	// L3
	Output[1] |= ((Input->Buttons.bButtons[0] & 0x02) << 1);
	// R3
	Output[1] |= ((Input->Buttons.bButtons[0] & 0x04) << 1);
	// PS
	Output[1] |= ((Input->Buttons.bButtons[2] & 0x01) << 4);

	// Trigger axes (inverted)
	Output[10] = (0xFF - Input->Pressure.Values.L2);
	Output[11] = (0xFF - Input->Pressure.Values.R2);

	// Face buttons (pressure, inverted)
	Output[8] = (0xFF - Input->Pressure.Values.Circle);
	Output[9] = (0xFF - Input->Pressure.Values.Cross);
}

UCHAR REVERSE_BITS(_In_ UCHAR x)
{
	x = ((x >> 1) & 0x55) | ((x << 1) & 0xaa);
	x = ((x >> 2) & 0x33) | ((x << 2) & 0xcc);
	x = ((x >> 4) & 0x0f) | ((x << 4) & 0xf0);
	return x;
}

VOID DS3_RAW_TO_DS4WINDOWS_HID_INPUT_REPORT(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PUCHAR Output,
	_In_ const BOOLEAN IsWired,
	_In_ const PDS_THUMB_SETTINGS ThumbSettings,
	_In_ const PDS_FLIP_AXIS_SETTINGS FlipAxis
)
{
	// Report ID
	Output[0] = Input->ReportId;

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Remaining buttons
	Output[6] &= ~0xFF; // Clear all 8 bits

	// PS button
	Output[7] &= ~0x01; // Clear button bit

	// Battery + cable info
	Output[30] &= ~0xF; // Clear lower 4 bits

	// Finger 1 touchpad contact info
	Output[35] |= 0x80; // Set top bit to disable finger contact
	Output[44] |= 0x80; // Set top bit to disable finger contact

	// Finger 2 touchpad contact info
	Output[39] |= 0x80; // Set top bit to disable finger contact
	Output[48] |= 0x80; // Set top bit to disable finger contact

	// Translate D-Pad to HAT format
	switch (Input->Buttons.bButtons[0] & ~0xF)
	{
	case 0x10: // N
		Output[5] |= 0 & 0xF;
		break;
	case 0x30: // NE
		Output[5] |= 1 & 0xF;
		break;
	case 0x20: // E
		Output[5] |= 2 & 0xF;
		break;
	case 0x60: // SE
		Output[5] |= 3 & 0xF;
		break;
	case 0x40: // S
		Output[5] |= 4 & 0xF;
		break;
	case 0xC0: // SW
		Output[5] |= 5 & 0xF;
		break;
	case 0x80: // W
		Output[5] |= 6 & 0xF;
		break;
	case 0x90: // NW
		Output[5] |= 7 & 0xF;
		break;
	default: // Released
		Output[5] |= 8 & 0xF;
		break;
	}

	// Face buttons
	Output[5] |= ((REVERSE_BITS(Input->Buttons.bButtons[1]) << 4) & 0xF0);

	// Select to Share
	Output[6] |= ((Input->Buttons.bButtons[0] & 0x01) << 4);

	// Start to Options
	Output[6] |= (((Input->Buttons.bButtons[0] >> 3) & 0x01) << 5);

	// L1, L2, R1, R2
	Output[6] |= (((Input->Buttons.bButtons[1] >> 2) & 0x01) << 0);
	Output[6] |= (((Input->Buttons.bButtons[1] >> 0) & 0x01) << 2);
	Output[6] |= (((Input->Buttons.bButtons[1] >> 3) & 0x01) << 1);
	Output[6] |= (((Input->Buttons.bButtons[1] >> 1) & 0x01) << 3);

	// L3, R3
	Output[6] |= (((Input->Buttons.bButtons[0] >> 1) & 0x01) << 6);
	Output[6] |= (((Input->Buttons.bButtons[0] >> 2) & 0x01) << 7);

	// Thumb axes
	DS3_RAW_AXIS_TRANSFORM(
		Input->LeftThumbX,
		Input->LeftThumbY,
		&Output[1],
		&Output[2],
		ThumbSettings->DeadZoneLeft.Apply,
		ThumbSettings->DeadZoneLeft.PolarValue,
		FlipAxis->LeftX,
		FlipAxis->LeftY
	);
	DS3_RAW_AXIS_TRANSFORM(
		Input->RightThumbX,
		Input->RightThumbY,
		&Output[3],
		&Output[4],
		ThumbSettings->DeadZoneRight.Apply,
		ThumbSettings->DeadZoneRight.PolarValue,
		FlipAxis->RightX,
		FlipAxis->RightY
	);

	// Trigger axes
	Output[8] = Input->Pressure.Values.L2;
	Output[9] = Input->Pressure.Values.R2;

	// PS button
	Output[7] |= Input->Buttons.Individual.PS;

	// Battery translation when IsWired = 0: ( Value * 100 ) / 8
	// Battery translation when IsWired = 1: ( Value * 100 ) / 11
	if (IsWired)
	{
		// Wired sets a flag
		Output[30] |= 0x10;

		switch ((DS_BATTERY_STATUS)Input->BatteryStatus)
		{
		case DsBatteryStatusCharging:
			Output[30] |= 4; // 36%
			break;
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			Output[30] |= 11; // 100%
			break;
		}
	}
	else
	{
		// Clear flag
		Output[30] &= ~0x10;

		switch ((DS_BATTERY_STATUS)Input->BatteryStatus)
		{
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			Output[30] |= 8; // 100%
			break;
		case DsBatteryStatusHigh:
			Output[30] |= 6; // 75%
			break;
		case DsBatteryStatusMedium:
			Output[30] |= 4; // 50%
			break;
		case DsBatteryStatusLow:
			Output[30] |= 2; // 25%
			break;
		case DsBatteryStatusDying:
			Output[30] |= 1; // 12%
			break;
		}
	}
}

VOID DS3_RAW_TO_XINPUTHID_HID_INPUT_REPORT(
	_In_ const PDS3_RAW_INPUT_REPORT Input,
	_Out_ PXINPUT_HID_INPUT_REPORT Output,
	_In_ const PDS_THUMB_SETTINGS ThumbSettings,
	_In_ const PDS_FLIP_AXIS_SETTINGS FlipAxis
)
{
	UCHAR leftThumbX = Input->LeftThumbX;
	UCHAR leftThumbY = Input->LeftThumbY;
	UCHAR rightThumbX = Input->RightThumbX;
	UCHAR rightThumbY = Input->RightThumbY;

	//
	// Thumb axes
	// 
	DS3_RAW_AXIS_TRANSFORM(
		Input->LeftThumbX,
		Input->LeftThumbY,
		&leftThumbX,
		&leftThumbY,
		ThumbSettings->DeadZoneLeft.Apply,
		ThumbSettings->DeadZoneLeft.PolarValue,
		FlipAxis->LeftX,
		FlipAxis->LeftY
	);
	DS3_RAW_AXIS_TRANSFORM(
		Input->RightThumbX,
		Input->RightThumbY,
		&rightThumbX,
		&rightThumbY,
		ThumbSettings->DeadZoneRight.Apply,
		ThumbSettings->DeadZoneRight.PolarValue,
		FlipAxis->RightX,
		FlipAxis->RightY
	);
	Output->GD_GamePadX = leftThumbX * 257;
	Output->GD_GamePadY = leftThumbY * 257;
	Output->GD_GamePadRx = rightThumbX * 257;
	Output->GD_GamePadRy = rightThumbY * 257;

	//
	// Triggers
	// 
	Output->GD_GamePadZ = Input->Pressure.Values.L2 * 4;
	Output->GD_GamePadRz = Input->Pressure.Values.R2 * 4;

	//
	// Face
	// 
	Output->BTN_GamePadButton1 = Input->Buttons.Individual.Cross;
	Output->BTN_GamePadButton2 = Input->Buttons.Individual.Circle;
	Output->BTN_GamePadButton3 = Input->Buttons.Individual.Square;
	Output->BTN_GamePadButton4 = Input->Buttons.Individual.Triangle;

	//
	// Shoulder
	// 
	Output->BTN_GamePadButton5 = Input->Buttons.Individual.L1;
	Output->BTN_GamePadButton6 = Input->Buttons.Individual.R1;

	//
	// Select & Start
	// 
	Output->BTN_GamePadButton7 = Input->Buttons.Individual.Select;
	Output->BTN_GamePadButton8 = Input->Buttons.Individual.Start;

	//
	// Thumbs
	// 
	Output->BTN_GamePadButton9 = Input->Buttons.Individual.L3;
	Output->BTN_GamePadButton10 = Input->Buttons.Individual.R3;

	// 
	// D-Pad (POV/HAT format)
	// 
	switch (Input->Buttons.bButtons[0] & ~0xF)
	{
	case 0x10: // N
		Output->GD_GamePadHatSwitch = 1;
		break;
	case 0x30: // NE
		Output->GD_GamePadHatSwitch = 2;
		break;
	case 0x20: // E
		Output->GD_GamePadHatSwitch = 3;
		break;
	case 0x60: // SE
		Output->GD_GamePadHatSwitch = 4;
		break;
	case 0x40: // S
		Output->GD_GamePadHatSwitch = 5;
		break;
	case 0xC0: // SW
		Output->GD_GamePadHatSwitch = 6;
		break;
	case 0x80: // W
		Output->GD_GamePadHatSwitch = 7;
		break;
	case 0x90: // NW
		Output->GD_GamePadHatSwitch = 8;
		break;
	default: // Released
		Output->GD_GamePadHatSwitch = 0;
		break;
	}

	Output->GD_GamePadSystemControlSystemMainMenu = Input->Buttons.Individual.PS;
}
