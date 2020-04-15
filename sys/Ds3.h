#pragma once

#define DS3_POOL_TAG	'p3sD'
#define SET_HOST_BD_ADDR_CONTROL_BUFFER_LENGTH  8

extern const UCHAR G_Ds3UsbHidOutputReport[];

#define DS3_USB_HID_OUTPUT_REPORT_SIZE		0x30

extern const UCHAR G_Ds3BthHidOutputReport[];

#define DS3_BTH_HID_OUTPUT_REPORT_SIZE		0x32

#define DS3_LED_1       0x02
#define DS3_LED_2       0x04
#define DS3_LED_3       0x08
#define DS3_LED_4       0x10
#define DS3_LED_OFF     0x20

#define DS3_USB_SET_LED(_buf_, _led_)   ((_buf_)[9] = (_led_))
#define DS3_USB_GET_LED(_buf_)          ((_buf_)[9])

#define DS3_USB_SET_SMALL_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[1] = (_dur_))
#define DS3_USB_SET_LARGE_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[3] = (_dur_))
#define DS3_USB_SET_SMALL_RUMBLE_STRENGTH(_buf_, _dur_)  ((_buf_)[2] = (_dur_))
#define DS3_USB_SET_LARGE_RUMBLE_STRENGTH(_buf_, _dur_)  ((_buf_)[4] = (_dur_))

#define DS3_BTH_SET_LED(_buf_, _led_)   ((_buf_)[11] = (_led_))
#define DS3_BTH_GET_LED(_buf_)          ((_buf_)[11])

#define DS3_BTH_SET_SMALL_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[3] = (_dur_))
#define DS3_BTH_SET_LARGE_RUMBLE_DURATION(_buf_, _dur_)  ((_buf_)[5] = (_dur_))
#define DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(_buf_, _dur_)  ((_buf_)[4] = (_dur_))
#define DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(_buf_, _dur_)  ((_buf_)[6] = (_dur_))

typedef enum _USB_HID_REQUEST
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

typedef enum _USB_HID_REPORT_REQUEST_TYPE
{
    HidReportRequestTypeInput = 0x01,
    HidReportRequestTypeOutput = 0x02,
    HidReportRequestTypeFeature = 0x03

} USB_HID_REPORT_REQUEST_TYPE;

typedef enum _USB_HID_REPORT_REQUEST_ID
{
    HidReportRequestIdOne = 0x01

} USB_HID_REPORT_REQUEST_ID;

typedef enum _USB_HID_CLASS_DESCRIPTOR_TYPE
{
    Hid = 0x21,
    Report = 0x22,
    PhysicalDescriptor = 0x23

} USB_HID_CLASS_DESCRIPTOR_TYPE;

typedef enum _DS3_FEATURE_VALUE
{
    Ds3FeatureDeviceAddress = 0x03F2,
    Ds3FeatureStartDevice = 0x03F4,
    Ds3FeatureHostAddress = 0x03F5

} DS3_FEATURE_VALUE;

#define USB_SETUP_VALUE(_type_, _id_) (USHORT)(((_type_) << 8) | (_id_))

NTSTATUS DsUsb_Ds3Init(PDEVICE_CONTEXT Context);

NTSTATUS DsUsb_Ds3PairToFirstRadio(PDEVICE_CONTEXT Context);

VOID DsBth_Ds3Init(PDEVICE_CONTEXT Context);
