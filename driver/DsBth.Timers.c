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

	const WDFDEVICE device = WdfTimerGetParentObject(Timer);
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(device);

	//
	// Wired always wins: if the same MAC is already present over USB, drop
	// this wireless instance before starting the input stream (issue #330).
	// 
	if (DsDevice_IsWiredInstancePresent(pDevCtx))
	{
		TraceInformation(
			TRACE_DSBTH,
			"Wired instance of %s is already present, disconnecting wireless",
			pDevCtx->DeviceAddressString
		);
		EventWriteYieldingToWiredInstance(pDevCtx->DeviceAddressString);

		if (!NT_SUCCESS(status = DsBth_SendDisconnectRequest(pDevCtx)))
		{
			TraceError(
				TRACE_DSBTH,
				"DsBth_SendDisconnectRequest failed with status %!STATUS!",
				status
			);
			EventWriteFailedWithNTStatus(__FUNCTION__, L"DsBth_SendDisconnectRequest", status);
		}

		FuncExitNoReturn(TRACE_DSBTH);
		return;
	}

	//
	// Feature 0x01 then Feature 0xEF page 0xA0, before the first output
	// and interrupt stream. Serialized under OutputReport.Lock so rumble
	// or LED writes cannot consume a control-channel reply. Soft-fail keeps
	// nominal motion calibration (issue #217).
	//
	WdfWaitLockAcquire(pDevCtx->OutputReport.Lock, NULL);

	DsBth_TryLoadIdentification(device);
	DsMotion_TryLoadBluetoothCalibration(device);

	//
	// Apply LEDs (mode-aware, authority-checked - fixes issue #351 for the
	// wireless startup path, which used to always use the single-LED
	// mapping and write regardless of authority), zero rumble strength,
	// then send - still under the same lock as the feature reads so the
	// first output can carry the EEPROM cal byte.
	//
	// The zero-strength call below also writes an explicit, finite
	// duration by itself now (DS3_PROCESS_RUMBLE_STRENGTH, issue #356), so
	// the two 0xFE duration writes that used to precede it - and that were
	// never restored afterwards, permanently time-capping every wireless
	// rumble for the rest of the session - are gone.
	//
	DsLed_ApplyLocked(pDevCtx);

	DS3_SET_BOTH_RUMBLE_STRENGTH(pDevCtx, 0x00, 0x00);

	status = DSHM_SendOutputReportUnlocked(pDevCtx, Ds3OutputReportSourceDriverHighPriority);

	WdfWaitLockRelease(pDevCtx->OutputReport.Lock);

	if (!NT_SUCCESS(status))
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
