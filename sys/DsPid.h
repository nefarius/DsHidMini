#pragma once

/*
 * The Report IDs defined here must match the ones used in the report descriptor.
 */

#define PID_DEVICE_CONTROL_REPORT_ID	0x1C // 28

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

typedef struct _PID_DEVICE_CONTROL_REPORT
{
	UCHAR ReportID;

	PID_DEVICE_CONTROL Control;
	
} PID_DEVICE_CONTROL_REPORT, *PPID_DEVICE_CONTROL_REPORT;
