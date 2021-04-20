#pragma once

typedef UCHAR HID_REPORT_DESCRIPTOR, * PHID_REPORT_DESCRIPTOR;

extern CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Split_Mode[];

extern CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Split_Mode;

extern CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Single_Mode[];

extern CONST HID_DESCRIPTOR G_Ds3HidDescriptor_Single_Mode;

extern CONST HID_REPORT_DESCRIPTOR G_SixaxisHidReportDescriptor[];

extern CONST HID_DESCRIPTOR G_SixaxisHidDescriptor;

extern CONST HID_REPORT_DESCRIPTOR G_VendorDefinedUSBDS4HidReportDescriptor[];

extern CONST HID_DESCRIPTOR G_VendorDefinedUSBDS4HidDescriptor;

#define DS3_COMMON_MAX_HID_INPUT_REPORT_SIZE	0x40
#define DS3_DS4REV1_USB_HID_INPUT_REPORT_SIZE	DS3_COMMON_MAX_HID_INPUT_REPORT_SIZE
#define DS3_SPLIT_SINGLE_HID_INPUT_REPORT_SIZE	0x27
#define SIXAXIS_HID_INPUT_REPORT_SIZE			0x0C
#define SIXAXIS_HID_GET_FEATURE_REPORT_SIZE		0x31

#define DS3_RAW_SLIDER_IDLE_THRESHOLD			0x7F // 127 ( (256 * 0,5 ) -1 )
#define DS3_RAW_AXIS_IDLE_THRESHOLD_LOWER		0x3F // 63 ( ( 128 * 0,5 ) - 1 )
#define DS3_RAW_AXIS_IDLE_THRESHOLD_UPPER		0xC0 // 192 ( ( 128 * 1,5 ) - 1 )

// Artificial identifiers to ease detection
// 
#define DS3_DS4WINDOWS_HID_VID					0x7331
#define DS3_DS4WINDOWS_HID_PID					0x0001

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
BOOLEAN DS3_RAW_IS_IDLE(
	_In_ PDS3_RAW_INPUT_REPORT Input
);

VOID DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN MuteDigitalPressureButtons
);

VOID DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
);

VOID DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN MuteDigitalPressureButtons
);

VOID DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
);

UCHAR REVERSE_BITS(UCHAR x);

VOID DS3_RAW_TO_DS4REV1_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN IsWired
);
