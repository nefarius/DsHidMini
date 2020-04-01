#include "Driver.h"
#include "DsBth.tmh"
#include <bluetoothapis.h>
#include <bthioctl.h>

NTSTATUS DsBth_SendDisconnectRequest(PDEVICE_CONTEXT Context)
{
	BLUETOOTH_ADDRESS address;
	WDF_MEMORY_DESCRIPTOR memDesc;
	UCHAR buffer[sizeof(BLUETOOTH_ADDRESS)];

	RtlZeroMemory(buffer, sizeof(BLUETOOTH_ADDRESS));
	RtlCopyMemory(buffer, &Context->DeviceAddress, sizeof(Context->DeviceAddress));

	address.ullLong = *(PULONGLONG)&buffer[0];

	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(
		&memDesc,
		&address,
		sizeof(BLUETOOTH_ADDRESS)
	);

	//
	// Send disconnect request
	// 
	return WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		NULL, // use internal request object
		IOCTL_BTH_DISCONNECT_DEVICE,
		&memDesc, // holds address to disconnect
		NULL,
		NULL,
		NULL
	);
}

_Use_decl_annotations_
VOID
DsBth_EvtControlReadTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS					status;
	PDEVICE_CONTEXT				pDevCtx;
	WDF_MEMORY_DESCRIPTOR		memDesc;
	size_t						bufferLength = 0;
	WDF_REQUEST_REUSE_PARAMS    params;

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSBTH, "%!FUNC! Exit");

	pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(&memDesc,
		pDevCtx->Connection.Bth.HidControl.ReadMemory,
		NULL);

	status = WdfIoTargetSendIoctlSynchronously(
		pDevCtx->Connection.Bth.BthIoTarget,
		pDevCtx->Connection.Bth.HidControl.ReadRequest,
		IOCTL_BTHPS3_HID_CONTROL_READ,
		NULL,
		&memDesc,
		NULL,
		&bufferLength
	);

	if (status == STATUS_DEVICE_NOT_CONNECTED)
	{
		goto Exit;
	}

	if (!NT_SUCCESS(status)) {
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
	}

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(
		pDevCtx->Connection.Bth.HidControl.ReadRequest,
		&params
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
		goto Exit;
	}

	WdfTimerStart(
		pDevCtx->Connection.Bth.Timers.HidControlConsume,
		WDF_REL_TIMEOUT_IN_MS(0x0A)
	);

Exit:

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSBTH, "%!FUNC! Exit");
}
