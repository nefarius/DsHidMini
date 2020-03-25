#include "Driver.h"
#include "DsBth.tmh"
#include <bluetoothapis.h>
#include <bthioctl.h>

NTSTATUS DsBth_SendDisconnectRequestAsync(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;
	WDF_OBJECT_ATTRIBUTES attribs;
	WDFMEMORY memory = NULL;
	WDFREQUEST request;
	BLUETOOTH_ADDRESS* pPayload;

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);

	//
	// Create new request (this is fired infrequently so no reuse)
	// 
	status = WdfRequestCreate(&attribs,
		Context->Connection.Bth.BthIoTarget,
		&request);
	if (!NT_SUCCESS(status)) {
		return status;
	}

	//
	// Associate payload with request
	// 
	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = request;

	//
	// Create payload memory
	// 
	status = WdfMemoryCreate(&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		sizeof(BLUETOOTH_ADDRESS_STRUCT),
		&memory,
		(PVOID)&pPayload);
	if (!NT_SUCCESS(status)) {
		return status;
	}

	//
	// TODO: currently fails with STATUS_DEVICE_NOT_CONNECTED (0xC000009D)
	//
	pPayload->ullLong = *(PULONGLONG)&Context->DeviceAddress;

	//
	// Format async request
	// 
	status = WdfIoTargetFormatRequestForIoctl(
		Context->Connection.Bth.BthIoTarget,
		request,
		IOCTL_BTH_DISCONNECT_DEVICE,
		memory,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status)) {
		return status;
	}

	//
	// Assign a fixed completion routine
	// 
	WdfRequestSetCompletionRoutine(
		request,
		DsBth_SendDisconnectRequestCompletionRoutine,
		NULL
	);

	//
	// Send it
	// 
	if (WdfRequestSend(request,
		Context->Connection.Bth.BthIoTarget,
		NULL) == FALSE)
	{
		return WdfRequestGetStatus(request);
	}

	return status;
}

void DsBth_SendDisconnectRequestCompletionRoutine(
	WDFREQUEST Request,
	WDFIOTARGET Target,
	PWDF_REQUEST_COMPLETION_PARAMS Params,
	WDFCONTEXT Context
)
{
	UNREFERENCED_PARAMETER(Target);
	UNREFERENCED_PARAMETER(Params);
	UNREFERENCED_PARAMETER(Context);

	TraceEvents(TRACE_LEVEL_ERROR,
		TRACE_DSBTH,
		"%!FUNC! finished with status %!STATUS!",
		WdfRequestGetStatus(Request)
	);

	WdfObjectDelete(Request);
}
