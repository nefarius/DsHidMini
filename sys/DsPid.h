#pragma once

typedef enum _PID_DEVICE_CONTROL
{
	PidDcEnableActuators = 1,
	PidDcDisableActuators = 2,
	PidDcStopAllEffects = 3,
	PidDcReset = 4,
	PidDcPause = 5,
	PidDcContinue = 6	
	
} PID_DEVICE_CONTROL;
