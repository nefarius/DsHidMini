#include "Driver.h"
#include "Power.tmh"


//
// Start Bluetooth communication here
// 
#pragma code_seg("PAGED")
_Use_decl_annotations_
NTSTATUS
DsHidMini_EvtWdfDeviceSelfManagedIoInit(
	WDFDEVICE Device
)
{
	NTSTATUS status = STATUS_SUCCESS;

	PAGED_CODE();

	FuncEntry(TRACE_POWER);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		status = DsBth_SelfManagedIoInit(Device);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}
#pragma code_seg()

//
// Stop Bluetooth communication here
// 
NTSTATUS
DsHidMini_EvtWdfDeviceSelfManagedIoSuspend(
	WDFDEVICE Device
)
{
	NTSTATUS status = STATUS_SUCCESS;
	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_POWER);

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		status = DsBth_SelfManagedIoSuspend(Device);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Initialize USB communication here
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_EvtDevicePrepareHardware(
	WDFDEVICE  Device,
	WDFCMRESLIST  ResourcesRaw,
	WDFCMRESLIST  ResourcesTranslated
)
{
	NTSTATUS status = STATUS_SUCCESS;

	UNREFERENCED_PARAMETER(ResourcesRaw);
	UNREFERENCED_PARAMETER(ResourcesTranslated);

	FuncEntry(TRACE_POWER);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	//
	// Initialize USB
	// 
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		status = DsUsb_PrepareHardware(Device);

		if (NT_SUCCESS(status))
		{
			DsDevice_RegisterHotReloadListener(pDevCtx);
		}
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Tear down USB communication here
//
// NOTE: this runs both on a normal removal path (after D0Exit) and, crucially,
// on the surprise-removal path that WDF starts when EvtDeviceD0Entry fails while
// resuming from a low-power state (see issue #311). D0Exit is *not* called in
// that case, so anything that must always happen before the DMF Module
// collection is closed (in particular before [VirtualHidMini] closes) has to
// live here instead, and must be safe to call even if the corresponding
// PrepareHardware/D0Entry step never ran or already failed.
// 
_Use_decl_annotations_
NTSTATUS
DsHidMini_EvtDeviceReleaseHardware(
	WDFDEVICE Device,
	WDFCMRESLIST ResourcesTranslated
)
{
	const NTSTATUS status = STATUS_SUCCESS;

	UNREFERENCED_PARAMETER(ResourcesTranslated);

	FuncEntry(TRACE_POWER);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	//
	// Stop delivering input reports before any DMF Module gets a chance to
	// close. Idempotent: if D0Exit already stopped this target (normal
	// removal path) this is a no-op; if D0Entry failed before D0Exit could
	// run (issue #311), this is what prevents DsUsb_EvtUsbInterruptPipeReadComplete
	// from firing against an already-closed [VirtualHidMini] Module.
	//
	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb
		&& pDevCtx->Connection.Usb.InterruptInPipe != NULL)
	{
		TraceVerbose(
			TRACE_POWER,
			"Stopping USB interrupt reader from ReleaseHardware"
		);

		WdfIoTargetStop(
			WdfUsbTargetPipeGetIoTarget(pDevCtx->Connection.Usb.InterruptInPipe),
			WdfIoTargetCancelSentIo
		);
	}

	//
	// Moved here from D0Exit: D0Exit is skipped by WDF whenever D0Entry
	// returns a failure status, which used to leak this handle/event pair
	// on every failed resume attempt.
	//
	if (pDevCtx->ConfigurationDirectoryWatcherWaitHandle)
	{
		//
		// INVALID_HANDLE_VALUE makes this block until any callback that is
		// already running (DsDevice_HotReloadEventCallback) has completed,
		// so the handles below can't be freed out from under a callback
		// that is still in flight. Plain UnregisterWait()/UnregisterWaitEx()
		// with a NULL completion event does not guarantee that.
		// 
		UnregisterWaitEx(pDevCtx->ConfigurationDirectoryWatcherWaitHandle, INVALID_HANDLE_VALUE);
		pDevCtx->ConfigurationDirectoryWatcherWaitHandle = NULL;
	}

	if (pDevCtx->ConfigurationDirectoryWatcherEvent)
	{
		//
		// This handle comes from FindFirstChangeNotification(A), which must
		// be paired with FindCloseChangeNotification, not CloseHandle.
		// 
		FindCloseChangeNotification(pDevCtx->ConfigurationDirectoryWatcherEvent);
		pDevCtx->ConfigurationDirectoryWatcherEvent = NULL;
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Power up
// 
NTSTATUS DsHidMini_EvtDeviceD0Entry(
	_In_ WDFDEVICE              Device,
	_In_ WDF_POWER_DEVICE_STATE PreviousState
)
{
	NTSTATUS status = STATUS_SUCCESS;

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	FuncEntry(TRACE_POWER);

	//
	// Re-arm the "first dropped input report" trace latch for this power
	// cycle (see DSHM_ProcessHidInputReport).
	// 
	pDevCtx->InputReportDropLogged = FALSE;

	//
	// Re-arm the HID mode mismatch restart latch for this power cycle (see
	// DMF_DsHidMini_Open, issue #374).
	// 
	pDevCtx->HidModeRestartRequested = FALSE;

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		status = DsUsb_D0Entry(Device, PreviousState);
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		status = DsBth_D0Entry(Device, PreviousState);
	}

	//
	// Only start processing received output report packets if the
	// connection-specific power-up succeeded; starting it on a failed
	// D0Entry would leave the worker running against a device that is
	// about to be surprise-removed.
	//
	if (NT_SUCCESS(status))
	{
		DMF_ThreadedBufferQueue_Start(pDevCtx->OutputReport.Worker);
	}
	
	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}

//
// Power down
// 
NTSTATUS DsHidMini_EvtDeviceD0Exit(
	_In_ WDFDEVICE Device,
	_In_ WDF_POWER_DEVICE_STATE TargetState
)
{
	NTSTATUS status = STATUS_SUCCESS;

	UNREFERENCED_PARAMETER(TargetState);

	FuncEntry(TRACE_POWER);

	const PDEVICE_CONTEXT pDevCtx = DeviceGetContext(Device);

	//
	// Stop processing received output report packets
	//
	DMF_ThreadedBufferQueue_Stop(pDevCtx->OutputReport.Worker);

	//
	// NOTE: ConfigurationDirectoryWatcher handle/event teardown lives in
	// DsHidMini_EvtDeviceReleaseHardware now, because this callback is not
	// invoked by WDF when EvtDeviceD0Entry fails (see issue #311), which used
	// to leak them.
	//

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeUsb)
	{
		status = DsUdb_D0Exit(Device);
	}

	if (pDevCtx->ConnectionType == DsDeviceConnectionTypeBth)
	{
		WdfIoTargetPurge(
			pDevCtx->Connection.Bth.HidInterrupt.InputStreamerIoTarget,
			WdfIoTargetPurgeIoAndWait
		);
		WdfIoTargetPurge(
			pDevCtx->Connection.Bth.HidControl.OutputWriterIoTarget,
			WdfIoTargetPurgeIoAndWait
		);
	}

	FuncExit(TRACE_POWER, "status=%!STATUS!", status);

	return status;
}
