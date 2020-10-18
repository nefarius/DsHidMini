#pragma once

#define PID_INPUT_REPORT_ID						0x02

// Output report Ids.
#define PID_SET_EFFECT_REPORT_ID				0x10 // 16
#define PID_SET_ENVELOPE_REPORT_ID				0x11 // 17
#define PID_SET_CONDITION_REPORT_ID				0x12 // 18
#define PID_SET_PERIODIC_REPORT_ID				0x13 // 19
#define PID_SET_CONSTANT_FORCE_REPORT_ID		0x14 // 20
#define PID_SET_RAMP_FORCE_REPORT_ID			0x15 // 21
#define PID_SET_CUSTOM_FORCE_DATA_REPORT_ID		0x16 // 22
#define PID_DOWNLOAD_SAMPLE_REPORT_ID			0x17 // 23
#define PID_EFFECT_OPERATION_REPORT_ID			0x18 // 24
#define PID_DEVICE_CONTROL_REPORT_ID			0x19 // 25
#define PID_BLOCK_FREE_REPORT_ID				0x1A // 26
#define PID_DEVICE_GAIN_REPORT_ID				0x1B // 27
#define PID_SET_CUSTOM_FORCE_REPORT_ID			0x1C // 28

// Feature report Ids.
#define PID_NEW_EFFECT_REPORT_ID				0x20 // 32
#define PID_BLOCK_LOAD_REPORT_ID				0x21 // 33
#define PID_POOL_REPORT_ID						0x22 // 34

// Maximum number of EffectIndexBlocks & Simultaneous effects playing.
#define MAX_EFFECT_BLOCKS						0x7F // 127


/** Possible PID device control values */
typedef enum _PID_DEVICE_CONTROL
{
	PidDcEnableActuators = 1,
	PidDcDisableActuators = 2,
	PidDcStopAllEffects = 3,
	PidDcReset = 4,
	PidDcPause = 5,
	PidDcContinue = 6	
	
} PID_DEVICE_CONTROL;

/** Possible PID effect type values */
typedef enum _PID_EFFECT_TYPE
{
	PidEtConstantForce = 1,
	PidEtRamp = 2,
	PidEtSquare = 3,
	PidEtSine = 4,
	PidEtTriangle = 5,
	PidEtSawtoothUp = 6,
	PidEtSawtoothDown = 7,
	PidEtSpring = 8,
	PidEtDamper = 9,
	PidEtInertia = 10,
	PidEtFriction = 11
	
} PID_EFFECT_TYPE;

typedef enum _PID_BLOCK_LOAD_STATUS
{
	PidBlsSuccess = 1,
	PidBlsFull = 2,
	PidBlsError = 3
	
} PID_BLOCK_LOAD_STATUS;

#include <pshpack1.h>

typedef struct _PID_DEVICE_CONTROL_REPORT
{
	UCHAR ReportID;

	UCHAR Control;
	
} PID_DEVICE_CONTROL_REPORT, *PPID_DEVICE_CONTROL_REPORT;

typedef struct _PID_POOL_REPORT
{
	UCHAR ReportID;

	USHORT RAMPoolSize;

	UCHAR SimultaneousEffectsMax;

	UCHAR DeviceManagedPool : 1;

	UCHAR SharedParameterBlocks : 1;

	UCHAR : 6; // Padding
	
} PID_POOL_REPORT, *PPID_POOL_REPORT;

typedef struct _PID_DEVICE_GAIN_REPORT
{
	UCHAR ReportID;

	USHORT DeviceGain;
	
} PID_DEVICE_GAIN_REPORT, *PPID_DEVICE_GAIN_REPORT;

typedef struct _PID_CREATE_NEW_EFFECT_REPORT
{
	UCHAR ReportID;

	UCHAR EffectType;

	UCHAR ByteCount;
	
} PID_CREATE_NEW_EFFECT_REPORT, *PPID_CREATE_NEW_EFFECT_REPORT;

typedef struct _PID_BLOCK_LOAD_REPORT
{
	UCHAR ReportID;

	UCHAR EffectBlockIndex;

	UCHAR BlockLoadStatus;

	USHORT RAMPoolAvailable;
	
} PID_BLOCK_LOAD_REPORT, *PPID_BLOCK_LOAD_REPORT;

#include <poppack.h>
