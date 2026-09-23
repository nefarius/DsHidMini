#pragma once

//
// Buttons, hat, sticks and the 12 pressure bytes. The trailing vendor
// words are not required.
// 
#define THIRD_PARTY_HID_INPUT_REPORT_MINIMUM    19

//
// Output report id 0. Default transport is the interrupt OUT pipe, which
// is where Linux usbhid delivers hid-shanwan's SET_REPORT. The control
// endpoint (wValue 0x0200) remains available when UsbOutputReportTransport
// is ControlEndpoint.
// 
#define THIRD_PARTY_HID_OUTPUT_REPORT_VALUE     0x0200
#define THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH    8

//
// DS3 small-motor writes are on/off. A pass-through report that turns the
// motor on has no 0-255 magnitude, so the adapter is given full strength.
// 
#define THIRD_PARTY_HID_SMALL_MOTOR_ON_STRENGTH 0xFF

VOID
ThirdPartyHid_TranslateInput(
	_In_reads_(Length) const UCHAR* Raw,
	_In_ size_t Length,
	_Out_ PDS3_RAW_INPUT_REPORT Report
);

UCHAR
ThirdPartyHid_CaptureRightMotorStrength(
	_In_ DS_OUTPUT_REPORT_SOURCE Source,
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR LightCache
);

VOID
ThirdPartyHid_BuildOutputReport(
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_In_ UCHAR RightMotorStrength,
	_Out_writes_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
);

NTSTATUS
ThirdPartyHid_SendOutputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_reads_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
);
