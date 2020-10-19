#pragma once

#define PID_INPUT_REPORT_ID						0x04 // 4

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

//
// [OPTIONAL] The virtual device does not require this input report 
// to be sent by the client in order for proper functioning
//
typedef struct _PID_STATE_REPORT
{
	UCHAR  ReportId;	// Report ID = 0x02
	UCHAR  DevicePaused : 1;	// Value = 0 to 1
	UCHAR  ActuatorsEnabled : 1;	// Value = 0 to 1
	UCHAR  SafetySwitch : 1;	// Value = 0 to 1
	UCHAR  ActuatorOverrideSwitch : 1;	// Value = 0 to 1
	UCHAR  ActuatorPower : 1;	// Value = 0 to 1
	UCHAR : 3;	// Pad

    UCHAR ReportEffectPlaying : 1;	// Value = 0 to 1
    UCHAR EffectBlockIndex : 7;	// Value = 1 to MAX_EFFECT_BLOCKS (127)
} PID_STATE_REPORT, * PPID_STATE_REPORT;


typedef struct _PID_SET_EFFECT_REPORT
{
	UCHAR ReportId;	// Report ID = 0x10
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	UCHAR EffectType;	// Constant = 1,
				// Ramp = 2,
				// Square = 3,
				// Sine = 4,
				// Triangle = 5,
				// SawtoothUp = 6,
				// SawtoothDown = 7,
				// Spring = 8,
				// Damper = 9,
				// Inertia = 10,
				// Friction = 11,
				// CustomForce = 12

	USHORT Duration;   // Value = 0 to 10000
	USHORT TriggerRepeatInterval; // Value = 0 to 10000
	USHORT SamplePeriod; // Value = 0 to 10000
	USHORT Gain; // Value = 0 to 10000
	UCHAR TriggerButton; // Value = 1 to 128

	UCHAR AxesEnableX : 1; // Value = 0 to 1
	UCHAR AxesEnableY : 1; // Value = 0 to 1
	UCHAR DirectionEnable : 1; // Value = 0 to 1, Physical = Value
	UCHAR : 5;	// Pad

	USHORT DirectionInstance1;	// Value = 0 to 36000
	USHORT DirectionInstance2;	// Value = 0 to 36000
	USHORT StartDelay;	// Value = 0 to 10000
} PID_SET_EFFECT_REPORT, * PPID_SET_EFFECT_REPORT;


typedef struct _PID_SET_ENVELOPE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x11
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	USHORT AttackLevel; // Value = 0 to 10000
	USHORT FadeLevel; // Value = 0 to 10000
	USHORT AttackTime; // Value = 0 to 10000
	USHORT FadeTime; // Value = 0 to 10000
} PID_SET_ENVELOPE_REPORT, * PPID_SET_ENVELOPE_REPORT;


typedef struct _PID_SET_CONDITION_REPORT
{
	UCHAR ReportId;	// Report ID = 0x12
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	UCHAR ParameterBlockOffset : 4; // Value = 0 to 1
	UCHAR TypeSpecificBlockOffsetInstance1 : 2; // Value = 0 to 1
	UCHAR TypeSpecificBlockOffsetInstance2 : 2; // Value = 0 to 1
	SHORT CpOffset;	// Value = -10000 to 10000
	SHORT PositiveCoefficient; // Value = -10000 to 10000
	SHORT NegativeCoefficient; // Value = -10000 to 10000
	USHORT PositiveSaturation; // Value = 0 to 10000
	USHORT NegativeSaturation; // Value = 0 to 10000
	USHORT DeadBand;  // Value = 0 to 10000
} PID_SET_CONDITION_REPORT, * PPID_SET_CONDITION_REPORT;

typedef struct _PID_SET_PERIODIC_REPORT
{
	UCHAR ReportId;	// Report ID = 0x13
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	USHORT Magnitude;	// Value = 0 to 10000
	SHORT Offset;	// Value = -10000 to 10000
	USHORT Phase;	// Value = 0 to 36000
	USHORT Period;	// Value = 0 to 10000
} PID_SET_PERIODIC_REPORT, * PPID_SET_PERIODIC_REPORT;

typedef struct _PID_SET_CONSTANT_FORCE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x14
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	SHORT Magnitude; // Value = -10000 to 10000
} PID_SET_CONSTANT_FORCE_REPORT, * PPID_SET_CONSTANT_FORCE_REPORT;

typedef struct _PID_SET_RAMP_FORCE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x15
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	SHORT RampStart;  // Value = -10000 to 10000
	SHORT RampEnd;    // Value = -10000 to 10000
} PID_SET_RAMP_FORCE_REPORT, * PPID_SET_RAMP_FORCE_REPORT;

typedef struct _PID_SET_CUSTOM_FORCE_DATA_REPORT
{
	UCHAR ReportId;	// Report ID = 0x16
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	USHORT CustomForceDataOffset; // Value = 0 to 10000
	UCHAR CustomForceData[12];
} PID_SET_CUSTOM_FORCE_DATA_REPORT, * PPID_SET_CUSTOM_FORCE_DATA_REPORT;

typedef struct _PID_DOWNLOAD_SAMPLE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x17
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	CHAR  ForceSampleX; // Value = -127 to 127
	CHAR  ForceSampleY; // Value = -127 to 127
} PID_DOWNLOAD_SAMPLE_REPORT, * PPID_DOWNLOAD_SAMPLE_REPORT;


typedef struct _PID_EFFECT_OPERATION_REPORT
{
	UCHAR ReportId;	// Report ID = 0x18
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	UCHAR EffectOperation;	// Start = 1,
					// StartSolo = 2,
					// Stop = 3
	UCHAR LoopCount; // Value = 0 to 255;
} PID_EFFECT_OPERATION_REPORT, * PPID_EFFECT_OPERATION_REPORT;

typedef struct _PID_DEVICE_CONTROL_REPORT
{
	UCHAR ReportId;	// Report ID = 0x19
	UCHAR DeviceControlCommand;	// EnableActuactors = 1,
						// DisableActuactors = 2,
						// StopAllEffects = 3,
						// Reset = 4,
						// Pause = 5,
						// Continue = 6

} PID_DEVICE_CONTROL_REPORT, * PPID_DEVICE_CONTROL_REPORT;


typedef struct _PID_BLOCK_FREE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x1A
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
} PID_BLOCK_FREE_REPORT, * PPID_BLOCK_FREE_REPORT;



typedef struct _PID_DEVICE_GAIN_REPORT
{
	UCHAR ReportId;	// Report ID = 0x1B
	USHORT DeviceGain;	// Value = 0 to 10000
} PID_DEVICE_GAIN_REPORT, * PPID_DEVICE_GAIN_REPORT;

typedef struct _PID_SET_CUSTOM_FORCE_REPORT
{
	UCHAR ReportId;	// Report ID = 0x1C
	UCHAR EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
	UCHAR SampleCount; // Value = 0 to 255
	USHORT SamplePeriod; // Value = 0 to 10000
} PID_SET_CUSTOM_FORCE_REPORT, * PPID_SET_CUSTOM_FORCE_REPORT;

typedef struct _PID_NEW_EFFECT_REPORT
{
	UCHAR ReportId;	// Report ID = 0x20
	UCHAR EffectType;	// Constant = 1,
				// Ramp = 2,
				// Square = 3,
				// Sine = 4,
				// Triangle = 5,
				// SawtoothUp = 6,
				// SawtoothDown = 7,
				// Spring = 8,
				// Damper = 9,
				// Inertia = 10,
				// Friction = 11,
				// CustomForce = 12
	UCHAR ByteCount;	// Applies to CustomForce only.

} PID_NEW_EFFECT_REPORT, * PPID_NEW_EFFECT_REPORT;


typedef struct _PID_BLOCK_LOAD_REPORT
{
	UCHAR  ReportId;	// Report ID = 0x21
	UCHAR  EffectBlockIndex; // Value = 1 to MAX_EFFECT_BLOCKS(127); 
					// Value = 0 if operation fails.
	UCHAR  BlockLoadStatus; // Success = 1
				// Full = 2
				// Error = 3
	USHORT RamPoolAvailable; //Value = 0 to 65535
} PID_BLOCK_LOAD_REPORT, * PPID_BLOCK_LOAD_REPORT;


//
// Handled by the driver internally.
//
typedef struct _PID_POOL_REPORT
{
	UCHAR  ReportId; // Report ID = 0x22
	USHORT RamPoolSize;  // Value = 0 to 65535
	UCHAR  SimultaneousEffectsMax; // MAX_EFFECT_BLOCKS(127);
	UCHAR  DeviceManagedPool : 1; // Value = 0 to 1
	UCHAR  SharedParameterBlocks : 1; // Value = 0 to 1
	UCHAR : 6;
} PID_POOL_REPORT, * PPID_POOL_REPORT;

#include <poppack.h>
