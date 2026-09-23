#pragma once

//
// Buttons, hat, sticks and the 12 pressure bytes. The trailing vendor
// words are not required.
// 
#define THIRD_PARTY_HID_INPUT_REPORT_MINIMUM    19

//
// SET_REPORT Output, report id 0. hid-shanwan uses this on the control
// endpoint; an interrupt OUT write is not the path that firmware accepts.
// 
#define THIRD_PARTY_HID_OUTPUT_REPORT_VALUE     0x0200
#define THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH    8

VOID
ThirdPartyHid_TranslateInput(
	_In_reads_(Length) const UCHAR* Raw,
	_In_ size_t Length,
	_Out_ PDS3_RAW_INPUT_REPORT Report
);

VOID
ThirdPartyHid_BuildOutputReport(
	_In_ const PDEVICE_CONTEXT Context,
	_In_reads_(Ds3ReportLength) const UCHAR* Ds3Report,
	_In_ size_t Ds3ReportLength,
	_Out_writes_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
);

NTSTATUS
ThirdPartyHid_SendOutputReport(
	_In_ PDEVICE_CONTEXT Context,
	_In_reads_(THIRD_PARTY_HID_OUTPUT_REPORT_LENGTH) PUCHAR Output
);
