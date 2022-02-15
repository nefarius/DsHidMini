#pragma once


//
// Defines a Bluetooth client MAC address
// 
#include <pshpack1.h>
typedef struct _BD_ADDR
{
	UCHAR Address[6];

} BD_ADDR, * PBD_ADDR;
#include <poppack.h>

//
// Device type
// 
typedef enum
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

//
// Device connection type
// 
typedef enum
{
	DsDeviceConnectionTypeUnknown = 0x00,
	DsDeviceConnectionTypeUsb,
	DsDeviceConnectionTypeBth

} DS_CONNECTION_TYPE, * PDS_CONNECTION_TYPE;

//
// Battery status values reported by the hardware
// 
typedef enum
{
	DsBatteryStatusNone = 0x00,
	DsBatteryStatusDying = 0x01,
	DsBatteryStatusLow = 0x02,
	DsBatteryStatusMedium = 0x03,
	DsBatteryStatusHigh = 0x04,
	DsBatteryStatusFull = 0x05,
	DsBatteryStatusCharging = 0xEE,
	DsBatteryStatusCharged = 0xEF

} DS_BATTERY_STATUS, * PDS_BATTERY_STATUS;

//
// See https://vigem.org/projects/DsHidMini/HID-Device-Modes-Explained
// 
typedef enum
{
	DsHidMiniDeviceModeUnknown = 0,
	DsHidMiniDeviceModeSDF,
	DsHidMiniDeviceModeGPJ,
	DsHidMiniDeviceModeSixaxisCompatible,
	DsHidMiniDeviceModeDS4WindowsCompatible,
	DsHidMiniDeviceModeXInputHIDCompatible

} DS_HID_DEVICE_MODE, * PDS_HID_DEVICE_MODE;

//
// String-representation of the modes ordered by index
// 
static CONST PSTR G_HID_DEVICE_MODE_NAMES[] = 
{
	"\0", // Reserved/invalid
	"SDF",
	"GPJ",
	"SXS",
	"DS4Windows",
	"XInput"
};

//
// Output report processing mode
// 
typedef enum
{
	//
	// DSHM controls output report generation
	// 
	Ds3OutputReportModeDriverHandled = 0,

	//
	// Output reports come in from "the outside" and get passed on
	// 
	Ds3OutputReportModeWriteReportPassThrough

} DS_OUTPUT_REPORT_MODE, * PDS_OUTPUT_REPORT_MODE;

//
// The originator of output reports
// 
typedef enum
{
	//
	// Request originated from ourself (must not be discarded)
	// 
	Ds3OutputReportSourceDriverHighPriority = 0,

	//
	// Request originated from ourself (may be discarded)
	// 
	Ds3OutputReportSourceDriverLowPriority,

	//
	// Request came in through Force Feedback exposure
	// 
	Ds3OutputReportSourceForceFeedback,

	//
	// Request came in through SXS exposure
	// 
	Ds3OutputReportSourcePassThrough,

	//
	// Request came in from DS4 emulation
	// 
	Ds3OutputReportSourceDualShock4,

	//
	// Request came from XINPUTHID.SYS
	// 
	Ds3OutputReportSourceXInputHID

} DS_OUTPUT_REPORT_SOURCE, * PDS_OUTPUT_REPORT_SOURCE;

//
// Flags of whether pressure sensitive button variants should be exposed as digital, analogue or both
// 
typedef enum
{
	//
	// Only report digital (two-state) pressure sensitive button state changes
	// 
	DsPressureExposureModeDigital = 1 << 0,
	//
	// Only report analogue (range) pressure sensitive button state changes
	// 
	DsPressureExposureModeAnalogue = 1 << 1,
	//
	// Default behaviour exposes both unaltered
	// 
	DsPressureExposureModeDefault = DsPressureExposureModeDigital | DsPressureExposureModeAnalogue

} DS_PRESSURE_EXPOSURE_MODE, * PDS_PRESSURE_EXPOSURE_MODE;

//
// Flags of whether the D-Pad should be reported as HAT switch or individual buttons
typedef enum
{
	//
	// Expose as default HAT/POV format
	// 
	DsDPadExposureModeHAT = 1 << 0,
	//
	// Expose as individual buttons
	// 
	DsDPadExposureModeIndividualButtons = 1 << 1,
	//
	// Default behaviour exposes HAT/POV format
	// 
	DsDPadExposureModeDefault = DsDPadExposureModeHAT

} DS_DPAD_EXPOSURE_MODE, * PDS_DPAD_EXPOSURE_MODE;

//
// Axis dead-zone settings
// 
typedef struct _DS_AXIS_DEADZONE
{
	BOOLEAN Apply;

	DOUBLE PolarValue;

} DS_AXIS_DEADZONE, * PDS_AXIS_DEADZONE;

//
// Thumb settings
// 
typedef struct _DS_THUMB_SETTINGS
{
	DS_AXIS_DEADZONE DeadZoneLeft;

	DS_AXIS_DEADZONE DeadZoneRight;

} DS_THUMB_SETTINGS, * PDS_THUMB_SETTINGS;

//
// Rumble settings
// 
typedef struct _DS_RUMBLE_SETTINGS
{
	BOOLEAN DisableBM;

	BOOLEAN DisableSM;

	struct
	{
		BOOLEAN Enabled;

		UCHAR MinValue;

		UCHAR MaxValue;
		
	} BMStrRescale;

	struct
	{
		BOOLEAN Enabled;

		UCHAR RescaleMinValue;

		UCHAR RescaleMaxValue;
				
	} SMToBMConversion;

	struct
	{
		BOOLEAN BMThresholdEnabled;

		UCHAR BMThresholdValue;

		BOOLEAN SMThresholdEnabled;

		UCHAR SMThresholdValue;
		
	} ForcedSM;

} DS_RUMBLE_SETTINGS, * PDS_RUMBLE_SETTINGS;

//
// Per device dynamic configuration properties
// 
typedef struct _DS_DRIVER_CONFIGURATION
{
	/** The HID device mode */
	DS_HID_DEVICE_MODE HidDeviceMode;

	/** When set, pairing will not be attempted on device boot */
	BOOLEAN DisableAutoPairing;

	/** True if output rate control is enabled, false if not */
	BOOLEAN IsOutputRateControlEnabled;

	/** The output rate control period in milliseconds */
	UCHAR OutputRateControlPeriodMs;

	/** True if output deduplicator is enabled, false if not */
	UCHAR IsOutputDeduplicatorEnabled;

	/** Idle disconnect period in milliseconds */
	ULONG WirelessIdleTimeoutPeriodMs;

	//
	// Thumb stick specific settings, can be set per mode
	// 
	DS_THUMB_SETTINGS ThumbSettings;

	//
	// Rumble customizing
	// 
	DS_RUMBLE_SETTINGS RumbleSettings;

	//
	// SDF-mode specific
	// 
	struct
	{
		DS_PRESSURE_EXPOSURE_MODE PressureExposureMode;

		DS_DPAD_EXPOSURE_MODE DPadExposureMode;

	} SDF;

	//
	// GPJ-mode specific
	// 
	struct
	{
		DS_PRESSURE_EXPOSURE_MODE PressureExposureMode;

		DS_DPAD_EXPOSURE_MODE DPadExposureMode;

	} GPJ;

} DS_DRIVER_CONFIGURATION, * PDS_DRIVER_CONFIGURATION;
