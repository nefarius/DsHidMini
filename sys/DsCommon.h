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
    DsDeviceTypeUnknown,

    //
    // Sony DualShock 3 Controller
    // 
    DsDeviceTypeSixaxis,

    //
	// Sony Navigation Controller
	// 
	DsDeviceTypeNavigation,

	//
	// Sony Motion Controller
	// 
	DsDeviceTypeMotion,
	
    //
    // Sony DualShock 4 Controller
    // 
    DsDeviceTypeWireless

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
