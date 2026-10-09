#pragma once

//
// Public contract between DsHidMini (UMDF) and nssmkig / igfilter (KMDF).
// Canonical copy: DsHidMini/include/DsHidMini/nssmkig.h; nssmkig carries an
// identical copy in sys/nssmkig.h. Keep both in sync.
//
// Background (DsHidMini issue #374): UMDF
// WdfDeviceSetFailed(WdfDeviceFailedAttemptRestart) on the DsHidMini stack
// (hub PDO -> WUDFRd -> nssmkig -> mshidumdf) never produces a
// re-enumeration request towards usbhub3; the reflector marks the devnode
// failed and the USB device ends in Code 43 until it is replugged.
//
// nssmkig 1.2 and later therefore expose a control device
// (NSSMKIG_CONTROL_DEVICE_NAME, symbolic link NSSMKIG_SYMBOLIC_LINK_NAME).
// Device interfaces on the filter device itself are not used because any
// open of such a symbolic link is delivered to the HID class FDO at the top
// of the stack.
//
// IOCTL_NSSMKIG_REENUMERATE_SELF
//   Access: FILE_READ_DATA | FILE_WRITE_DATA (not FILE_ANY_ACCESS). A
//           handle opened with no read/write desired access must not be
//           able to force a device removal.
//   Input:  NUL-terminated UTF-16 device instance ID (as returned by
//           DEVPKEY_Device_InstanceId) of the devnode nssmkig is attached to.
//   Output: none.
//   USB instance IDs (prefix USB\) are re-enumerated with
//   IOCTL_INTERNAL_USB_CYCLE_PORT sent to the PDO. usbhub3 does not
//   implement GUID_REENUMERATE_SELF_INTERFACE_STANDARD on device PDOs.
//   Other buses, and USB stacks that reject the cycle, fall back to
//   querying that interface and calling SurpriseRemoveAndReenumerateSelf.
//   Returns STATUS_NOT_FOUND when no nssmkig instance is attached to the
//   given instance ID, STATUS_DEVICE_NOT_READY when a matching instance
//   exists but its PDO is unavailable, the RtlStringCchCopyW error when
//   the matched ID cannot be copied, and STATUS_NOT_SUPPORTED when neither
//   mechanism is available (for example a Bluetooth PDO).
//
// Older nssmkig binaries do not create the control device; opening
// NSSMKIG_WIN32_DEVICE_NAME then fails and DsHidMini leaves the device
// running in the already-probed HID mode.
//

#define NSSMKIG_CONTROL_DEVICE_NAME		L"\\Device\\nssmkig"
#define NSSMKIG_SYMBOLIC_LINK_NAME		L"\\DosDevices\\nssmkig"
#define NSSMKIG_WIN32_DEVICE_NAME		L"\\\\.\\nssmkig"

#ifndef FILE_DEVICE_NSSMKIG
#define FILE_DEVICE_NSSMKIG 0x8000
#endif

#define IOCTL_NSSMKIG_REENUMERATE_SELF \
	((ULONG)CTL_CODE(FILE_DEVICE_NSSMKIG, 0x800, METHOD_BUFFERED, FILE_READ_DATA | FILE_WRITE_DATA))
