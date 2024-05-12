#pragma once

//
// Interface GUID used to identify/enumerate devices running under the driver
// 

// {399ED672-E0BD-4FB3-AB0C-4955B56FB86A}
DEFINE_GUID(GUID_DEVINTERFACE_DSHIDMINI,
	0x399ed672, 0xe0bd, 0x4fb3, 0xab, 0xc, 0x49, 0x55, 0xb5, 0x6f, 0xb8, 0x6a);


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
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_LastPairingStatus,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 3); // DEVPROP_TYPE_NTSTATUS

// See: https://github.com/nefarius/DsHidMini/issues/50
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationData,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 4); // DEVPROP_TYPE_BINARY

// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_LastHostRequestStatus,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 5); // DEVPROP_TYPE_NTSTATUS
