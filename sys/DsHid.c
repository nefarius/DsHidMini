#include "Driver.h"
#include "DsHid.tmh"
#ifdef DSHM_FEATURE_FFB
#include "PID/PIDTypes.h"
#endif

#pragma region DS3 HID Report Descriptor (Split Device Mode)

CONST HID_REPORT_DESCRIPTOR G_Ds3HidReportDescriptor_Split_Mode[] = 
{
	/************************************************************************/
	/* Gamepad definition (for regular DS3 buttons, axes & features)        */
	/************************************************************************/
	0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
	0x09, 0x05,        // Usage (Game Pad)
	0xA1, 0x01,        // Collection (Application)
	0x85, 0x01,        //   Report ID (1)
	0x09, 0x30,        //   Usage (X)
	0x09, 0x31,        //   Usage (Y)
	0x09, 0x32,        //   Usage (Z)
	0x09, 0x35,        //   Usage (Rz)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x04,        //   Report Count (4)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x39,        //   Usage (Hat switch)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x07,        //   Logical Maximum (7)
	0x35, 0x00,        //   Physical Minimum (0)
	0x46, 0x3B, 0x01,  //   Physical Maximum (315)
	0x65, 0x14,        //   Unit (System: English Rotation, Length: Centimeter)
	0x75, 0x04,        //   Report Size (4)
	0x95, 0x01,        //   Report Count (1)
	0x81, 0x42,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,Null State)
	0x65, 0x00,        //   Unit (None)
	0x05, 0x09,        //   Usage Page (Button)
	0x19, 0x01,        //   Usage Minimum (0x01)
	0x29, 0x0D,        //   Usage Maximum (0x0D)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x01,        //   Logical Maximum (1)
	0x75, 0x01,        //   Report Size (1)
	0x95, 0x0D,        //   Report Count (13)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x06, 0x00, 0xFF,  //   Usage Page (Vendor Defined 0xFF00)
	0x09, 0x20,        //   Usage (0x20)
	0x75, 0x07,        //   Report Size (7)
	0x95, 0x01,        //   Report Count (1)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x7F,        //   Logical Maximum (127)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x05, 0x01,        //   Usage Page (Generic Desktop Ctrls)
	0x09, 0x33,        //   Usage (Rx)
	0x09, 0x34,        //   Usage (Ry)
	0x09, 0x36,        //   Usage (Slider)	
	0x09, 0x37,        //   Usage (Dial)	
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x04,        //   Report Count (4)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xA1, 0x01,        //   Collection (Application)
	0x85, 0x01,        //     Report ID (1)
	0x06, 0x01, 0xFF,  //     Usage Page (Vendor Defined 0xFF01)
	0x09, 0x01,        //     Usage (0x01)
	0x75, 0x08,        //     Report Size (8)
	0x95, 0x1B,        //     Report Count (27)
	0x15, 0x00,        //     Logical Minimum (0)
	0x26, 0xFF, 0x00,  //     Logical Maximum (255)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //   End Collection
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
	0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
	0x15, 0x00,        // Logical Minimum (0)
	0x09, 0x04,        // Usage (Joystick)
	0xA1, 0x01,        // Collection (Application)
	0x05, 0x01,        //   Usage Page (Generic Desktop Ctrls)
	0x85, 0x02,        //   Report ID (2)
	0x09, 0x01,        //   Usage (Pointer)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x01,        //   Report Count (1)
	0xA1, 0x00,        //   Collection (Physical)
	0x09, 0x30,        //     Usage (X)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x31,        //     Usage (Y)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x32,        //     Usage (Z)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x33,        //     Usage (Rx)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x34,        //     Usage (Ry)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x35,        //     Usage (Rz)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x36,        //     Usage (Slider)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x37,        //     Usage (Dial)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //   End Collection
	0xC0,              // End Collection
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
	0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
	0x09, 0x05,        // Usage (Game Pad)
	0xA1, 0x01,        // Collection (Application)
	0x85, 0x01,        //   Report ID (1)
	0x09, 0x30,        //   Usage (X)
	0x09, 0x31,        //   Usage (Y)
	0x09, 0x32,        //   Usage (Z)
	0x09, 0x35,        //   Usage (Rz)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x04,        //   Report Count (4)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x39,        //   Usage (Hat switch)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x07,        //   Logical Maximum (7)
	0x35, 0x00,        //   Physical Minimum (0)
	0x46, 0x3B, 0x01,  //   Physical Maximum (315)
	0x65, 0x14,        //   Unit (System: English Rotation, Length: Centimeter)
	0x75, 0x04,        //   Report Size (4)
	0x95, 0x01,        //   Report Count (1)
	0x81, 0x42,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,Null State)
	0x65, 0x00,        //   Unit (None)
	0x05, 0x09,        //   Usage Page (Button)
	0x19, 0x01,        //   Usage Minimum (0x01)
	0x29, 0x0D,        //   Usage Maximum (0x0D)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x01,        //   Logical Maximum (1)
	0x75, 0x01,        //   Report Size (1)
	0x95, 0x0D,        //   Report Count (13)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x06, 0x00, 0xFF,  //   Usage Page (Vendor Defined 0xFF00)
	0x09, 0x20,        //   Usage (0x20)
	0x75, 0x07,        //   Report Size (7)
	0x95, 0x01,        //   Report Count (1)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x7F,        //   Logical Maximum (127)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x05, 0x01,        //   Usage Page (Generic Desktop Ctrls)
	0x09, 0x33,        //   Usage (Rx)
	0x09, 0x34,        //   Usage (Ry)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x02,        //   Report Count (2)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x01,        //   Report Count (1)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0xA1, 0x00,        //   Collection (Physical)
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xA1, 0x02,        //     Collection (Logical)
	0x09, 0x36,        //       Usage (Slider)
	0x81, 0x02,        //       Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //     End Collection
	0xC0,              //   End Collection
	0xA1, 0x01,        //   Collection (Application)
	0x85, 0x01,        //     Report ID (1)
	0x06, 0x01, 0xFF,  //     Usage Page (Vendor Defined 0xFF01)
	0x09, 0x01,        //     Usage (0x01)
	0x75, 0x08,        //     Report Size (8)
	0x95, 0x13,        //     Report Count (19)
	0x15, 0x00,        //     Logical Minimum (0)
	0x26, 0xFF, 0x00,  //     Logical Maximum (255)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0xC0,              //   End Collection
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
	0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
	0x09, 0x04,        // Usage (Joystick)
	0xA1, 0x01,        // Collection (Application)
	0x09, 0x01,        //   Usage (Pointer)
	0xA1, 0x00,        //   Collection (Physical)
	0x05, 0x09,        //     Usage Page (Button)
	0x19, 0x01,        //     Usage Minimum (0x01)
	0x29, 0x18,        //     Usage Maximum (0x18)
	0x15, 0x00,        //     Logical Minimum (0)
	0x25, 0x01,        //     Logical Maximum (1)
	0x35, 0x00,        //     Physical Minimum (0)
	0x45, 0x01,        //     Physical Maximum (1)
	0x75, 0x01,        //     Report Size (1)
	0x95, 0x18,        //     Report Count (24)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x05, 0x01,        //     Usage Page (Generic Desktop Ctrls)
	0x09, 0x39,        //     Usage (Hat switch)
	0x15, 0x00,        //     Logical Minimum (0)
	0x25, 0x07,        //     Logical Maximum (7)
	0x35, 0x00,        //     Physical Minimum (0)
	0x46, 0x3B, 0x01,  //     Physical Maximum (315)
	0x65, 0x14,        //     Unit (System: English Rotation, Length: Centimeter)
	0x75, 0x04,        //     Report Size (4)
	0x95, 0x01,        //     Report Count (1)
	0x81, 0x42,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,Null State)
	0x75, 0x01,        //     Report Size (1)
	0x95, 0x04,        //     Report Count (4)
	0x81, 0x03,        //     Input (Const,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x05, 0x01,        //     Usage Page (Generic Desktop Ctrls)
	0x09, 0x30,        //     Usage (X)
	0x09, 0x31,        //     Usage (Y)
	0x09, 0x32,        //     Usage (Z)
	0x09, 0x35,        //     Usage (Rz)
	0x09, 0x36,        //     Usage (Slider)
	0x09, 0x36,        //     Usage (Slider)
	0x09, 0x33,        //     Usage (Rx)
	0x09, 0x34,        //     Usage (Ry)
	0x15, 0x00,        //     Logical Minimum (0)
	0x26, 0xFF, 0x00,  //     Logical Maximum (255)
	0x35, 0x00,        //     Physical Minimum (0)
	0x46, 0xFF, 0x00,  //     Physical Maximum (255)
	0x75, 0x08,        //     Report Size (8)
	0x95, 0x08,        //     Report Count (8)
	0x81, 0x02,        //     Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x06, 0x00, 0xFF,  //     Usage Page (Vendor Defined 0xFF00)
	0x09, 0x01,        //     Usage (0x01)
	0x75, 0x08,        //     Report Size (8)
	0x95, 0x30,        //     Report Count (48)
	0x91, 0x02,        //     Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x06, 0x00, 0xFF,  //     Usage Page (Vendor Defined 0xFF00)
	0x09, 0x01,        //     Usage (0x01)
	0x15, 0x00,        //     Logical Minimum (0)
	0x26, 0xFF, 0x00,  //     Logical Maximum (255)
	0x75, 0x08,        //     Report Size (8)
	0x95, 0x31,        //     Report Count (49)
	0xB1, 0x00,        //     Feature (Data,Array,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0xC0,              //   End Collection
	0xC0,              // End Collection

	// 120 bytes
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

#pragma region DS3 HID Report Descriptor (DualShock 4 Rev1 emulation)

CONST HID_REPORT_DESCRIPTOR G_DualShock4Rev1HidReportDescriptor[] =
{
	/************************************************************************/
	/* DualShock 4 Rev1 compatible report descriptor                        */
	/************************************************************************/
	0x05, 0x01,        // Usage Page (Generic Desktop Ctrls)
	0x09, 0x05,        // Usage (Game Pad)
	0xA1, 0x01,        // Collection (Application)
	0x85, 0x01,        //   Report ID (1)
	0x09, 0x30,        //   Usage (X)
	0x09, 0x31,        //   Usage (Y)
	0x09, 0x32,        //   Usage (Z)
	0x09, 0x35,        //   Usage (Rz)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x04,        //   Report Count (4)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x09, 0x39,        //   Usage (Hat switch)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x07,        //   Logical Maximum (7)
	0x35, 0x00,        //   Physical Minimum (0)
	0x46, 0x3B, 0x01,  //   Physical Maximum (315)
	0x65, 0x14,        //   Unit (System: English Rotation, Length: Centimeter)
	0x75, 0x04,        //   Report Size (4)
	0x95, 0x01,        //   Report Count (1)
	0x81, 0x42,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,Null State)
	0x65, 0x00,        //   Unit (None)
	0x05, 0x09,        //   Usage Page (Button)
	0x19, 0x01,        //   Usage Minimum (0x01)
	0x29, 0x0E,        //   Usage Maximum (0x0E)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x01,        //   Logical Maximum (1)
	0x75, 0x01,        //   Report Size (1)
	0x95, 0x0E,        //   Report Count (14)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x06, 0x00, 0xFF,  //   Usage Page (Vendor Defined 0xFF00)
	0x09, 0x20,        //   Usage (0x20)
	0x75, 0x06,        //   Report Size (6)
	0x95, 0x01,        //   Report Count (1)
	0x15, 0x00,        //   Logical Minimum (0)
	0x25, 0x7F,        //   Logical Maximum (127)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x05, 0x01,        //   Usage Page (Generic Desktop Ctrls)
	0x09, 0x33,        //   Usage (Rx)
	0x09, 0x34,        //   Usage (Ry)
	0x15, 0x00,        //   Logical Minimum (0)
	0x26, 0xFF, 0x00,  //   Logical Maximum (255)
	0x75, 0x08,        //   Report Size (8)
	0x95, 0x02,        //   Report Count (2)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x06, 0x00, 0xFF,  //   Usage Page (Vendor Defined 0xFF00)
	0x09, 0x21,        //   Usage (0x21)
	0x95, 0x36,        //   Report Count (54)
	0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
	0x85, 0x05,        //   Report ID (5)
	0x09, 0x22,        //   Usage (0x22)
	0x95, 0x1F,        //   Report Count (31)
	0x91, 0x02,        //   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x04,        //   Report ID (4)
	0x09, 0x23,        //   Usage (0x23)
	0x95, 0x24,        //   Report Count (36)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x02,        //   Report ID (2)
	0x09, 0x24,        //   Usage (0x24)
	0x95, 0x24,        //   Report Count (36)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x08,        //   Report ID (8)
	0x09, 0x25,        //   Usage (0x25)
	0x95, 0x03,        //   Report Count (3)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x10,        //   Report ID (16)
	0x09, 0x26,        //   Usage (0x26)
	0x95, 0x04,        //   Report Count (4)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x11,        //   Report ID (17)
	0x09, 0x27,        //   Usage (0x27)
	0x95, 0x02,        //   Report Count (2)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x12,        //   Report ID (18)
	0x06, 0x02, 0xFF,  //   Usage Page (Vendor Defined 0xFF02)
	0x09, 0x21,        //   Usage (0x21)
	0x95, 0x0F,        //   Report Count (15)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x13,        //   Report ID (19)
	0x09, 0x22,        //   Usage (0x22)
	0x95, 0x16,        //   Report Count (22)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x14,        //   Report ID (20)
	0x06, 0x05, 0xFF,  //   Usage Page (Vendor Defined 0xFF05)
	0x09, 0x20,        //   Usage (0x20)
	0x95, 0x10,        //   Report Count (16)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x15,        //   Report ID (21)
	0x09, 0x21,        //   Usage (0x21)
	0x95, 0x2C,        //   Report Count (44)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x06, 0x80, 0xFF,  //   Usage Page (Vendor Defined 0xFF80)
	0x85, 0x80,        //   Report ID (-128)
	0x09, 0x20,        //   Usage (0x20)
	0x95, 0x06,        //   Report Count (6)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x81,        //   Report ID (-127)
	0x09, 0x21,        //   Usage (0x21)
	0x95, 0x06,        //   Report Count (6)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x82,        //   Report ID (-126)
	0x09, 0x22,        //   Usage (0x22)
	0x95, 0x05,        //   Report Count (5)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x83,        //   Report ID (-125)
	0x09, 0x23,        //   Usage (0x23)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x84,        //   Report ID (-124)
	0x09, 0x24,        //   Usage (0x24)
	0x95, 0x04,        //   Report Count (4)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x85,        //   Report ID (-123)
	0x09, 0x25,        //   Usage (0x25)
	0x95, 0x06,        //   Report Count (6)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x86,        //   Report ID (-122)
	0x09, 0x26,        //   Usage (0x26)
	0x95, 0x06,        //   Report Count (6)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x87,        //   Report ID (-121)
	0x09, 0x27,        //   Usage (0x27)
	0x95, 0x23,        //   Report Count (35)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x88,        //   Report ID (-120)
	0x09, 0x28,        //   Usage (0x28)
	0x95, 0x22,        //   Report Count (34)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x89,        //   Report ID (-119)
	0x09, 0x29,        //   Usage (0x29)
	0x95, 0x02,        //   Report Count (2)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x90,        //   Report ID (-112)
	0x09, 0x30,        //   Usage (0x30)
	0x95, 0x05,        //   Report Count (5)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x91,        //   Report ID (-111)
	0x09, 0x31,        //   Usage (0x31)
	0x95, 0x03,        //   Report Count (3)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x92,        //   Report ID (-110)
	0x09, 0x32,        //   Usage (0x32)
	0x95, 0x03,        //   Report Count (3)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0x93,        //   Report ID (-109)
	0x09, 0x33,        //   Usage (0x33)
	0x95, 0x0C,        //   Report Count (12)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA0,        //   Report ID (-96)
	0x09, 0x40,        //   Usage (0x40)
	0x95, 0x06,        //   Report Count (6)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA1,        //   Report ID (-95)
	0x09, 0x41,        //   Usage (0x41)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA2,        //   Report ID (-94)
	0x09, 0x42,        //   Usage (0x42)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA3,        //   Report ID (-93)
	0x09, 0x43,        //   Usage (0x43)
	0x95, 0x30,        //   Report Count (48)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA4,        //   Report ID (-92)
	0x09, 0x44,        //   Usage (0x44)
	0x95, 0x0D,        //   Report Count (13)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA5,        //   Report ID (-91)
	0x09, 0x45,        //   Usage (0x45)
	0x95, 0x15,        //   Report Count (21)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA6,        //   Report ID (-90)
	0x09, 0x46,        //   Usage (0x46)
	0x95, 0x15,        //   Report Count (21)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xF0,        //   Report ID (-16)
	0x09, 0x47,        //   Usage (0x47)
	0x95, 0x3F,        //   Report Count (63)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xF1,        //   Report ID (-15)
	0x09, 0x48,        //   Usage (0x48)
	0x95, 0x3F,        //   Report Count (63)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xF2,        //   Report ID (-14)
	0x09, 0x49,        //   Usage (0x49)
	0x95, 0x0F,        //   Report Count (15)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA7,        //   Report ID (-89)
	0x09, 0x4A,        //   Usage (0x4A)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA8,        //   Report ID (-88)
	0x09, 0x4B,        //   Usage (0x4B)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xA9,        //   Report ID (-87)
	0x09, 0x4C,        //   Usage (0x4C)
	0x95, 0x08,        //   Report Count (8)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAA,        //   Report ID (-86)
	0x09, 0x4E,        //   Usage (0x4E)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAB,        //   Report ID (-85)
	0x09, 0x4F,        //   Usage (0x4F)
	0x95, 0x39,        //   Report Count (57)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAC,        //   Report ID (-84)
	0x09, 0x50,        //   Usage (0x50)
	0x95, 0x39,        //   Report Count (57)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAD,        //   Report ID (-83)
	0x09, 0x51,        //   Usage (0x51)
	0x95, 0x0B,        //   Report Count (11)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAE,        //   Report ID (-82)
	0x09, 0x52,        //   Usage (0x52)
	0x95, 0x01,        //   Report Count (1)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xAF,        //   Report ID (-81)
	0x09, 0x53,        //   Usage (0x53)
	0x95, 0x02,        //   Report Count (2)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xB0,        //   Report ID (-80)
	0x09, 0x54,        //   Usage (0x54)
	0x95, 0x3F,        //   Report Count (63)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xB1,        //   Report ID (-79)
	0x09, 0x55,        //   Usage (0x55)
	0x95, 0x02,        //   Report Count (2)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0x85, 0xB2,        //   Report ID (-78)
	0x09, 0x56,        //   Usage (0x56)
	0x95, 0x02,        //   Report Count (2)
	0xB1, 0x02,        //   Feature (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position,Non-volatile)
	0xC0,              // End Collection

	// 483 bytes
};

CONST HID_DESCRIPTOR G_DualShock4Rev1HidDescriptor = {
	0x09,   // length of HID descriptor
	0x21,   // descriptor type == HID  0x21
	0x0100, // hid spec release
	0x00,   // country code == Not Specified
	0x01,   // number of HID class descriptors
{ 0x22,   // descriptor type 
sizeof(G_DualShock4Rev1HidReportDescriptor) }  // total length of report descriptor
};

#pragma endregion


BOOLEAN DS3_RAW_IS_IDLE(
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
	// If we end up here, no movement is going on
	// 

	return TRUE;
}

VOID DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_01(
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
		Output[5] |= Input[3] & 0xF0; // OUTPUT: SQUARE [7], CROSS [6], CIRCLE [5], TRIANGLE [4]

		// Remaining buttons
		Output[6] |= (Input[2] & 0xF); // OUTPUT: START [3], RSB [2], LSB [1], SELECT [0]
		Output[6] |= (Input[3] & 0xF) << 4; // OUTPUT: R1 [7], L1 [6], R2 [5], L2 [4]
	}
	else {
		// Clear HAT position
		Output[5] |= 8 & 0xF;
	}

	// PS button
	Output[7] = Input[4]; // OUTPUT: PADDING [7-1], PS BUTTON [0]

	// Thumb axes
	Output[1] = Input[6]; // LTX
	Output[2] = Input[7]; // LTY
	Output[3] = Input[8]; // RTX
	Output[4] = Input[9]; // RTY

	// Trigger axes
	Output[8] = Input[18]; // L2
	Output[9] = Input[19]; // R2

	// Shoulders (pressure)
	Output[10] = Input[20]; // L1
	Output[11] = Input[21]; // R1

}

VOID DS3_RAW_TO_SPLIT_HID_INPUT_REPORT_02(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output
)
{
	// Report ID
	Output[0] = 0x02;

	// D-Pad (pressure)
	Output[1] = Input[14]; // UP
	Output[2] = Input[15]; // RIGHT
	Output[3] = Input[16]; // DOWN
	Output[4] = Input[17]; // LEFT

	// Face buttons (pressure)
	Output[5] = Input[22]; // TRIANGLE
	Output[6] = Input[23]; // CIRCLE
	Output[7] = Input[24]; // CROSS
	Output[8] = Input[25]; // SQUARE
}

VOID DS3_RAW_TO_SINGLE_HID_INPUT_REPORT(
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
		Output[5] |= Input[3] & 0xF0; // OUTPUT: SQUARE[7], CROSS[6], CIRCLE[5], TRIANGLE[4]

		// Remaining buttons
		Output[6] |= (Input[2] & 0xF);  // OUTPUT: START [3], RSB [2], LSB [1], SELECT [0]
		Output[6] |= (Input[3] & 0xF) << 4; // OUTPUT: R1 [7], L1 [6], R2 [5], L2 [4]
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
	Output[8] = Input[18]; // L2
	Output[9] = Input[19]; // R2

	// PS button
	Output[7] = Input[4]; // OUTPUT: PADDING [7-1], PS BUTTON [0]

	// D-Pad (pressure)
	Output[10] = Input[14]; // UP
	Output[11] = Input[15]; // RIGHT
	Output[12] = Input[16]; // DOWN
	Output[13] = Input[17]; // LEFT

	// Shoulders (pressure)
	Output[14] = Input[20]; // L1
	Output[15] = Input[21]; // R1

	// Face buttons (pressure)
	Output[16] = Input[22]; // TRIANGLE
	Output[17] = Input[23]; // CIRCLE
	Output[18] = Input[24]; // CROSS
	Output[19] = Input[25]; // SQUARE
}

VOID DS3_RAW_TO_SIXAXIS_HID_INPUT_REPORT(
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

UCHAR REVERSE_BITS(UCHAR x)
{
	x = ((x >> 1) & 0x55) | ((x << 1) & 0xaa);
	x = ((x >> 2) & 0x33) | ((x << 2) & 0xcc);
	x = ((x >> 4) & 0x0f) | ((x << 4) & 0xf0);
	return x;
}

VOID DS3_RAW_TO_DS4REV1_HID_INPUT_REPORT(
	_In_ PUCHAR Input,
	_Out_ PUCHAR Output,
	_In_ BOOLEAN IsWired
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

	// Battery + cable info
	Output[30] &= ~0xF; // Clear lower 4 bits

	// Finger 1 touchpad contact info
	Output[35] |= 0x80; // Set top bit to disable finger contact
	Output[44] |= 0x80; // Set top bit to disable finger contact

	// Finger 2 touchpad contact info
	Output[39] |= 0x80; // Set top bit to disable finger contact
	Output[48] |= 0x80; // Set top bit to disable finger contact

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
	
	// Face buttons
	Output[5] |= ((REVERSE_BITS(Input[3]) << 4) & 0xF0);
		
	// Select to Share
	Output[6] |= ((Input[2] & 0x01) << 4);

	// Start to Options
	Output[6] |= (((Input[2] >> 3) & 0x01) << 5);

	// L1, L2, R1, R2
	Output[6] |= (((Input[3] >> 2) & 0x01) << 0);
	Output[6] |= (((Input[3] >> 0) & 0x01) << 2);
	Output[6] |= (((Input[3] >> 3) & 0x01) << 1);
	Output[6] |= (((Input[3] >> 1) & 0x01) << 3);

	// L3, R3
	Output[6] |= (((Input[2] >> 1) & 0x01) << 6);
	Output[6] |= (((Input[2] >> 2) & 0x01) << 7);
	
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

	// Battery translation, from 8 to 0: 100%, 87%, 75%, 62%, 50%, 37%, 25%, 12%, 0%
	if (IsWired)
	{
		// Wired sets a flag
		Output[30] |= 0x10;

		switch ((DS_BATTERY_STATUS)Input[30])
		{
		case DsBatteryStatusCharging:
			Output[30] |= 7; // 87%
			break;
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			Output[30] |= 8; // 100%
			break;
		}
	}
	else
	{
		// Clear flag
		Output[30] &= ~0x10;

		switch ((DS_BATTERY_STATUS)Input[30])
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
