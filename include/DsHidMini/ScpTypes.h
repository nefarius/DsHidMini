#pragma once

#include <pshpack1.h>
/**
 * SCP custom report format for DualShock 3 Controllers.
 *
 * @author	Benjamin "Nefarius" Höglinger-Stelzer
 * @date	18.04.2021
 */
typedef struct _SCP_EXTN {
	FLOAT SCP_UP;
	FLOAT SCP_RIGHT;
	FLOAT SCP_DOWN;
	FLOAT SCP_LEFT;

	FLOAT SCP_LX;
	FLOAT SCP_LY;

	FLOAT SCP_L1;
	FLOAT SCP_L2;
	FLOAT SCP_L3;

	FLOAT SCP_RX;
	FLOAT SCP_RY;

	FLOAT SCP_R1;
	FLOAT SCP_R2;
	FLOAT SCP_R3;

	FLOAT SCP_T;
	FLOAT SCP_C;
	FLOAT SCP_X;
	FLOAT SCP_S;

	FLOAT SCP_SELECT;
	FLOAT SCP_START;

	FLOAT SCP_PS;
} SCP_EXTN, * PSCP_EXTN;
#include <poppack.h>
