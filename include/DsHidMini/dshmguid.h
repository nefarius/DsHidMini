#pragma once

//
// Interface GUID used to identify/enumerate devices running under the driver
// 

// {16F3FE42-B710-4F67-B6EE-9A8D249C9CE5}
DEFINE_GUID(GUID_DEVINTERFACE_DSHIDMINI, 
	0x16f3fe42, 0xb710, 0x4f67, 0xb6, 0xee, 0x9a, 0x8d, 0x24, 0x9c, 0x9c, 0xe5);


//
// Use this category for driver state only
// Driver writes reads and writes during startup/shutdown
// Applications may read only
// 

// {6D293077-C3D6-4062-9597-BE4389404C02}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RW_HidDeviceMode,
    0x6d293077, 0xc3d6, 0x4062, 0x95, 0x97, 0xbe, 0x43, 0x89, 0x40, 0x4c, 0x2, 2); // DEVPROP_TYPE_BYTE


//
// Use this category for status/state reporting only
// Driver writes during operation
// Applications read only
// 

// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_BatteryStatus,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 2); // DEVPROP_TYPE_BYTE

// {3FECF510-CC94-4FBE-8839-738201F84D59}
__declspec(deprecated)
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_LastPairingStatus,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 3); // DEVPROP_TYPE_NTSTATUS

// See: https://github.com/nefarius/DsHidMini/issues/50
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationData,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 4); // DEVPROP_TYPE_BINARY

// See: https://github.com/nefarius/DsHidMini/pull/360
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_LastHostRequestStatus,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 5); // DEVPROP_TYPE_NTSTATUS
