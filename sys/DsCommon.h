#pragma once

/**
* \typedef struct _BD_ADDR
*
* \brief   Defines a Bluetooth client MAC address.
*/
typedef struct _BD_ADDR
{
    UCHAR Address[6];

} BD_ADDR, * PBD_ADDR;

/**
 * \enum    _DS_DEVICE_TYPE
 *
 * \brief   Device type.
 */
typedef enum _DS_DEVICE_TYPE
{
    //
    // Unknown device type
    // 
    DS_DEVICE_TYPE_UNKNOWN,

    //
    // Sony DualShock 3 Controller
    // 
    DS_DEVICE_TYPE_PS3_DUALSHOCK,

    //
    // Sony DualShock 4 Controller
    // 
    DS_DEVICE_TYPE_PS4_DUALSHOCK,

    //
    // Sony Navigation Controller
    // 
    DS_DEVICE_TYPE_PS3_NAVIGATION,

    //
    // Sony Motion Controller
    // 
    DS_DEVICE_TYPE_PS3_MOTION

} DS_DEVICE_TYPE, * PDS_DEVICE_TYPE;

/**
 * \enum    _DS_CONNECTION_TYPE
 *
 * \brief   Device connection type.
 */
typedef enum _DS_CONNECTION_TYPE
{
    DsDeviceConnectionTypeUnknown = 0x00,
    DsDeviceConnectionTypeUsb,
    DsDeviceConnectionTypeBth

} DS_CONNECTION_TYPE, * PDS_CONNECTION_TYPE;

typedef enum _DS_BATTERY_STATUS
{
    DsBatteryStatusNone = 0x00,
    DsBatteryStatusDying = 0x01,
    DsBatteryStatusLow = 0x02,
    DsBatteryStatusMedium = 0x03,
    DsBatteryStatusHigh = 0x04,
    DsBatteryStatusFull = 0x05,
    DsBatteryStatusCharging = 0xEE,
    DsBatteryStatusCharged = 0xEF
	
} DS_BATTERY_STATUS, *PDS_BATTERY_STATUS;

/**
 * \enum    _DS_FEATURE_TYPE
 *
 * \brief   Supported feature requests. These values must match the IDs in the report descriptor.
 */
typedef enum _DS_FEATURE_TYPE
{
    //
    // Receive controller-embedded Bluetooth host address
    // 
    DS_FEATURE_TYPE_GET_HOST_BD_ADDR = 0xC0,

    //
    // Update controller-embedded Bluetooth host address
    // 
    DS_FEATURE_TYPE_SET_HOST_BD_ADDR = 0xC1,

    //
    // Receive controller-embedded Bluetooth address
    // 
    DS_FEATURE_TYPE_GET_DEVICE_BD_ADDR = 0xC2,

    //
    // Receive device type (DS3, DS4, ...)
    // 
    DS_FEATURE_TYPE_GET_DEVICE_TYPE = 0xC3,

    //
    // Receive device connection type (USB, Bluetooth)
    // 
    DS_FEATURE_TYPE_GET_CONNECTION_TYPE = 0xC4,

    //
    // Receive current volatile configuration properties
    // 
    DS_FEATURE_TYPE_GET_DEVICE_CONFIG = 0xC5,

    //
    // Update current volatile configuration properties
    // 
    DS_FEATURE_TYPE_SET_DEVICE_CONFIG = 0xC6,

	//
	// Receive current battery status
	// 
    DS_FEATURE_TYPE_GET_BATTERY_STATUS = 0xC7

} DS_FEATURE_TYPE, * PDS_FEATURE_TYPE;

typedef enum _DS_HID_DEVICE_MODE
{
    DsHidMiniDeviceModeUnknown = 0x00,
    DsHidMiniDeviceModeSingle,
    DsHidMiniDeviceModeMulti,
    DsHidMiniDeviceModeSixaxisCompatible

} DS_HID_DEVICE_MODE, * PDS_HID_DEVICE_MODE;

#include <pshpack1.h>

//
// Per device dynamic configuration properties
// 
typedef struct _DS_DRIVER_CONFIGURATION
{
    DS_HID_DEVICE_MODE HidDeviceMode;

    BOOLEAN MuteDigitalPressureButtons;

    USHORT VendorId;

    USHORT ProductId;

    USHORT VersionNumber;

	BOOLEAN DisableAutoPairing;

} DS_DRIVER_CONFIGURATION, * PDS_DRIVER_CONFIGURATION;

#include <poppack.h>