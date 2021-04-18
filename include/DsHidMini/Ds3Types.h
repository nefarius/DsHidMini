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
	
	union
	{
		ULONG lButtons;

		UCHAR bButtons[4];
	} Buttons;

	UCHAR LeftThumbX;
	UCHAR LeftThumbY;

	UCHAR RightThumbX;
	UCHAR RightThumbY;

	UCHAR Reserved1[3];
	
	union
	{
		UCHAR bValues[12];

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

	UCHAR BatteryStatus;

	UCHAR Reserved3[11];

	USHORT AccelerometerX;
	USHORT AccelerometerY;
	USHORT AccelerometerZ;
	USHORT Gyroscope;

} DS3_RAW_INPUT_REPORT, * PDS3_RAW_INPUT_REPORT;

#include <poppack.h>
