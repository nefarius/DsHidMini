#pragma once

// {399ED672-E0BD-4FB3-AB0C-4955B56FB86A}
DEFINE_GUID(GUID_DEVINTERFACE_DSHIDMINI,
	0x399ed672, 0xe0bd, 0x4fb3, 0xab, 0xc, 0x49, 0x55, 0xb5, 0x6f, 0xb8, 0x6a);

/*
 * Readable (status) properties
 */

// {52FAC1DA-5A52-40F5-A123-367F760F8BC2}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_BatteryStatus,
	0x52fac1da, 0x5a52, 0x40f5, 0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2, 2); // DEVPROP_TYPE_BYTE

/*
 * Readable/writable (configuration) properties
 */

// {52FAC1DA-5A52-40F5-A123-367F760F8BC2}
DEFINE_DEVPROPKEY(DEVPKEY_DsHidMini_HidDeviceMode,
	0x52fac1da, 0x5a52, 0x40f5, 0xa1, 0x23, 0x36, 0x7f, 0x76, 0xf, 0x8b, 0xc2, 3); // DEVPROP_TYPE_BYTE
