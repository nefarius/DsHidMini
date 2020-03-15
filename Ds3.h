#pragma once

#define DS3_HID_COMMAND_ENABLE_SIZE             0x04

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

typedef enum _DS3_FEATURE_VALUE
{
    Ds3FeatureDeviceAddress = 0x03F2,
    Ds3FeatureStartDevice = 0x03F4,
    Ds3FeatureHostAddress = 0x03F5

} DS3_FEATURE_VALUE;

NTSTATUS Ds3Init(PDEVICE_CONTEXT Context);
