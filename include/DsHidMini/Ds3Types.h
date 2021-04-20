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

#include <poppack.h>
