#pragma once

//
// Single policy module for every LED write in the driver (issue #351).
// 
// All LED state changes - authority checks, the battery-to-flags mapping and
// the custom-pattern override - are decided here and nowhere else. Callers
// that used to re-decide authority and duplicate the battery mapping now
// call into this module instead; see driver/Ds3.c for the byte-level
// primitives this module builds on (DsLed_SetFlags, DsLed_GetFlags,
// DsLed_SetEffect).
// 

//
// Named effect blocks (TotalDuration, BasePortionDuration,
// OffPortionMultiplier, OnPortionMultiplier - see DS_LED in DsCommon.h),
// replacing the scattered magic numbers that used to live in three
// different files.
// 

//
// PS3-correct static effect: lasts forever, no flashing. See issue #365;
// ControlApp's DshmTranslationUtils.ApplyCustomLedPatterns uses the same
// values for its non-custom defaults.
// 
#define DS3_LED_EFFECT_STATIC       { 0xFF, 0x0001, 0x00, 0x01 }

//
// Slow flash, used for Low/Dying battery levels. Same values as the
// (0xFF, 15, 127, 127) magic numbers previously repeated in
// DsBth.Timers.c, DsHidMiniDrv.c and HID.Reports.c.
// 
#define DS3_LED_EFFECT_SLOW_FLASH   { 0xFF, 0x000F, 0x7F, 0x7F }

//
// Fast flash, used by the DS4Windows high-latency warning indicator. Same
// values as the (0xFF, 3, 127, 127) magic numbers previously repeated four
// times in HID.Reports.c.
// 
#define DS3_LED_EFFECT_FAST_FLASH   { 0xFF, 0x0003, 0x7F, 0x7F }

//
// All-zero effect block, applied to every LED whose flag bit is not set.
// 
#define DS3_LED_EFFECT_NONE         { 0x00, 0x0000, 0x00, 0x00 }

//
// Returns TRUE if the driver is currently allowed to write LED state.
// 
//  - DsLEDAuthorityDriver:      always TRUE
//  - DsLEDAuthorityApplication: always FALSE (issue #351)
//  - DsLEDAuthorityAutomatic:   TRUE only while OutputReport.Mode is still
//                               Ds3OutputReportModeDriverHandled, i.e. before
//                               any application has written its own report
// 
BOOLEAN
DsLed_IsDriverInCharge(
	_In_ PDEVICE_CONTEXT Context
);

//
// Recomputes LED flags/effects from the current configuration and battery
// status and writes them into the output report buffer. Does not send
// anything. No-op unless DsLed_IsDriverInCharge. Takes Context->OutputReport.Lock.
// 
VOID
DsLed_Apply(
	_In_ PDEVICE_CONTEXT Context
);

//
// Same as DsLed_Apply, but assumes Context->OutputReport.Lock is already
// held by the caller. Exposed for callers that must apply LEDs, touch other
// buffer state (e.g. rumble) and send, all under one hold of the lock - see
// DsBth_EvtStartupDelayTimerFunc, which follows exactly that sequence
// (lock, apply LEDs, set rumble, DSHM_SendOutputReportUnlocked, unlock) to
// fix issue #351 for the wireless startup path without ever dropping the
// lock between mutation and copy.
// 
VOID
DsLed_ApplyLocked(
	_In_ PDEVICE_CONTEXT Context
);

//
// Same as DsLed_Apply, immediately followed by a send, all under one hold
// of Context->OutputReport.Lock (so nothing can race the recompute with a
// send that copies a half-updated buffer). No-op (returns STATUS_SUCCESS
// without sending) unless DsLed_IsDriverInCharge.
// 
NTSTATUS
DsLed_Refresh(
	_In_ PDEVICE_CONTEXT Context,
	_In_ DS_OUTPUT_REPORT_SOURCE Source
);

//
// Advances the USB charging-cycle LED animation by one step and sends the
// result. No-op unless DsLed_IsDriverInCharge and LEDSettings.Mode is one of
// the two battery-indicator modes. Takes Context->OutputReport.Lock.
// 
VOID
DsLed_AdvanceChargingAnimation(
	_In_ PDEVICE_CONTEXT Context
);

//
// Sets the LED flags byte and, for each of the four LEDs, either Effect (if
// its flag bit is set in Flags) or an all-zero effect block (if it is not).
// Unlike DsLed_Apply/DsLed_Refresh this is callable even when the driver is
// not in charge - it is the primitive HID application report handlers
// (SIXAXIS pass-through, DS4Windows emulation) use to write LED state
// directly, so it carries no authority guard of its own. Takes
// Context->OutputReport.Lock.
// 
VOID
DsLed_SetFlagsAndEffects(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR Flags,
	_In_ const DS_LED* Effect
);

//
// Same as DsLed_SetFlagsAndEffects, but assumes Context->OutputReport.Lock
// is already held by the caller. Exposed so OutputReport.c's locked
// send-prep can re-apply a driver-owned custom pattern immediately before
// the report copy, without releasing and re-acquiring the lock (issue
// #350: rumble-only/HID sends must not be able to drop a driver-owned
// custom pattern).
// 
VOID
DsLed_ApplyCustomPatternLocked(
	_In_ PDEVICE_CONTEXT Context
);

//
// Maps a player index (1-7) to the DS3 LED flags byte used by the console
// and by XInput-style extra-player encodings (5 = 1+4, 6 = 2+4, 7 = 3+4).
// Returns FALSE when PlayerIndex is outside 1-7; Flags is then set to 0.
// 
BOOLEAN
DsLed_PlayerIndexToFlags(
	_In_ BYTE PlayerIndex,
	_Out_ PUCHAR Flags
);

//
// Applies an IPC player-index override: marks LEDs application-owned for
// Automatic authority, writes the matching static LED pattern, and sends
// the report under one hold of Context->OutputReport.Lock. Returns
// STATUS_INVALID_PARAMETER for an out-of-range index and STATUS_ACCESS_DENIED
// when LED authority is Driver (configuration owns the indicators).
// 
NTSTATUS
DsLed_ApplyIpcPlayerIndex(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BYTE PlayerIndex
);
