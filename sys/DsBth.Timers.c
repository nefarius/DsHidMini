#include "Driver.h"
#include "DsBth.Timers.tmh"

//
// Called once delayed after power-up
// 
_Use_decl_annotations_
VOID
DsBth_EvtStartupDelayTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS status;

	FuncEntry(TRACE_DSBTH);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	//
	// We have not yet received an input report from the remote device
	// 
	if (pDevCtx->BatteryStatus == DsBatteryStatusNone)
	{
		DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_OFF);
	}
	else
	{
		switch (pDevCtx->BatteryStatus)
		{
		case DsBatteryStatusCharged:
		case DsBatteryStatusFull:
			DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_4);
			break;
		case DsBatteryStatusHigh:
			DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_3);
			break;
		case DsBatteryStatusMedium:
			DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_2);
			break;
		case DsBatteryStatusLow:
		case DsBatteryStatusDying:
			DS3_SET_LED_FLAGS(pDevCtx, DS3_LED_1);
			DS3_SET_LED_DURATION(pDevCtx, 0, 0xFF, 15, 127, 127);
			break;
		default:
			break;
		}
	}
		
	DS3_SET_SMALL_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_LARGE_RUMBLE_DURATION(pDevCtx, 0xFE);
	DS3_SET_BOTH_RUMBLE_STRENGTH(pDevCtx, 0x00, 0x00);

	if (!NT_SUCCESS(status = DSHM_SendOutputReport(pDevCtx, Ds3OutputReportSourceDriverHighPriority)))
	{
		TraceError(
			TRACE_DSBTH,
			"Ds_SendOutputReport failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"Ds_SendOutputReport", status);
	}
	
	//
	// Start consuming input packets
	// 
	if (!NT_SUCCESS(status = DMF_DefaultTarget_StreamStart(pDevCtx->Connection.Bth.HidInterrupt.InputStreamerModule)))
	{
		TraceError(
			TRACE_DSBTH,
			"DMF_DefaultTarget_StreamStart failed with status %!STATUS!",
			status
		);
		EventWriteFailedWithNTStatus(__FUNCTION__, L"DMF_DefaultTarget_StreamStart", status);
	}

	//
	// Send delayed initialization packets
	// Required for compatibility with some SIXAXIS models
	// 
	WdfTimerStart(
		pDevCtx->Connection.Bth.Timers.PostStartupTasks,
		WDF_REL_TIMEOUT_IN_SEC(1)
	);
	
	FuncExitNoReturn(TRACE_DSBTH);
}

//
// Invoked after startup delay to apply workarounds
// 
_Use_decl_annotations_
VOID
DsBth_EvtPostStartupTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS status;

	FuncEntry(TRACE_DSBTH);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	//
	// We have not yet received an input report from the remote device
	// 
	if (pDevCtx->BatteryStatus == DsBatteryStatusNone)
	{
		TraceWarning(
			TRACE_DSBTH,
			"Battery status still unknown, applying workarounds"
		);
		EventWriteApplyingWirelessWorkarounds();

		//
		// Send magic packet, starts input report sending
		// NOTE: this is only required on certain models like the OG SIXAXIS
		// It must not be issued on e.g. the Defender BTH or it disconnects
		// 
		if (!NT_SUCCESS(status = DsBth_Ds3SixaxisInit(pDevCtx)))
		{
			TraceError(
				TRACE_DSBTH,
				"DsBth_Ds3Init failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_Ds3Init", status);
		} 
	}

	FuncExitNoReturn(TRACE_DSBTH);
}
