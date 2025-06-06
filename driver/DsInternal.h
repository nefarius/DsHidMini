/*
 * Some type definitions that are not exposed in the UMDF WDK
 */

#pragma once

//
// Host address the device is currently paired to, if supported
// 
// NOTE: this is a system-included property, but not defined in the WDK
// 
// {a92f26ca-eda7-4b1d-9db2-27b68aa5a2eb}
DEFINE_DEVPROPKEY(DEVPKEY_BluetoothRadio_Address,
	0xa92f26ca, 0xeda7, 0x4b1d, 0x9d, 0xb2, 0x27, 0xb6, 0x8a, 0xa5, 0xa2, 0xeb,
	1
); // DEVPROP_TYPE_UINT64

#ifndef KENEL_MODE

/*
 * Define properties not defined in the user-mode portion of the WDK
 */

DEFINE_DEVPROPKEY(DEVPKEY_Bluetooth_DeviceAddress,
	0x2bd67d8b, 0x8beb, 0x48d5, 0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a, 1);     // DEVPROP_TYPE_STRING

DEFINE_DEVPROPKEY(DEVPKEY_Bluetooth_DeviceVID,
	0x2bd67d8b, 0x8beb, 0x48d5, 0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a, 7);     // DEVPROP_TYPE_UINT16

DEFINE_DEVPROPKEY(DEVPKEY_Bluetooth_DevicePID,
	0x2bd67d8b, 0x8beb, 0x48d5, 0x87, 0xe0, 0x6c, 0xda, 0x34, 0x28, 0x04, 0x0a, 8);     // DEVPROP_TYPE_UINT16

#endif
