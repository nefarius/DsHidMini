#pragma once

#define DS3_POOL_TAG	'p3sD'
#define SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH  8

extern const UCHAR G_Ds3UsbHidOutputReport[];

#define DS3_USB_HID_OUTPUT_REPORT_SIZE		0x31

extern const UCHAR G_Ds3BthHidOutputReport[];

#define DS3_BTH_HID_OUTPUT_REPORT_SIZE		0x32

#define DS3_LED_1       0x02
#define DS3_LED_2       0x04
#define DS3_LED_3       0x08
#define DS3_LED_4       0x10
#define DS3_LED_OFF     0x20

#define DS3_USB_SET_LED(_buf_, _led_)   ((_buf_)[10] = (_led_))
#define DS3_USB_GET_LED(_buf_)          ((_buf_)[10])

#define DS3_USB_SET_SMALL_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[2] = (_dur_))
#define DS3_USB_SET_LARGE_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[4] = (_dur_))
#define DS3_USB_SET_SMALL_RUMBLE_STRENGTH(_buf_, _str_)  ((_buf_)[3] = (_str_) > 0 ? 0x01 : 0x00)
#define DS3_USB_SET_LARGE_RUMBLE_STRENGTH(_buf_, _str_)  ((_buf_)[5] = (_str_))

#define DS3_BTH_SET_LED(_buf_, _led_)   ((_buf_)[11] = (_led_))
#define DS3_BTH_GET_LED(_buf_)          ((_buf_)[11])

#define DS3_BTH_SET_SMALL_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[3] = (_dur_))
#define DS3_BTH_SET_LARGE_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[5] = (_dur_))
#define DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(_buf_, _str_)  ((_buf_)[4] = (_str_) > 0 ? 0x01 : 0x00)
#define DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(_buf_, _str_)  ((_buf_)[6] = (_str_))


VOID DS3_SET_LED_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR LedIndex,
	UCHAR TotalDuration,
	UCHAR Interval,
	UCHAR OffInterval,
	UCHAR OnInterval
);

VOID DS3_SET_LED_DURATION_DEFAULT(
	PDEVICE_CONTEXT Context,
	UCHAR LedIndex
);

VOID DS3_GET_UNIFIED_OUTPUT_REPORT_BUFFER(
	PDEVICE_CONTEXT Context,
	UCHAR** Buffer,
	PSIZE_T BufferLength
);

VOID DS3_GET_RAW_OUTPUT_REPORT_BUFFER(
	PDEVICE_CONTEXT Context,
	UCHAR** Buffer,
	PSIZE_T BufferLength
);

VOID DS3_SET_LED_FLAGS(
	PDEVICE_CONTEXT Context,
	UCHAR Value
);

UCHAR DS3_GET_LED(
	PDEVICE_CONTEXT Context
);

VOID DS3_SET_SMALL_RUMBLE_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR Value
);

VOID DS3_SET_SMALL_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR Value
);

VOID DS3_SET_LARGE_RUMBLE_DURATION(
	PDEVICE_CONTEXT Context,
	UCHAR Value
);

VOID DS3_SET_LARGE_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR Value
);

VOID DS3_SET_BOTH_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context,
	UCHAR LargeValue,
	UCHAR SmallValue
);

VOID DS3_PROCESS_RUMBLE_STRENGTH(
	PDEVICE_CONTEXT Context
);

typedef enum
{
    // Class-Specific Requests
    GetReport = 0x01,
    GetIdle = 0x02,
    GetProtocol = 0x03,
    SetReport = 0x09,
    SetIdle = 0x0A,
    SetProtocol = 0x0B,
    // Standard Requests
    GetDescriptor = 0x06,
    SetDescriptor = 0x07

} USB_HID_REQUEST;

typedef enum
{
    HidReportRequestTypeInput = 0x01,
    HidReportRequestTypeOutput = 0x02,
    HidReportRequestTypeFeature = 0x03

} USB_HID_REPORT_REQUEST_TYPE;

typedef enum
{
    HidReportRequestIdOne = 0x01

} USB_HID_REPORT_REQUEST_ID;

typedef enum
{
    Hid = 0x21,
    Report = 0x22,
    PhysicalDescriptor = 0x23

} USB_HID_CLASS_DESCRIPTOR_TYPE;

typedef enum
{
    Ds3FeatureDeviceAddress = 0x03F2,
    Ds3FeatureStartDevice = 0x03F4,
    Ds3FeatureHostAddress = 0x03F5

} DS3_FEATURE_VALUE;

#define USB_SETUP_VALUE(_type_, _id_) (USHORT)(((_type_) << 8) | (_id_))

NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context);

NTSTATUS DsUsb_Ds3PairToFirstRadio(WDFDEVICE Device);

NTSTATUS DsBth_Ds3Init(PDEVICE_CONTEXT Context);
