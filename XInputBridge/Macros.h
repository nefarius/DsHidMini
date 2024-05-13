#pragma once

//
// Dead-Zone value to stop jittering
// 
#define DS3_AXIS_ANTI_JITTER_OFFSET		10

//
// Constants
// 

#define DS3_VID							0x054C
#define DS3_PID							0x0268
#define SXS_MODE_GET_FEATURE_REPORT_ID	0xF2
#define SXS_MODE_GET_FEATURE_BUFFER_LEN	0x40
#define DS3_DEVICES_MAX					8
#define LOGGER_NAME						"XInputBridge"
#define XI_SYSTEM_LIB_NAME				"XInput1_3.dll"

// {EC87F1E3-C13B-4100-B5F7-8B84D54260CB}
DEFINE_GUID(XUSB_INTERFACE_CLASS_GUID,
	0xEC87F1E3, 0xC13B, 0x4100, 0xB5, 0xF7, 0x8B, 0x84, 0xD5, 0x42, 0x60, 0xCB);


//
// Helpers
// 

#define NAMEOF(name) #name
#define CALL_FPN_SAFE(fpn, ...)	((fpn)) ? (fpn)(__VA_ARGS__) : ERROR_DEVICE_NOT_CONNECTED
#define CALL_FPN_SAFE_NO_RETURN(fpn, ...)	((fpn)) ? (fpn)(__VA_ARGS__) : void(0)

