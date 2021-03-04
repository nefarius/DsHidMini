#pragma once

typedef UCHAR HID_REPORT_DESCRIPTOR, * PHID_REPORT_DESCRIPTOR;

extern CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Split_Mode[];

extern CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Split_Mode;

extern CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Single_Mode[];

extern CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Single_Mode;

extern CONST HID_REPORT_DESCRIPTOR G_SixaxisHidReportDescriptor[];

extern CONST HID_DESCRIPTOR G_SixaxisHidDescriptor;

extern CONST HID_REPORT_DESCRIPTOR G_DualShock4Rev1HidReportDescriptor[];

extern CONST HID_DESCRIPTOR G_DualShock4Rev1HidDescriptor;

#define DS3_COMMON_MAX_HID_INPUT_REPORT_SIZE	0x40
#define DS3_DS4REV1_HID_INPUT_REPORT_SIZE		DS3_COMMON_MAX_HID_INPUT_REPORT_SIZE
#define DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE	0x27
#define SIXAXIS_HID_INPUT_REPORT_SIZE			0x0C
#define SIXAXIS_HID_GET_FEATURE_REPORT_SIZE		0x31

#define DS3_RAW_SLIDER_IDLE_THRESHOLD			0x05 // 5
#define DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER		0x73 // 115
#define DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER		0x87 // 135

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
BOOLEAN FORCEINLINE DS3_RAW_IS_IDLE(
	_In_ PUCHAR Input
)
{
	//
	// Button states
	// 

	if (Input[2] || Input[3] || Input[4])
	{
		return FALSE;
	}

	//
	// Axes
	// 
	
	for (UCHAR i = 6; i < 9; i++)
	{
		if (Input[i] < DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER
			|| Input[i] > DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER)
		{
			return FALSE;
		}
	}

	//
	// Sliders
	// 

	for (UCHAR i = 18; i < 19; i++)
	{
		if (Input[i] > DS3_RAW_SLIDER_IDLE_THRESHOLD)
		{
			return FALSE;
		}
	}

	//
	// If we end up here, some movement is going on
	// 
	
	return TRUE;
}

VOID FORCEINLINE DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN MuteDigitalPressureButtons
)
{
	// Report ID
	Output[0] = 0x01;

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Remaining buttons
	Output[6] &= ~0xFF; // Clear all 8 bits

	if (!MuteDigitalPressureButtons)
	{
		// Translate D-Pad to HAT format
		switch (Input[2] & ~0xF)
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

		// Set face buttons
		Output[5] |= Input[3] & 0xF0;

		// Remaining buttons
		Output[6] |= (Input[2] & 0xF);
		Output[6] |= (Input[3] & 0xF) << 4;
	}
	else {
		// Clear HAT position
		Output[5] |= 8 & 0xF;
	}

	// Thumb axes
	Output[1] = Input[6]; // LTX
	Output[2] = Input[7]; // LTY
	Output[3] = Input[8]; // RTX
	Output[4] = Input[9]; // RTY

	// Trigger axes
	Output[8] = Input[18];
	Output[9] = Input[19];

	// PS button
	Output[7] = Input[4];

	// D-Pad (pressure)
	Output[10] = Input[14];
	Output[11] = Input[15];
	Output[12] = Input[16];
	Output[13] = Input[17];

	// Shoulders (pressure)
	Output[14] = Input[20];
	Output[15] = Input[21];

	// Face buttons (pressure)
	Output[16] = Input[22];
	Output[17] = Input[23];
	Output[18] = Input[24];
	Output[19] = Input[25];
}

VOID FORCEINLINE DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
)
{
	// Report ID
	Output[0] = 0x02;

	// D-Pad (pressure)
	Output[1] = Input[14];
	Output[2] = Input[15];
	Output[3] = Input[16];
	Output[4] = Input[17];

	// Face buttons (pressure)
	Output[5] = Input[22];
	Output[6] = Input[23];
	Output[7] = Input[24];
	Output[8] = Input[25];

	// Shoulders (pressure)
	// NOTE: not accessible via DirectInput because of axis limit
	// TODO: introduce 3rd device?
	Output[9] = Input[20];
	Output[10] = Input[21];
}

VOID FORCEINLINE DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN MuteDigitalPressureButtons
)
{
	// Report ID
	Output[0] = Input[0];

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Remaining buttons
	Output[6] &= ~0xFF; // Clear all 8 bits

	if (!MuteDigitalPressureButtons)
	{
		// Translate D-Pad to HAT format
		switch (Input[2] & ~0xF)
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

		// Set face buttons
		Output[5] |= Input[3] & 0xF0;

		// Remaining buttons
		Output[6] |= (Input[2] & 0xF);
		Output[6] |= (Input[3] & 0xF) << 4;
	}
	else {
		// Clear HAT position
		Output[5] |= 8 & 0xF;
	}

	// Thumb axes
	Output[1] = Input[6]; // LTX
	Output[2] = Input[7]; // LTY
	Output[3] = Input[8]; // RTX
	Output[4] = Input[9]; // RTY

	// Trigger axes
	Output[8] = Input[18];
	Output[9] = Input[19];

	// PS button
	Output[7] = Input[4];

	// D-Pad (pressure)
	Output[10] = Input[14];
	Output[11] = Input[15];
	Output[12] = Input[16];
	Output[13] = Input[17];

	// Shoulders (pressure)
	Output[14] = Input[20];
	Output[15] = Input[21];

	// Face buttons (pressure)
	Output[16] = Input[22];
	Output[17] = Input[23];
	Output[18] = Input[24];
	Output[19] = Input[25];
}

VOID FORCEINLINE DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
)
{
	// Prepare D-Pad
	Output[3] &= ~0xF; // Clear lower 4 bits
	
	// Translate D-Pad to HAT format
	switch (Input[2] & ~0xF)
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
	Output[4] = Input[6]; // LTX
	Output[5] = Input[7]; // LTY
	Output[6] = Input[8]; // RTX
	Output[7] = Input[9]; // RTY

	// Buttons
	Output[0] &= ~0xFF; // Clear all 8 bits
	Output[1] &= ~0xFF; // Clear all 8 bits

	// Face buttons
	Output[0] |= ((Input[3] & 0xF0) >> 4);
	// L2, R2, L1, R1
	Output[0] |= ((Input[3] & 0x0F) << 4);

	// Select
	Output[1] |= ((Input[2] & 0x01) << 1);
	// Start
	Output[1] |= ((Input[2] & 0x08) >> 3);
	// L3
	Output[1] |= ((Input[2] & 0x02) << 1);
	// R3
	Output[1] |= ((Input[2] & 0x04) << 1);
	// PS
	Output[1] |= ((Input[4] & 0x01) << 4);
	
	// Trigger axes
	Output[10] = (0xFF - Input[18]);
	Output[11] = (0xFF - Input[19]);
		
	// Face buttons (pressure)
	Output[8] = (0xFF - Input[23]);
	Output[9] = (0xFF - Input[24]);
}

VOID FORCEINLINE DS3_RAW_TO_DS4REV1_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
)
{
	// Report ID
	Output[0] = Input[0];

	// Prepare D-Pad
	Output[5] &= ~0xF; // Clear lower 4 bits

	// Prepare face buttons
	Output[5] &= ~0xF0; // Clear upper 4 bits

	// Remaining buttons
	Output[6] &= ~0xFF; // Clear all 8 bits

	// PS button
	Output[7] &= ~0x01; // Clear button bit
	
	// Translate D-Pad to HAT format
	switch (Input[2] & ~0xF)
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

	// Set face buttons
	Output[5] |= Input[3] & 0xF0;

	// Remaining buttons
	Output[6] |= (Input[2] & 0xF);
	Output[6] |= (Input[3] & 0xF) << 4;
	
	// Thumb axes
	Output[1] = Input[6]; // LTX
	Output[2] = Input[7]; // LTY
	Output[3] = Input[8]; // RTX
	Output[4] = Input[9]; // RTY

	// Trigger axes
	Output[8] = Input[18];
	Output[9] = Input[19];

	// PS button
	Output[7] |= Input[4] & 0x01;
}
