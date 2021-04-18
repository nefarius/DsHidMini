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

	ULONG Buttons;

	UCHAR LeftThumbX;
	UCHAR LeftThumbY;

	UCHAR RightThumbX;
	UCHAR RightThumbY;

	UCHAR Reserved1[3];

	UCHAR PressureValues[12];

	UCHAR Reserved2[16];

	USHORT AccelerometerX;
	USHORT AccelerometerY;
	USHORT AccelerometerZ;
	USHORT Gyroscope;

} DS3_RAW_INPUT_REPORT, * PDS3_RAW_INPUT_REPORT;

#include <poppack.h>
