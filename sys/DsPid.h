#pragma once

/*
 * The Report IDs defined here must match the ones used in the report descriptor.
 */

#define PID_DEVICE_CONTROL_REPORT_ID	0x1C // 28
#define PID_POOL_REPORT_ID				0x13 // 19

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

#include <poppack.h>
