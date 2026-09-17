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
	// No known Bluetooth host - not the PS3, not Linux hid-sony, not the
	// USB Host Shield library - ever issues GET_REPORT for Feature 0x01 or
	// 0xEF over the HID control channel; the PS3 itself only reads them
	// over USB, while pairing (see docs/PS3_USB_STARTUP.md). So instead of
	// asking the pad, read back whatever its USB instance cached under
	// DEVPKEY_DsHidMini_RO_IdentificationData / _MotionCalibrationData for
	// this same Bluetooth MAC. A cache miss (pad never seen over USB) keeps
	// nominal 512/399 calibration with no wire traffic and no startup
	// delay. Serialized under OutputReport.Lock, same as before, so this
	// and the F4 motion-enable write below cannot race the first output
	// (issue #217).
	//
	WdfWaitLockAcquire(pDevCtx->OutputReport.Lock, NULL);

	DsIdentification_Clear(device);

	{
		UCHAR identification[DS_IDENTIFICATION_REPORT_SIZE];
		UCHAR calibration[CONTROL_TRANSFER_BUFFER_LENGTH];
		ULONG identificationLength = 0;
		ULONG calibrationLength = 0;
		const BOOLEAN cacheFound = DsDevice_ReadCachedWiredProperties(
			pDevCtx,
			identification,
			sizeof(identification),
			&identificationLength,
			calibration,
			sizeof(calibration),
			&calibrationLength
		);

		if (identificationLength > 0)
		{
			DsIdentification_PublishFromReport(device, identification, identificationLength);
		}

		if (calibrationLength > 0)
		{
			if (!DsMotion_LoadCalibrationBuffer(
				device,
				calibration,
				calibrationLength,
				DsMotionCalibrationSourceCachedFromUsb
			))
			{
				TraceWarning(
					TRACE_DSBTH,
					"Cached EEPROM page 0xA0 for %s failed to parse; using nominal calibration",
					pDevCtx->DeviceAddressString
				);
			}
		}
		else
		{
			DsMotion_OnBluetoothCacheMiss(device);
		}

		TraceInformation(
			TRACE_DSBTH,
			"%s cached calibration for %s (identification=%lu bytes, EEPROM=%lu bytes)",
			cacheFound ? "Found" : "No",
			pDevCtx->DeviceAddressString,
			identificationLength,
			calibrationLength
		);
	}

	//
	// Gyro-enable experiment (issue #217): raw gyro reads ~5 on some pads
	// over Bluetooth until this is sent. Gated on cached identification so
	// it never reaches a pad we cannot recognize, and skipped for the
	// clone heuristic since those pads' gyros are known frozen regardless.
	// Swap DS3_BTH_MOTION_ENABLE_PAYLOAD_USB for _LINUX here to compare the
	// two candidates on hardware; see driver/Ds3.h.
	// 
	if (pDevCtx->IdentificationPresent && !pDevCtx->Identification.CloneHeuristic)
	{
		static const UCHAR motionEnable[] = { DS3_BTH_MOTION_ENABLE_PAYLOAD_USB };

		if (!NT_SUCCESS(status = DsBth_HidControlSetFeature(
			pDevCtx,
			0xF4,
			motionEnable,
			ARRAYSIZE(motionEnable)
		)))
		{
			TraceWarning(
				TRACE_DSBTH,
				"SET Feature 0xF4 motion-enable failed with %!STATUS!",
				status
			);
		}
	}

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
