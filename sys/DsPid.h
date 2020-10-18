#pragma once

/*
 * The Report IDs defined here must match the ones used in the report descriptor.
 */

//
// Output
// 

#define PID_DEVICE_CONTROL_REPORT_ID	0x1C // 28
#define PID_DEVICE_GAIN_REPORT_ID		0x1D // 29

//
// Features
// 

#define PID_POOL_REPORT_ID				0x13 // 19
#define PID_CREATE_NEW_EFFECT_REPORT_ID	0x11 // 17

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

#include <poppack.h>
