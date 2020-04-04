#include "Driver.h"
#include "DsBth.tmh"
#include <bluetoothapis.h>
#include <bthioctl.h>


//
// Initialize objects required for BTH communication
// 
NTSTATUS DsHidMini_BthConnectionContextInit(
	WDFDEVICE Device
)
{
	NTSTATUS				status = STATUS_SUCCESS;
	PDEVICE_CONTEXT			pDeviceContext;
	WDF_OBJECT_ATTRIBUTES	attribs;
	PUCHAR					outBuffer;
	WDF_TIMER_CONFIG		timerCfg;

	PAGED_CODE();

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSBTH, "%!FUNC! Entry");

	pDeviceContext = DeviceGetContext(Device);

#pragma region HID Interrupt Read

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidInterrupt.ReadRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BTHPS3_SIXAXIS_HID_INPUT_REPORT_SIZE,
		&pDeviceContext->Connection.Bth.HidInterrupt.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

#pragma region HID Control Write

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidControl.WriteRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidControl.WriteRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		BTHPS3_SIXAXIS_HID_OUTPUT_REPORT_SIZE,
		&pDeviceContext->Connection.Bth.HidControl.WriteMemory,
		(PVOID)&outBuffer
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}
	else
	{
		//
		// Initialize output report
		// 
		RtlCopyMemory(outBuffer, G_Ds3BthHidOutputReport, DS3_BTH_HID_OUTPUT_REPORT_SIZE);

		//
		// Turn flashing LEDs off
		// 
		DS3_BTH_SET_LED(outBuffer, DS3_LED_OFF);
	}

#pragma endregion

#pragma region HID Control Read

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	status = WdfRequestCreate(
		&attribs,
		pDeviceContext->Connection.Bth.BthIoTarget,
		&pDeviceContext->Connection.Bth.HidControl.ReadRequest
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = pDeviceContext->Connection.Bth.HidControl.ReadRequest;

	status = WdfMemoryCreate(
		&attribs,
		NonPagedPoolNx,
		DS3_POOL_TAG,
		0x0A, // TODO: introduce const
		&pDeviceContext->Connection.Bth.HidControl.ReadMemory,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfMemoryCreate failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

#pragma region Timers

	//
	// Control consume
	// 

	WDF_OBJECT_ATTRIBUTES_INIT(&attribs);
	attribs.ParentObject = Device;

	WDF_TIMER_CONFIG_INIT(
		&timerCfg,
		DsBth_EvtControlReadTimerFunc
	);

	status = WdfTimerCreate(
		&timerCfg,
		&attribs,
		&pDeviceContext->Connection.Bth.Timers.HidControlConsume
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfTimerCreate (HidControlConsume) failed with status %!STATUS!",
			status
		);
		return status;
	}

	WdfTimerStart(
		pDeviceContext->Connection.Bth.Timers.HidControlConsume,
		WDF_REL_TIMEOUT_IN_MS(0x64) // TODO: introduce const
	);

	//
	// Output Report Delay
	// 

	WDF_TIMER_CONFIG_INIT(
		&timerCfg,
		DsBth_EvtControlWriteTimerFunc
	);

	status = WdfTimerCreate(
		&timerCfg,
		&attribs,
		&pDeviceContext->Connection.Bth.Timers.HidOutputReport
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfTimerCreate (HidOutputReport) failed with status %!STATUS!",
			status
		);
		return status;
	}

#pragma endregion

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSBTH, "%!FUNC! Exit");

	return status;
}

//
// Send HID Control OUT Request (TODO: unused)
// 
NTSTATUS DsBth_SendHidControlWriteRequest(PDEVICE_CONTEXT Context)
{
	NTSTATUS					status;
	WDF_MEMORY_DESCRIPTOR		memDesc;
	WDF_REQUEST_REUSE_PARAMS    params;

	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(
		&memDesc,
		Context->Connection.Bth.HidControl.WriteMemory,
		NULL
	);

	status = WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		Context->Connection.Bth.HidControl.WriteRequest,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		&memDesc,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status)) {
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(
		Context->Connection.Bth.HidControl.WriteRequest,
		&params
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
	}

	return status;
}

//
// Send off async HID Control OUT request
// 
NTSTATUS DsBth_SendHidControlWriteRequestAsync(PDEVICE_CONTEXT Context)
{
	NTSTATUS status;

	status = WdfIoTargetFormatRequestForIoctl(
		Context->Connection.Bth.BthIoTarget,
		Context->Connection.Bth.HidControl.WriteRequest,
		IOCTL_BTHPS3_HID_CONTROL_WRITE,
		Context->Connection.Bth.HidControl.WriteMemory,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfIoTargetFormatRequestForIoctl failed with status %!STATUS!",
			status
		);
	}

	WdfRequestSetCompletionRoutine(
		Context->Connection.Bth.HidControl.WriteRequest,
		DsBth_HidControlWriteRequestCompletionRoutine,
		Context
	);

	if (WdfRequestSend(
		Context->Connection.Bth.HidControl.WriteRequest,
		Context->Connection.Bth.BthIoTarget,
		WDF_NO_SEND_OPTIONS
	) == FALSE) {
		status = WdfRequestGetStatus(Context->Connection.Bth.HidControl.WriteRequest);
	}
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestSend failed with status %!STATUS!",
			status
		);
	}

	return status;
}

//
// Send HID Interrupt OUT Request (TODO: unused)
// 
NTSTATUS DsBth_SendHidInterruptWriteRequest(PDEVICE_CONTEXT Context)
{
	NTSTATUS					status;
	WDF_MEMORY_DESCRIPTOR		memDesc;
	WDF_REQUEST_REUSE_PARAMS    params;

	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(
		&memDesc,
		Context->Connection.Bth.HidInterrupt.WriteMemory,
		NULL
	);

	status = WdfIoTargetSendIoctlSynchronously(
		Context->Connection.Bth.BthIoTarget,
		Context->Connection.Bth.HidInterrupt.WriteRequest,
		IOCTL_BTHPS3_HID_INTERRUPT_WRITE,
		&memDesc,
		NULL,
		NULL,
		NULL
	);
	if (!NT_SUCCESS(status)) {
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfIoTargetSendInternalIoctlSynchronously failed with status %!STATUS!",
			status
		);
		return status;
	}

	WDF_REQUEST_REUSE_PARAMS_INIT(
		&params,
		WDF_REQUEST_REUSE_NO_FLAGS,
		STATUS_SUCCESS
	);
	status = WdfRequestReuse(
		Context->Connection.Bth.HidInterrupt.WriteRequest,
		&params
	);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"WdfRequestReuse failed with status %!STATUS!",
			status
		);
	}

	return status;
}

//
// Request device disconnect from host radio
// 
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

//
// Periodic timer to consume pending memory on HID Control channel
// 
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

	TraceDbg(TRACE_DSBTH,
		"++ Control bytes consumed: %d",
		(ULONG)bufferLength
	);

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

//
// Called once 1 second after power-up
// 
_Use_decl_annotations_
VOID
DsBth_EvtControlWriteTimerFunc(
	WDFTIMER  Timer
)
{
	NTSTATUS			status;
	PDEVICE_CONTEXT		pDevCtx;
	PUCHAR				buffer;

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSBTH, "%!FUNC! Exit");

	pDevCtx = DeviceGetContext(WdfTimerGetParentObject(Timer));

	buffer = WdfMemoryGetBuffer(pDevCtx->Connection.Bth.HidControl.WriteMemory, NULL);

	DS3_BTH_SET_LED(buffer, DS3_LED_1); // TODO: what should the default be?
	DS3_BTH_SET_SMALL_RUMBLE_DURATION(buffer, 0xFE);
	DS3_BTH_SET_SMALL_RUMBLE_STRENGTH(buffer, 0x00);
	DS3_BTH_SET_LARGE_RUMBLE_DURATION(buffer, 0xFE);
	DS3_BTH_SET_LARGE_RUMBLE_STRENGTH(buffer, 0x00);

	status = DsBth_SendHidControlWriteRequestAsync(pDevCtx);
	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR,
			TRACE_DSBTH,
			"DsBth_SendHidControlWriteRequestAsync failed with status %!STATUS!",
			status
		);
	}

	TraceEvents(TRACE_LEVEL_INFORMATION, TRACE_DSBTH, "%!FUNC! Exit");
}

//
// Reuse HID Control OUT Request
// 
void DsBth_HidControlWriteRequestCompletionRoutine(
	WDFREQUEST Request,
	WDFIOTARGET Target,
	PWDF_REQUEST_COMPLETION_PARAMS Params,
	WDFCONTEXT Context
)
{
	NTSTATUS                    status;
	WDF_REQUEST_REUSE_PARAMS    params;

	UNREFERENCED_PARAMETER(Target);
	UNREFERENCED_PARAMETER(Context);

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSBTH, "%!FUNC! Entry");

	if (Params->IoStatus.Status != STATUS_DEVICE_NOT_CONNECTED)
	{
		WDF_REQUEST_REUSE_PARAMS_INIT(
			&params,
			WDF_REQUEST_REUSE_NO_FLAGS,
			Params->IoStatus.Status
		);
		status = WdfRequestReuse(Request, &params);
		if (!NT_SUCCESS(status))
		{
			TraceEvents(TRACE_LEVEL_ERROR,
				TRACE_DSBTH,
				"WdfRequestReuse failed with status %!STATUS!",
				status
			);
		}
	}

	TraceEvents(TRACE_LEVEL_VERBOSE, TRACE_DSBTH, "%!FUNC! Exit");
}
