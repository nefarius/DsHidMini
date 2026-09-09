#include "Driver.h"
#include "DsLed.tmh"


//
// Bit masks of the four physical LEDs, indexed 0 (LED 1 / lowest) to 3
// (LED 4 / highest). Mirrors DS3_LED_1..DS3_LED_4 from Ds3.h.
// 
static const UCHAR G_DsLedBits[4] = { DS3_LED_1, DS3_LED_2, DS3_LED_3, DS3_LED_4 };


//
// Returns TRUE if the driver is currently allowed to write LED state.
// See DsLed.h for the per-authority semantics.
// 
_Use_decl_annotations_
BOOLEAN
DsLed_IsDriverInCharge(
	_In_ PDEVICE_CONTEXT Context
)
{
	switch (Context->Configuration.LEDSettings.Authority)
	{
	case DsLEDAuthorityDriver:
		return TRUE;

	case DsLEDAuthorityApplication:
		return FALSE;

	case DsLEDAuthorityAutomatic:
	default:
		return Context->OutputReport.Mode == Ds3OutputReportModeDriverHandled;
	}
}

//
// Locked internal of DsLed_SetFlagsAndEffects; assumes
// Context->OutputReport.Lock is already held.
// 
static
UCHAR
DsLedClampFlagsForDevice(
	_In_ const PDEVICE_CONTEXT Context,
	_In_ UCHAR Flags
)
{
	if (Context->DeviceType != DsDeviceTypeNavigation)
	{
		return Flags;
	}

	//
	// Navigation has one physical LED. Preserve the explicit off marker,
	// otherwise keep only LED 1 when any player LED was requested.
	// 
	if (Flags == DS3_LED_OFF)
	{
		return DS3_LED_OFF;
	}

	return (Flags & (DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4))
		? DS3_LED_1
		: DS3_LED_OFF;
}

static
VOID
DsLedSetFlagsAndEffectsLocked(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR Flags,
	_In_ const DS_LED* Effect
)
{
	static const DS_LED noEffect = DS3_LED_EFFECT_NONE;

	Flags = DsLedClampFlagsForDevice(Context, Flags);

	DsLed_SetFlags(Context, Flags);

	for (UCHAR ledIndex = 0; ledIndex < 4; ledIndex++)
	{
		const DS_LED* pEffect = (Flags & G_DsLedBits[ledIndex]) ? Effect : &noEffect;

		DsLed_SetEffect(
			Context,
			ledIndex,
			pEffect->TotalDuration,
			pEffect->BasePortionDuration,
			pEffect->OffPortionMultiplier,
			pEffect->OnPortionMultiplier
		);
	}
}

//
// Sets the LED flags byte and normalizes all four effect blocks against it.
// Public, lock-taking entry point; see DsLed.h.
// 
_Use_decl_annotations_
VOID
DsLed_SetFlagsAndEffects(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR Flags,
	_In_ const DS_LED* Effect
)
{
	FuncEntry(TRACE_LED);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	DsLedSetFlagsAndEffectsLocked(Context, Flags, Effect);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExitNoReturn(TRACE_LED);
}

//
// Writes the configured custom LED pattern (flags + four per-LED effect
// blocks) into the output report buffer. Assumes
// Context->OutputReport.Lock is already held; called both from
// DsLed_Apply's DsLEDModeCustomPattern branch and from OutputReport.c's
// locked send-prep immediately before every send (issue #350), so a
// driver-owned custom pattern can never be silently overwritten by a
// rumble-only or HID application send.
// 
_Use_decl_annotations_
VOID
DsLed_ApplyCustomPatternLocked(
	_In_ PDEVICE_CONTEXT Context
)
{
	const PDS_LED_SETTINGS pLed = &Context->Configuration.LEDSettings;
	const UCHAR flags = DsLedClampFlagsForDevice(Context, pLed->CustomPatterns.LEDFlags);

	DsLed_SetFlags(Context, flags);

	DsLed_SetEffect(
		Context,
		0,
		pLed->CustomPatterns.Player1.TotalDuration,
		pLed->CustomPatterns.Player1.BasePortionDuration,
		pLed->CustomPatterns.Player1.OffPortionMultiplier,
		pLed->CustomPatterns.Player1.OnPortionMultiplier
	);

	if (Context->DeviceType == DsDeviceTypeNavigation)
	{
		DsLed_SetEffect(Context, 1, 0, 0, 0, 0);
		DsLed_SetEffect(Context, 2, 0, 0, 0, 0);
		DsLed_SetEffect(Context, 3, 0, 0, 0, 0);
		return;
	}

	DsLed_SetEffect(
		Context,
		1,
		pLed->CustomPatterns.Player2.TotalDuration,
		pLed->CustomPatterns.Player2.BasePortionDuration,
		pLed->CustomPatterns.Player2.OffPortionMultiplier,
		pLed->CustomPatterns.Player2.OnPortionMultiplier
	);
	DsLed_SetEffect(
		Context,
		2,
		pLed->CustomPatterns.Player3.TotalDuration,
		pLed->CustomPatterns.Player3.BasePortionDuration,
		pLed->CustomPatterns.Player3.OffPortionMultiplier,
		pLed->CustomPatterns.Player3.OnPortionMultiplier
	);
	DsLed_SetEffect(
		Context,
		3,
		pLed->CustomPatterns.Player4.TotalDuration,
		pLed->CustomPatterns.Player4.BasePortionDuration,
		pLed->CustomPatterns.Player4.OffPortionMultiplier,
		pLed->CustomPatterns.Player4.OnPortionMultiplier
	);
}

//
// Mode-aware battery-to-flags mapping for the two battery-indicator modes.
// Separate handling for DsLEDModeBatteryIndicatorPlayerIndex (single LED,
// 1-4) and DsLEDModeBatteryIndicatorBarGraph (fill 1 / 1-2 / 1-3 / 1-4), so
// bar-graph configurations never receive single-LED flags (issue #351).
// Assumes Context->OutputReport.Lock is already held.
// 
static
VOID
DsLedApplyBatteryIndicatorLocked(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BOOLEAN IsBarGraph
)
{
	static const DS_LED staticEffect = DS3_LED_EFFECT_STATIC;
	static const DS_LED slowFlashEffect = DS3_LED_EFFECT_SLOW_FLASH;
	static const DS_LED fastFlashEffect = DS3_LED_EFFECT_FAST_FLASH;

	UCHAR flags;
	const DS_LED* pEffect = &staticEffect;

	if (Context->DeviceType == DsDeviceTypeNavigation)
	{
		switch (Context->BatteryStatus)
		{
		case DsBatteryStatusNone:
		default:
			flags = DS3_LED_OFF;
			break;

		case DsBatteryStatusDying:
		case DsBatteryStatusLow:
			flags = DS3_LED_1;
			pEffect = &fastFlashEffect;
			break;

		case DsBatteryStatusCharging:
			flags = DS3_LED_1;
			pEffect = &slowFlashEffect;
			break;

		case DsBatteryStatusMedium:
		case DsBatteryStatusHigh:
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			flags = DS3_LED_1;
			break;
		}

		DsLedSetFlagsAndEffectsLocked(Context, flags, pEffect);
		return;
	}

	switch (Context->BatteryStatus)
	{
	case DsBatteryStatusNone:
	default:

		//
		// Unknown - all off, no effect matters since no flag bit is set.
		// 
		flags = DS3_LED_OFF;

		break;

	case DsBatteryStatusDying:
	case DsBatteryStatusLow:

		flags = DS3_LED_1;
		pEffect = &slowFlashEffect;

		break;

	case DsBatteryStatusMedium:

		flags = IsBarGraph ? (DS3_LED_1 | DS3_LED_2) : DS3_LED_2;

		break;

	case DsBatteryStatusHigh:

		flags = IsBarGraph ? (DS3_LED_1 | DS3_LED_2 | DS3_LED_3) : DS3_LED_3;

		break;

	case DsBatteryStatusCharged:
	case DsBatteryStatusFull:

		flags = IsBarGraph ? (DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4) : DS3_LED_4;

		break;

	case DsBatteryStatusCharging:

		//
		// Leave the flag byte to DsLed_AdvanceChargingAnimation, which owns
		// the cycling pattern while USB charging is in progress; only
		// normalize the effect blocks against whatever flags are currently
		// set, so a hot-reload mid-charge does not clobber the animation
		// (issue #349).
		// 
		flags = DsLed_GetFlags(Context);

		break;
	}

	DsLedSetFlagsAndEffectsLocked(Context, flags, pEffect);
}

//
// Locked internal of DsLed_Apply; assumes Context->OutputReport.Lock is
// already held. Also exposed publicly as DsLed_ApplyLocked; see DsLed.h.
// 
_Use_decl_annotations_
VOID
DsLed_ApplyLocked(
	_In_ PDEVICE_CONTEXT Context
)
{
	if (!DsLed_IsDriverInCharge(Context))
	{
		return;
	}

	switch (Context->Configuration.LEDSettings.Mode)
	{
	case DsLEDModeCustomPattern:

		DsLed_ApplyCustomPatternLocked(Context);

		break;

	case DsLEDModeBatteryIndicatorPlayerIndex:

		DsLedApplyBatteryIndicatorLocked(Context, FALSE);

		break;

	case DsLEDModeBatteryIndicatorBarGraph:

		DsLedApplyBatteryIndicatorLocked(Context, TRUE);

		break;

	default:

		break;
	}
}

//
// Recomputes LED flags/effects, does not send. Public, lock-taking entry
// point; see DsLed.h.
// 
_Use_decl_annotations_
VOID
DsLed_Apply(
	_In_ PDEVICE_CONTEXT Context
)
{
	FuncEntry(TRACE_LED);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	DsLed_ApplyLocked(Context);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExitNoReturn(TRACE_LED);
}

//
// Recomputes LED flags/effects and sends the result, all under one hold of
// Context->OutputReport.Lock. Public, lock-taking entry point; see DsLed.h.
// 
_Use_decl_annotations_
NTSTATUS
DsLed_Refresh(
	_In_ PDEVICE_CONTEXT Context,
	_In_ DS_OUTPUT_REPORT_SOURCE Source
)
{
	FuncEntry(TRACE_LED);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	DsLed_ApplyLocked(Context);

	//
	// Match the no-op contract documented in DsLed.h: if the driver isn't
	// in charge (e.g. Application authority), DsLed_ApplyLocked above was
	// already a no-op, and sending here would just push out whatever
	// application-owned report is already in the buffer, unrequested.
	// 
	const NTSTATUS status = DsLed_IsDriverInCharge(Context)
		? DSHM_SendOutputReportUnlocked(Context, Source)
		: STATUS_SUCCESS;

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_LED, "status=%!STATUS!", status);

	return status;
}

//
// Advances the USB charging-cycle LED animation by one step and sends the
// result. Public, lock-taking entry point; see DsLed.h.
// 
_Use_decl_annotations_
VOID
DsLed_AdvanceChargingAnimation(
	_In_ PDEVICE_CONTEXT Context
)
{
	FuncEntry(TRACE_LED);

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	do
	{
		if (!DsLed_IsDriverInCharge(Context))
		{
			break;
		}

		const DS_LED_MODE mode = Context->Configuration.LEDSettings.Mode;

		if (mode != DsLEDModeBatteryIndicatorPlayerIndex
			&& mode != DsLEDModeBatteryIndicatorBarGraph)
		{
			break;
		}

		//
		// Navigation charging is a self-sustaining slow flash applied once
		// on status change. Re-sending here would restart the pad's flash
		// cycle. USB input no longer ticks this function for Navigation;
		// keep the branch as a defensive no-send.
		// 
		if (Context->DeviceType == DsDeviceTypeNavigation)
		{
			break;
		}

		UCHAR led = DsLed_GetFlags(Context);

		if (mode == DsLEDModeBatteryIndicatorPlayerIndex)
		{
			//
			// Cycle through single LEDs 1 -> 4 and repeat
			// 
			led <<= 1;

			if (led > DS3_LED_4 || led < DS3_LED_1)
			{
				led = DS3_LED_1;
			}
		}
		else
		{
			//
			// Cycle bar-graph fill 1 -> 1-4 and repeat
			// 
			if (led & 0xF0)
			{
				led = DS3_LED_1;
			}
			else
			{
				led |= (!led) ? DS3_LED_1 : led << 1;
			}
		}

		static const DS_LED staticEffect = DS3_LED_EFFECT_STATIC;

		DsLedSetFlagsAndEffectsLocked(Context, led, &staticEffect);

		(void)DSHM_SendOutputReportUnlocked(Context, Ds3OutputReportSourceDriverLowPriority);

	} while (FALSE);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExitNoReturn(TRACE_LED);
}

//
// Console / XInput-style player-index to LED flags. Indices 5-7 light the
// extra combinations the hardware uses once four physical LEDs are exhausted.
// 
_Use_decl_annotations_
BOOLEAN
DsLed_PlayerIndexToFlags(
	_In_ BYTE PlayerIndex,
	_Out_ PUCHAR Flags
)
{
	static const UCHAR map[] =
	{
		0x00,
		DS3_LED_1,
		DS3_LED_2,
		DS3_LED_3,
		DS3_LED_4,
		DS3_LED_1 | DS3_LED_4,
		DS3_LED_2 | DS3_LED_4,
		DS3_LED_3 | DS3_LED_4
	};

	if (PlayerIndex < 1 || PlayerIndex > 7)
	{
		*Flags = 0x00;
		return FALSE;
	}

	*Flags = map[PlayerIndex];
	return TRUE;
}

//
// IPC player-index write: hand Automatic authority to the application so
// DsLed_Refresh cannot immediately overwrite the requested slot, then
// mutate and enqueue under one hold of OutputReport.Lock.
// 
_Use_decl_annotations_
NTSTATUS
DsLed_ApplyIpcPlayerIndex(
	_In_ PDEVICE_CONTEXT Context,
	_In_ BYTE PlayerIndex
)
{
	FuncEntry(TRACE_LED);

	UCHAR flags;
	NTSTATUS status = STATUS_INVALID_PARAMETER;

	if (!DsLed_PlayerIndexToFlags(PlayerIndex, &flags))
	{
		FuncExit(TRACE_LED, "status=%!STATUS!", status);
		return status;
	}

	if (Context->Configuration.LEDSettings.Authority == DsLEDAuthorityDriver)
	{
		status = STATUS_ACCESS_DENIED;
		FuncExit(TRACE_LED, "status=%!STATUS!", status);
		return status;
	}

	static const DS_LED staticEffect = DS3_LED_EFFECT_STATIC;

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	Context->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;
	DsLedSetFlagsAndEffectsLocked(Context, flags, &staticEffect);
	status = DSHM_SendOutputReportUnlocked(Context, Ds3OutputReportSourceIpc);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_LED, "status=%!STATUS!", status);

	return status;
}

//
// IPC LED-pattern write: same authority hand-off as player-index, but
// applies four independent effect blocks like a custom pattern.
// 
_Use_decl_annotations_
BOOLEAN
DsLed_AreIpcFlagsValid(
	_In_ UCHAR Flags
)
{
	const UCHAR validMask = DS3_LED_1 | DS3_LED_2 | DS3_LED_3 | DS3_LED_4 | DS3_LED_OFF;

	return (Flags & ~validMask) == 0;
}

_Use_decl_annotations_
NTSTATUS
DsLed_ApplyIpcPattern(
	_In_ PDEVICE_CONTEXT Context,
	_In_ UCHAR Flags,
	_In_ const DS_LED Effects[4]
)
{
	FuncEntry(TRACE_LED);

	NTSTATUS status = STATUS_INVALID_PARAMETER;

	if (!DsLed_AreIpcFlagsValid(Flags))
	{
		FuncExit(TRACE_LED, "status=%!STATUS!", status);
		return status;
	}

	if (Context->Configuration.LEDSettings.Authority == DsLEDAuthorityDriver)
	{
		status = STATUS_ACCESS_DENIED;
		FuncExit(TRACE_LED, "status=%!STATUS!", status);
		return status;
	}

	WdfWaitLockAcquire(Context->OutputReport.Lock, NULL);

	Context->OutputReport.Mode = Ds3OutputReportModeWriteReportPassThrough;

	const UCHAR clamped = DsLedClampFlagsForDevice(Context, Flags);

	DsLed_SetFlags(Context, clamped);

	DsLed_SetEffect(
		Context,
		0,
		Effects[0].TotalDuration,
		Effects[0].BasePortionDuration,
		Effects[0].OffPortionMultiplier,
		Effects[0].OnPortionMultiplier
	);

	if (Context->DeviceType == DsDeviceTypeNavigation)
	{
		DsLed_SetEffect(Context, 1, 0, 0, 0, 0);
		DsLed_SetEffect(Context, 2, 0, 0, 0, 0);
		DsLed_SetEffect(Context, 3, 0, 0, 0, 0);
	}
	else
	{
		DsLed_SetEffect(
			Context,
			1,
			Effects[1].TotalDuration,
			Effects[1].BasePortionDuration,
			Effects[1].OffPortionMultiplier,
			Effects[1].OnPortionMultiplier
		);
		DsLed_SetEffect(
			Context,
			2,
			Effects[2].TotalDuration,
			Effects[2].BasePortionDuration,
			Effects[2].OffPortionMultiplier,
			Effects[2].OnPortionMultiplier
		);
		DsLed_SetEffect(
			Context,
			3,
			Effects[3].TotalDuration,
			Effects[3].BasePortionDuration,
			Effects[3].OffPortionMultiplier,
			Effects[3].OnPortionMultiplier
		);
	}

	status = DSHM_SendOutputReportUnlocked(Context, Ds3OutputReportSourceIpc);

	WdfWaitLockRelease(Context->OutputReport.Lock);

	FuncExit(TRACE_LED, "status=%!STATUS!", status);

	return status;
}
