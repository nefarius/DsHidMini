﻿#pragma once

//
// Dead-Zone value to stop jittering
// 
#define DS3_AXIS_ANTI_JITTER_OFFSET		10

#define IS_OUTSIDE_DZ(_axis_)	((_axis_) < ((UCHAR_MAX / 2) - DS3_AXIS_ANTI_JITTER_OFFSET) \
								|| (_axis_) > ((UCHAR_MAX / 2) + DS3_AXIS_ANTI_JITTER_OFFSET))


//
// Constants
// 

#define DS3_VID							0x054C
#define DS3_PID							0x0268
#define SXS_MODE_GET_FEATURE_REPORT_ID	0xF2
#define SXS_MODE_GET_FEATURE_BUFFER_LEN	0x40
#define DS3_DEVICES_MAX					8
#define XUSB_DEVICES_MAX				DS3_DEVICES_MAX
#define LOGGER_NAME						"XInputBridge"
#define TRACER_NAME						LOGGER_NAME
#define XI_SYSTEM_LIB_NAME				"XInput1_3.dll"
#define MAX_STARTUP_WAIT_MS				3000 // ms

// {EC87F1E3-C13B-4100-B5F7-8B84D54260CB}
DEFINE_GUID(XUSB_INTERFACE_CLASS_GUID,
	0xEC87F1E3, 0xC13B, 0x4100, 0xB5, 0xF7, 0x8B, 0x84, 0xD5, 0x42, 0x60, 0xCB);


//
// Helpers
// 

#define NAMEOF(name) #name
#define CALL_FPN_SAFE(fpn, ...)	((fpn)) ? (fpn)(__VA_ARGS__) : ERROR_DEVICE_NOT_CONNECTED
#define CALL_FPN_SAFE_NO_RETURN(fpn, ...)	((fpn)) ? (fpn)(__VA_ARGS__) : void(0)

#if defined(SCPLIB_ENABLE_TELEMETRY)
#define LOG_INFO(_body_, ...)	GlobalState::GetLogger(__FUNCTION__)->Info(fmt::format(_body_, __VA_ARGS__))
#define LOG_WARN(_body_, ...)	GlobalState::GetLogger(__FUNCTION__)->Warn(fmt::format(_body_, __VA_ARGS__))
#define LOG_ERROR(_body_, ...)	GlobalState::GetLogger(__FUNCTION__)->Error(fmt::format(_body_, __VA_ARGS__))
#else
#define LOG_INFO(_body_, ...)
#define LOG_WARN(_body_, ...)
#define LOG_ERROR(_body_, ...)
#endif
