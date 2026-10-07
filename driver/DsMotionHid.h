#pragma once

//
// Pure (WDF-free) conversions from the canonical calibrated motion sample
// (DS_MOTION_SAMPLE, see DsMotion.h) to the public HID motion formats. Kept in
// a header so ConfigParser.Tests can exercise it without the driver stack.
//
// Source frame (docs/MOTION.md, "Orientation / sign table"), after Sony's X
// mirror:
//   X towards the left grip, Y towards the trigger edge, Z through the pad
//   towards its back. A reading is +1 g when that axis points upwards.
//   Gyro: clockwise seen from above is positive.
//
// Public device frame (identical for the DS4 report and the HID Sensor
// collections; matches GameInput "Y-up right-handed" and SDL's gamepad frame):
//   X towards the right grip, Y out of the face (buttons) side, Z towards the
//   player. Flat on a table: accel (0, +1 g, 0). Angular velocity follows the
//   right-hand rule, so counter-clockwise seen from above is positive on Y.
//   The DS3 only has a single (yaw) gyro, so X and Z rotation are always 0.
//

#define DS_MOTION_HID_DS4_ACCEL_LSB_PER_G        8192
#define DS_MOTION_HID_DS4_GYRO_LSB_PER_DPS       16
#define DS_MOTION_HID_DS4_OFFSET_GYRO            13
#define DS_MOTION_HID_DS4_OFFSET_ACCEL           19
#define DS_MOTION_HID_DS4_OFFSET_TIMESTAMP       10

//
// HID Sensor collections (Generic Gamepad stays on report ID 1)
//
// (0x02 and 0x10-0x22 are taken by the PID force feedback reports)
#define DS_MOTION_HID_REPORT_ID_ACCEL            0x30
#define DS_MOTION_HID_REPORT_ID_GYRO             0x31

//
// Input report: ID, state, event, X, Y, Z (3 x int16 LE)
// Feature report: ID, reporting state, sensor status, sensitivity (u16 LE),
// report interval (u32 LE, milliseconds)
//
#define DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE   9
#define DS_MOTION_HID_SENSOR_FEATURE_REPORT_SIZE 9

#define DS_MOTION_HID_SENSOR_COUNT               2
#define DS_MOTION_HID_SENSOR_ACCEL               0
#define DS_MOTION_HID_SENSOR_GYRO                1

//
// Accelerometer values are milli-g (unit exponent -3), gyroscope values are
// 0.1 deg/s (unit exponent -1).
//
#define DS_MOTION_HID_GYRO_UNIT_PER_DPS          10

#define DS_MOTION_HID_REPORTING_STATE_NO_EVENTS  1
#define DS_MOTION_HID_REPORTING_STATE_ALL_EVENTS 2

#define DS_MOTION_HID_STATUS_READY               2
#define DS_MOTION_HID_STATUS_NO_DATA             4

#define DS_MOTION_HID_EVENT_DATA_UPDATED         4
#define DS_MOTION_HID_DEFAULT_INTERVAL_MS        10

typedef struct _DS_MOTION_HID_FRAME
{
	// milli-g in the public device frame
	INT32 AccelMilliG[3];
	// milli-deg/s in the public device frame
	INT32 GyroMilliDps[3];
} DS_MOTION_HID_FRAME, *PDS_MOTION_HID_FRAME;

typedef struct _DS_MOTION_HID_SENSOR_PROPS
{
	UCHAR ReportingState;
	USHORT Sensitivity;
	ULONG IntervalMs;
} DS_MOTION_HID_SENSOR_PROPS, *PDS_MOTION_HID_SENSOR_PROPS;

static __inline
SHORT
DsMotionHid_ClampS16(
	_In_ INT64 Value
)
{
	if (Value > 32767)
	{
		return 32767;
	}

	if (Value < -32767)
	{
		return -32767;
	}

	return (SHORT)Value;
}

static __inline
VOID
DsMotionHid_ToDeviceFrame(
	_In_ INT32 SrcAccelMilliGX,
	_In_ INT32 SrcAccelMilliGY,
	_In_ INT32 SrcAccelMilliGZ,
	_In_ INT32 SrcGyroMilliDps,
	_Out_ PDS_MOTION_HID_FRAME Frame
)
{
	// right = -X(left grip), up = -Z(back), towards player = -Y(trigger edge)
	Frame->AccelMilliG[0] = -SrcAccelMilliGX;
	Frame->AccelMilliG[1] = -SrcAccelMilliGZ;
	Frame->AccelMilliG[2] = -SrcAccelMilliGY;

	// Source: clockwise-from-above positive. Device Y points up: CCW positive.
	Frame->GyroMilliDps[0] = 0;
	Frame->GyroMilliDps[1] = -SrcGyroMilliDps;
	Frame->GyroMilliDps[2] = 0;
}

static __inline
VOID
DsMotionHid_WriteS16(
	_Out_writes_(2) PUCHAR Destination,
	_In_ SHORT Value
)
{
	Destination[0] = (UCHAR)((USHORT)Value & 0xFF);
	Destination[1] = (UCHAR)(((USHORT)Value >> 8) & 0xFF);
}

//
// Writes DS4 gyro (X, Y, Z) and accelerometer (X, Y, Z) into a 64-byte report
// (report ID at [0]); 16 LSB per deg/s and 8192 LSB per g like a real DS4.
//
static __inline
VOID
DsMotionHid_WriteDs4Report(
	_In_ const DS_MOTION_HID_FRAME* Frame,
	_Inout_updates_(64) PUCHAR Output
)
{
	for (int i = 0; i < 3; i++)
	{
		DsMotionHid_WriteS16(
			&Output[DS_MOTION_HID_DS4_OFFSET_GYRO + (i * 2)],
			DsMotionHid_ClampS16(((INT64)Frame->GyroMilliDps[i] * DS_MOTION_HID_DS4_GYRO_LSB_PER_DPS) / 1000)
		);
		DsMotionHid_WriteS16(
			&Output[DS_MOTION_HID_DS4_OFFSET_ACCEL + (i * 2)],
			DsMotionHid_ClampS16(((INT64)Frame->AccelMilliG[i] * DS_MOTION_HID_DS4_ACCEL_LSB_PER_G) / 1000)
		);
	}
}

//
// DS4 timestamp counter ticks in 1/187500 s (5.33 microseconds), wraps at 16 bit.
// Overflow-safe for any QPC value.
//
static __inline
USHORT
DsMotionHid_Ds4Timestamp(
	_In_ INT64 Qpc,
	_In_ INT64 Frequency
)
{
	if (Frequency <= 0 || Qpc < 0)
	{
		return 0;
	}

	const INT64 seconds = Qpc / Frequency;
	const INT64 remainder = Qpc % Frequency;
	const INT64 ticks = (seconds * 187500) + ((remainder * 187500) / Frequency);

	return (USHORT)(ticks & 0xFFFF);
}

//
// Writes a HID Sensor input report; Vector is already in the unit expected by
// the descriptor (milli-g or 0.1 deg/s).
//
static __inline
VOID
DsMotionHid_WriteSensorInputReport(
	_In_ UCHAR ReportId,
	_In_ UCHAR Status,
	_In_ const INT32 Vector[3],
	_Out_writes_(DS_MOTION_HID_SENSOR_INPUT_REPORT_SIZE) PUCHAR Output
)
{
	Output[0] = ReportId;
	Output[1] = Status;
	Output[2] = DS_MOTION_HID_EVENT_DATA_UPDATED;
	DsMotionHid_WriteS16(&Output[3], DsMotionHid_ClampS16(Vector[0]));
	DsMotionHid_WriteS16(&Output[5], DsMotionHid_ClampS16(Vector[1]));
	DsMotionHid_WriteS16(&Output[7], DsMotionHid_ClampS16(Vector[2]));
}

static __inline
VOID
DsMotionHid_SensorPropsInit(
	_Out_ PDS_MOTION_HID_SENSOR_PROPS Props
)
{
	Props->ReportingState = DS_MOTION_HID_REPORTING_STATE_ALL_EVENTS;
	Props->Sensitivity = 0;
	Props->IntervalMs = DS_MOTION_HID_DEFAULT_INTERVAL_MS;
}

static __inline
VOID
DsMotionHid_WriteSensorFeatureReport(
	_In_ UCHAR ReportId,
	_In_ UCHAR Status,
	_In_ const DS_MOTION_HID_SENSOR_PROPS* Props,
	_Out_writes_(DS_MOTION_HID_SENSOR_FEATURE_REPORT_SIZE) PUCHAR Output
)
{
	Output[0] = ReportId;
	Output[1] = Props->ReportingState;
	Output[2] = Status;
	Output[3] = (UCHAR)(Props->Sensitivity & 0xFF);
	Output[4] = (UCHAR)(Props->Sensitivity >> 8);
	Output[5] = (UCHAR)(Props->IntervalMs & 0xFF);
	Output[6] = (UCHAR)((Props->IntervalMs >> 8) & 0xFF);
	Output[7] = (UCHAR)((Props->IntervalMs >> 16) & 0xFF);
	Output[8] = (UCHAR)((Props->IntervalMs >> 24) & 0xFF);
}

static __inline
VOID
DsMotionHid_ReadSensorFeatureReport(
	_In_reads_(DS_MOTION_HID_SENSOR_FEATURE_REPORT_SIZE) const UCHAR* Input,
	_Out_ PDS_MOTION_HID_SENSOR_PROPS Props
)
{
	Props->ReportingState = Input[1];
	Props->Sensitivity = (USHORT)(Input[3] | (Input[4] << 8));
	Props->IntervalMs = (ULONG)Input[5] | ((ULONG)Input[6] << 8) | ((ULONG)Input[7] << 16) | ((ULONG)Input[8] << 24);
}
