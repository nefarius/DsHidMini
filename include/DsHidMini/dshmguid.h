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

// One-based IPC SlotIndex / message TargetIndex; matches shared HID region and per-slot wait events
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IpcSlotIndex,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 6); // DEVPROP_TYPE_UINT32

// TRUE if DeviceAddress was not reported by the hardware (GET Feature 0xF2 failed) and was
// synthesized instead; see issue #321
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_DeviceAddressSynthesized,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 7); // DEVPROP_TYPE_BOOLEAN

// Feature 0x01 firmware/board revision packed as b2<<16 | b3<<8 | b4
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationFirmware,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 8); // DEVPROP_TYPE_UINT32

// Feature 0x01 pad/sensor type byte (offset 8); informational only, not a gyro-path test
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationPadType,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 9); // DEVPROP_TYPE_BYTE

// Feature 0x01 motion path derived from the calibration field list
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationMotionPath,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 10); // DEVPROP_TYPE_BYTE

// TRUE if Feature 0x01 matches the clone heuristic (field list 01 02 and byte 0x29 == 0x64)
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_IdentificationCloneHeuristic,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 11); // DEVPROP_TYPE_BOOLEAN

// Hardware family (DS_DEVICE_TYPE). Navigation (2) has one LED and no rumble; see issue #48
// {3FECF510-CC94-4FBE-8839-738201F84D59}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_RO_DeviceType,
	0x3fecf510, 0xcc94, 0x4fbe, 0x88, 0x39, 0x73, 0x82, 0x1, 0xf8, 0x4d, 0x59, 12); // DEVPROP_TYPE_BYTE
