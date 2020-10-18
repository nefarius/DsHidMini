#pragma once

// Output report Ids.
#define PID_SET_EFFECT_REPORT_ID 0x10
#define PID_SET_ENVELOPE_REPORT_ID 0x11
#define PID_SET_CONDITION_REPORT_ID 0x12
#define PID_SET_PERIODIC_REPORT_ID 0x13
#define PID_SET_CONSTANT_FORCE_REPORT_ID 0x14
#define PID_SET_RAMP_FORCE_REPORT_ID 0x15
#define PID_SET_CUSTOM_FORCE_DATA_REPORT_ID 0x16
#define PID_DOWNLOAD_SAMPLE_REPORT_ID 0x17
#define PID_EFFECT_OPERATION_REPORT_ID 0x18
#define PID_DEVICE_CONTROL_REPORT_ID 0x19
#define PID_BLOCK_FREE_REPORT_ID 0x1A
#define PID_DEVICE_GAIN_REPORT_ID 0x1B
#define PID_SET_CUSTOM_FORCE_REPORT_ID 0x1C

// Feature report Ids.
#define PID_NEW_EFFECT_REPORT_ID 0x20
#define PID_BLOCK_LOAD_REPORT_ID 0x21
#define PID_POOL_REPORT_ID 0x22

// Maximum number of EffectIndexBlocks & Simultaneous effects playing.
#define MAX_EFFECT_BLOCKS 0x7F

#define PID_DEVICE_RESET_CMD 0x04 

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

/** Possible PID device control values */
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

	PID_DEVICE_CONTROL Control;
	
} PID_DEVICE_CONTROL_REPORT, *PPID_DEVICE_CONTROL_REPORT;

typedef struct _PID_POOL_REPORT
{
	UCHAR ReportID;

	USHORT RAMPoolSize;

	UCHAR SimultaneousEffectsMax;

	UINT DeviceManagedPool : 1;

	UINT SharedParameterBlocks : 1;
	
} PID_POOL_REPORT, *PPID_POOL_REPORT;

typedef struct _PID_DEVICE_GAIN_REPORT
{
	UCHAR ReportID;

	UCHAR DeviceGain;
	
} PID_DEVICE_GAIN_REPORT, *PPID_DEVICE_GAIN_REPORT;

typedef struct _PID_CREATE_NEW_EFFECT_REPORT
{
	UCHAR ReportID;

	UCHAR EffectType;
	
} PID_CREATE_NEW_EFFECT_REPORT, *PPID_CREATE_NEW_EFFECT_REPORT;

typedef struct _PID_BLOCK_LOAD_REPORT
{
	UCHAR ReportID;

	UCHAR EffectBlockIndex;

	UCHAR BlockLoadStatus;

	USHORT RAMPoolAvailable;
	
} PID_BLOCK_LOAD_REPORT, *PPID_BLOCK_LOAD_REPORT;

#include <poppack.h>
